namespace ChatApp.Domain.Entities;

public class Message
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ChatroomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsBotMessage { get; set; } = false;

    public virtual Chatroom Chatroom { get; set; } = null!;
}
