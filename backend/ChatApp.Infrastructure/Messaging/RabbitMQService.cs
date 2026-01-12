using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ChatApp.Infrastructure.Messaging;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private const string StockCommandsQueue = "stock-commands";
    private const string StockResponsesQueue = "stock-responses";
    private const string StockCommandsDLQ = "stock-commands-dlq";

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare queues
        _channel.QueueDeclare(queue: StockCommandsQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: StockResponsesQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: StockCommandsDLQ, durable: true, exclusive: false, autoDelete: false);
    }

    public Task PublishStockCommandAsync(int chatroomId, string stockCode, string userId)
    {
        try
        {
            var message = new StockCommandMessageDto
            {
                ChatroomId = chatroomId,
                StockCode = stockCode,
                UserId = userId
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: StockCommandsQueue,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published stock command: ChatroomId={ChatroomId}, StockCode={StockCode}", chatroomId, stockCode);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing stock command");
            throw;
        }
    }

    public Task PublishStockResponseAsync(int chatroomId, string message)
    {
        try
        {
            var response = new StockResponseMessageDto
            {
                ChatroomId = chatroomId,
                Message = message
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: StockResponsesQueue,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published stock response: ChatroomId={ChatroomId}", chatroomId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing stock response");
            throw;
        }
    }

    public IModel GetChannel() => _channel;
    public string StockCommandsQueueName => StockCommandsQueue;
    public string StockResponsesQueueName => StockResponsesQueue;
    public string StockCommandsDLQName => StockCommandsDLQ;

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
