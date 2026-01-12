using ChatApp.Application.Services;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatApp.Application.Tests.Services;

public class MessageServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MessageService _messageService;

    public MessageServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _messageService = new MessageService(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var chatroom = new Chatroom { Id = 1, Name = "Test Room", CreatedAt = DateTime.UtcNow };
        _context.Chatrooms.Add(chatroom);

        // Add 60 messages to test limit
        for (int i = 0; i < 60; i++)
        {
            _context.Messages.Add(new Message
            {
                UserId = "user1",
                ChatroomId = 1,
                Content = $"Message {i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                IsBotMessage = false
            });
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetLatestMessagesAsync_ReturnsMaximumLimit()
    {
        // Act
        var messages = await _messageService.GetLatestMessagesAsync(1, 50);

        // Assert
        messages.Should().HaveCount(50);
    }

    [Fact]
    public async Task GetLatestMessagesAsync_MessagesOrderedByTimestampAscending()
    {
        // Act
        var messages = (await _messageService.GetLatestMessagesAsync(1, 50)).ToList();

        // Assert
        messages.Should().BeInAscendingOrder(m => m.Timestamp);
    }

    [Fact]
    public async Task GetLatestMessagesAsync_ReturnsLatestMessages()
    {
        // Act
        var messages = (await _messageService.GetLatestMessagesAsync(1, 50)).ToList();

        // Assert
        messages.First().Timestamp.Should().BeBefore(messages.Last().Timestamp);
        // Latest messages should be the ones with most recent timestamps
        messages.Last().Content.Should().Contain("Message 0"); // Most recent (Message 0 has Timestamp = DateTime.UtcNow)
    }

    [Fact]
    public async Task CreateMessageAsync_CreatesMessageSuccessfully()
    {
        // Arrange
        var userId = "user2";
        var chatroomId = 1;
        var content = "Test message";

        // Act
        var message = await _messageService.CreateMessageAsync(userId, chatroomId, content);

        // Assert
        message.Should().NotBeNull();
        message.UserId.Should().Be(userId);
        message.ChatroomId.Should().Be(chatroomId);
        message.Content.Should().Be(content);
        message.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateMessageAsync_CreatesBotMessage()
    {
        // Arrange
        var userId = "BOT";
        var chatroomId = 1;
        var content = "Bot message";

        // Act
        var message = await _messageService.CreateMessageAsync(userId, chatroomId, content, isBotMessage: true);

        // Assert
        message.IsBotMessage.Should().BeTrue();
    }

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsMessage()
    {
        // Arrange
        var createdMessage = await _messageService.CreateMessageAsync("user1", 1, "Test");

        // Act
        var message = await _messageService.GetMessageByIdAsync(createdMessage.Id);

        // Assert
        message.Should().NotBeNull();
        message!.Id.Should().Be(createdMessage.Id);
        message.Content.Should().Be("Test");
    }

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsNullForNonExistentMessage()
    {
        // Act
        var message = await _messageService.GetMessageByIdAsync(99999);

        // Assert
        message.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
