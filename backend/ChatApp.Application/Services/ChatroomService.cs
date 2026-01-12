using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Application.Services;

public class ChatroomService : IChatroomService
{
    private readonly IApplicationDbContext _context;

    public ChatroomService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Chatroom>> GetAllChatroomsAsync()
    {
        return await _context.Chatrooms
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Chatroom?> GetChatroomByIdAsync(int id)
    {
        return await _context.Chatrooms
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Chatroom> CreateChatroomAsync(string name, string? description = null)
    {
        var chatroom = new Chatroom
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Chatrooms.Add(chatroom);
        await _context.SaveChangesAsync();

        return chatroom;
    }

    public async Task<bool> ChatroomExistsAsync(int id)
    {
        return await _context.Chatrooms
            .AnyAsync(c => c.Id == id);
    }
}
