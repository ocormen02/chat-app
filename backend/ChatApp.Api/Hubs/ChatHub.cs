using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatApp.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IChatroomService _chatroomService;
    private readonly ICommandService _commandService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageService messageService,
        IChatroomService chatroomService,
        ICommandService commandService,
        IRabbitMQService rabbitMQService,
        UserManager<ApplicationUser> userManager,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _chatroomService = chatroomService;
        _commandService = commandService;
        _rabbitMQService = rabbitMQService;
        _userManager = userManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChatroom(int chatroomId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        var chatroomExists = await _chatroomService.ChatroomExistsAsync(chatroomId);
        if (!chatroomExists)
        {
            await Clients.Caller.SendAsync("Error", "Chatroom not found");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chatroom-{chatroomId}");
        _logger.LogInformation("User {UserId} joined chatroom {ChatroomId}", userId, chatroomId);

        // Send latest messages
        var messages = await _messageService.GetLatestMessagesAsync(chatroomId, 50);
        var userIds = messages.Select(m => m.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var messageDtos = messages.Select(m =>
        {
            var user = users.FirstOrDefault(u => u.Id == m.UserId);
            return new MessageDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = user?.UserName,
                UserDisplayName = user?.DisplayName,
                ChatroomId = m.ChatroomId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsBotMessage = m.IsBotMessage
            };
        });

        await Clients.Caller.SendAsync("ReceiveMessages", messageDtos);
    }

    public async Task LeaveChatroom(int chatroomId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chatroom-{chatroomId}");
        _logger.LogInformation("User {UserId} left chatroom {ChatroomId}", userId, chatroomId);
    }

    public async Task SendMessage(int chatroomId, string content)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            await Clients.Caller.SendAsync("Error", "Message content cannot be empty");
            return;
        }

        // Check if it's a stock command
        if (_commandService.IsStockCommand(content, out string? stockCode))
        {
            if (stockCode != null && _commandService.ValidateStockCode(stockCode))
            {
                // Publish to RabbitMQ - don't save to database
                await _rabbitMQService.PublishStockCommandAsync(chatroomId, stockCode, userId);
                _logger.LogInformation("Stock command processed: ChatroomId={ChatroomId}, StockCode={StockCode}, UserId={UserId}", 
                    chatroomId, stockCode, userId);
                return;
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Invalid stock code format");
                return;
            }
        }

        // Regular message - save to database and broadcast
        var chatroomExists = await _chatroomService.ChatroomExistsAsync(chatroomId);
        if (!chatroomExists)
        {
            await Clients.Caller.SendAsync("Error", "Chatroom not found");
            return;
        }

        var message = await _messageService.CreateMessageAsync(userId, chatroomId, content);
        var user = await _userManager.FindByIdAsync(userId);

        var messageDto = new MessageDto
        {
            Id = message.Id,
            UserId = message.UserId,
            UserName = user?.UserName,
            UserDisplayName = user?.DisplayName,
            ChatroomId = message.ChatroomId,
            Content = message.Content,
            Timestamp = message.Timestamp,
            IsBotMessage = message.IsBotMessage
        };

        // Broadcast to all users in the chatroom
        await Clients.Group($"chatroom-{chatroomId}").SendAsync("ReceiveMessage", messageDto);
    }

    public async Task BroadcastMessage(int chatroomId, MessageDto message)
    {
        // Method to be called by the StockResponseProcessor
        await Clients.Group($"chatroom-{chatroomId}").SendAsync("ReceiveMessage", message);
    }
}
