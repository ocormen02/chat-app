namespace ChatApp.Application.DTOs;

public class ChatroomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateChatroomDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
