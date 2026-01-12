using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces;

public interface IChatroomService
{
    Task<IEnumerable<Chatroom>> GetAllChatroomsAsync();
    Task<Chatroom?> GetChatroomByIdAsync(int id);
    Task<Chatroom> CreateChatroomAsync(string name, string? description = null);
    Task<bool> ChatroomExistsAsync(int id);
}
