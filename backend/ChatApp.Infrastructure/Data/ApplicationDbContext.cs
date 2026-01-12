using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chatroom> Chatrooms { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Chatroom
        builder.Entity<Chatroom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.Name);
        });

        // Configure Message
        builder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(2000);
            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.HasOne(e => e.Chatroom)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChatroomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ChatroomId, e.Timestamp });
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasMany(e => e.Messages)
                .WithOne()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
