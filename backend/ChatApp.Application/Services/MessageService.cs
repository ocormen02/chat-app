using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Application.Services;

public class MessageService : IMessageService
{
    private readonly IApplicationDbContext _context;

    public MessageService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Message>> GetLatestMessagesAsync(int chatroomId, int limit = 50)
    {
        return await _context.Messages
            .Where(m => m.ChatroomId == chatroomId)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<Message> CreateMessageAsync(string userId, int chatroomId, string content, bool isBotMessage = false)
    {
        var message = new Message
        {
            UserId = userId,
            ChatroomId = chatroomId,
            Content = content,
            Timestamp = DateTime.UtcNow,
            IsBotMessage = isBotMessage
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return message;
    }

    public async Task<Message?> GetMessageByIdAsync(int messageId)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
}
