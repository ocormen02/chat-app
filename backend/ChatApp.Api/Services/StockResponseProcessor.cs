using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChatApp.Api.Services;

public class StockResponseProcessor : BackgroundService
{
    private readonly IHubContext<Hubs.ChatHub> _hubContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StockResponseProcessor> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isInitialized;
    private readonly object _initLock = new object();
    private string? _botUserId;
    private const string StockResponsesQueue = "stock-responses";
    private const string BotUserEmail = "stockbot@chatapp.local";

    public StockResponseProcessor(
        IHubContext<Hubs.ChatHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<StockResponseProcessor> logger)
    {
        _hubContext = hubContext;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockResponseProcessor starting...");

        try
        {
            // Prevent multiple initializations
            lock (_initLock)
            {
                if (_isInitialized)
                {
                    _logger.LogWarning("StockResponseProcessor already initialized. Skipping initialization.");
                    return;
                }
            }

            if (!InitializeRabbitMQ())
            {
                _logger.LogError("Failed to initialize RabbitMQ. StockResponseProcessor will not start. Waiting before shutdown...");
                // Wait a bit to avoid rapid restarts if there's a configuration issue
                await Task.Delay(5000, stoppingToken);
                return;
            }

            // Get bot user ID
            if (!await InitializeBotUserIdAsync())
            {
                _logger.LogError("Failed to get bot user ID. StockResponseProcessor will not start. Waiting before shutdown...");
                await Task.Delay(5000, stoppingToken);
                return;
            }

            lock (_initLock)
            {
                _isInitialized = true;
            }

            if (_channel == null)
            {
                _logger.LogError("Channel is null after initialization. StockResponseProcessor cannot start.");
                await Task.Delay(5000, stoppingToken);
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

                    // Configure JSON serializer options
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    };

                    StockResponseMessageDto? response;
                    try
                    {
                        response = JsonSerializer.Deserialize<StockResponseMessageDto>(messageJson, jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize message from queue. Message JSON: {MessageJson}", messageJson);
                        // ACK the corrupted message to prevent infinite requeuing
                        _channel?.BasicAck(deliveryTag, false);
                        return;
                    }

                    // Handle null response
                    if (response == null)
                    {
                        _logger.LogWarning("Deserialized response is null. Message JSON: {MessageJson}. ACKing message to prevent requeue.", messageJson);
                        _channel?.BasicAck(deliveryTag, false);
                        return;
                    }

                    // Validate response data
                    if (response.ChatroomId <= 0)
                    {
                        _logger.LogWarning("Invalid ChatroomId in response: {ChatroomId}. ACKing message.", response.ChatroomId);
                        _channel?.BasicAck(deliveryTag, false);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(response.Message))
                    {
                        _logger.LogWarning("Empty or null message content in response. ChatroomId: {ChatroomId}. ACKing message.", response.ChatroomId);
                        _channel?.BasicAck(deliveryTag, false);
                        return;
                    }

                    _logger.LogInformation("Processing stock response: ChatroomId={ChatroomId}, MessageLength={MessageLength}", 
                        response.ChatroomId, response.Message.Length);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

                    // Save bot message to database
                    if (string.IsNullOrEmpty(_botUserId))
                    {
                        _logger.LogError("Bot user ID is not set. Cannot save bot message.");
                        _channel?.BasicNack(deliveryTag, false, true); // Requeue
                        return;
                    }

                    var botMessage = await messageService.CreateMessageAsync(
                        _botUserId,
                        response.ChatroomId,
                        response.Message,
                        isBotMessage: true);

                    _logger.LogDebug("Bot message saved to database: MessageId={MessageId}, ChatroomId={ChatroomId}", 
                        botMessage.Id, botMessage.ChatroomId);

                    // Get user info (bot user)
                    var messageDto = new MessageDto
                    {
                        Id = botMessage.Id,
                        UserId = _botUserId,
                        UserName = "StockBot",
                        UserDisplayName = "StockBot",
                        ChatroomId = botMessage.ChatroomId,
                        Content = botMessage.Content,
                        Timestamp = botMessage.Timestamp,
                        IsBotMessage = true
                    };

                    // Broadcast via SignalR
                    await _hubContext.Clients.Group($"chatroom-{response.ChatroomId}")
                        .SendAsync("ReceiveMessage", messageDto, stoppingToken);

                    _logger.LogInformation("Stock response processed and broadcast successfully: ChatroomId={ChatroomId}, MessageId={MessageId}", 
                        response.ChatroomId, botMessage.Id);

                    _channel.BasicAck(deliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stock response. DeliveryTag={DeliveryTag}", deliveryTag);
                    _channel?.BasicNack(deliveryTag, false, true); // Requeue on error
                }
            };

            _channel.BasicConsume(
                queue: StockResponsesQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("StockResponseProcessor started successfully, consuming from queue: {QueueName}", 
                StockResponsesQueue);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("StockResponseProcessor is shutting down due to cancellation request.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("StockResponseProcessor was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in StockResponseProcessor. Worker will stop.");
            // Don't throw - let the service stop gracefully instead of causing rapid restarts
            await Task.Delay(5000, stoppingToken);
        }
        finally
        {
            Cleanup();
        }
    }

    private bool InitializeRabbitMQ()
    {
        try
        {
            // Clean up any existing connection first
            Cleanup();

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
                Password = password,
                AutomaticRecoveryEnabled = true, // Enable automatic recovery
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare queue
            _channel.QueueDeclare(queue: StockResponsesQueue, durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("RabbitMQ queue declared successfully: {QueueName}", StockResponsesQueue);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection. Check if RabbitMQ server is running and configuration is correct. Error: {ErrorMessage}", ex.Message);
            Cleanup();
            return false;
        }
    }

    private async Task<bool> InitializeBotUserIdAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var botUser = await userManager.FindByEmailAsync(BotUserEmail);
            if (botUser == null)
            {
                _logger.LogError("Bot user with email '{BotUserEmail}' not found. Make sure the bot user is initialized.", BotUserEmail);
                return false;
            }

            _botUserId = botUser.Id;
            _logger.LogInformation("Bot user ID retrieved successfully: {BotUserId}", _botUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bot user ID. Error: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    private void Cleanup()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _channel = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cleanup of RabbitMQ resources: {ErrorMessage}", ex.Message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StockResponseProcessor is stopping");
        
        lock (_initLock)
        {
            _isInitialized = false;
        }
        
        Cleanup();
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("StockResponseProcessor stopped");
    }
}
