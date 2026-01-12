using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Bot.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChatApp.Bot.Workers;

public class StockBotWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IStockService _stockService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<StockBotWorker> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private const string StockCommandsQueue = "stock-commands";
    private const string StockResponsesQueue = "stock-responses";
    private const string StockCommandsDLQ = "stock-commands-dlq";

    public StockBotWorker(
        IConfiguration configuration,
        IStockService stockService,
        IRabbitMQService rabbitMQService,
        ILogger<StockBotWorker> logger)
    {
        _configuration = configuration;
        _stockService = stockService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel. StockBotWorker cannot start.");
                return;
            }

            _logger.LogInformation("RabbitMQ channel initialized successfully. Host={HostName}, Port={Port}", 
                _configuration["RabbitMQ:HostName"] ?? "localhost",
                _configuration["RabbitMQ:Port"] ?? "5672");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var deliveryTag = ea.DeliveryTag;

                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    _logger.LogDebug("Received message from queue: {MessageJson}", messageJson);
                    
                    var command = JsonSerializer.Deserialize<StockCommandMessageDto>(messageJson);

                    if (command == null || string.IsNullOrWhiteSpace(command.StockCode))
                    {
                        _logger.LogWarning("Invalid stock command received: {MessageJson}", messageJson);
                        _channel?.BasicAck(deliveryTag, false);
                        return;
                    }

                    _logger.LogInformation("Processing stock command: ChatroomId={ChatroomId}, StockCode={StockCode}, UserId={UserId}",
                        command.ChatroomId, command.StockCode, command.UserId);

                    // Call Stooq API
                    var (success, symbol, price, errorMessage) = await _stockService.GetStockQuoteAsync(command.StockCode);

                    if (success && symbol != null && price.HasValue)
                    {
                        var responseMessage = $"{symbol.ToUpperInvariant()} quote is ${price:F2} per share";

                        await _rabbitMQService.PublishStockResponseAsync(command.ChatroomId, responseMessage);

                        _logger.LogInformation("Stock quote processed successfully: ChatroomId={ChatroomId}, Symbol={Symbol}, Price={Price}",
                            command.ChatroomId, symbol, price);
                    }
                    else
                    {
                        var errorResponse = $"Unable to retrieve stock quote for '{command.StockCode}'. Please check the stock code and try again.";
                        
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            _logger.LogWarning("Stock quote error: {ErrorMessage}", errorMessage);
                        }

                        await _rabbitMQService.PublishStockResponseAsync(command.ChatroomId, errorResponse);

                        // Send to Dead Letter Queue for monitoring
                        await PublishToDLQAsync(body, ea.BasicProperties, errorMessage ?? "Unknown error");
                    }

                    _channel?.BasicAck(deliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stock command");
                    _channel?.BasicNack(deliveryTag, false, true); // Requeue on error
                }
            };

            _channel.BasicConsume(
                queue: StockCommandsQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("StockBotWorker started successfully, consuming from queue: {QueueName}", StockCommandsQueue);
            _logger.LogInformation("StockBotWorker is ready to process stock commands");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in StockBotWorker. Worker will stop.");
            throw;
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");
            var userName = _configuration["RabbitMQ:UserName"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";

            _logger.LogInformation("Initializing RabbitMQ connection. Host={HostName}, Port={Port}, User={UserName}", 
                hostName, port, userName);

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare queues
            _channel.QueueDeclare(queue: StockCommandsQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(queue: StockResponsesQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(queue: StockCommandsDLQ, durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("RabbitMQ queues declared successfully: {CommandsQueue}, {ResponsesQueue}, {DLQ}", 
                StockCommandsQueue, StockResponsesQueue, StockCommandsDLQ);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection. Check if RabbitMQ server is running and configuration is correct.");
            _channel?.Dispose();
            _channel = null;
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
            throw;
        }
    }

    private Task PublishToDLQAsync(byte[] body, IBasicProperties? properties, string errorMessage)
    {
        try
        {
            if (_channel == null) return Task.CompletedTask;

            var dlqProperties = _channel.CreateBasicProperties();
            if (properties != null)
            {
                dlqProperties.MessageId = properties.MessageId;
                dlqProperties.CorrelationId = properties.CorrelationId;
            }
            dlqProperties.Headers = new Dictionary<string, object>
            {
                { "error", errorMessage },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: StockCommandsDLQ,
                basicProperties: dlqProperties,
                body: body);

            _logger.LogInformation("Message sent to DLQ: Error={Error}", errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to DLQ");
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StockBotWorker is stopping");
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
