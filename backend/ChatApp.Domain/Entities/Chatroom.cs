namespace ChatApp.Domain.Entities;

public class Chatroom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
