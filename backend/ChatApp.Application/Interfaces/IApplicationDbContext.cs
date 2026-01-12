using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Chatroom> Chatrooms { get; set; }
    DbSet<Message> Messages { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
