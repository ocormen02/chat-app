using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces;

public interface IMessageService
{
    Task<IEnumerable<Message>> GetLatestMessagesAsync(int chatroomId, int limit = 50);
    Task<Message> CreateMessageAsync(string userId, int chatroomId, string content, bool isBotMessage = false);
    Task<Message?> GetMessageByIdAsync(int messageId);
}
