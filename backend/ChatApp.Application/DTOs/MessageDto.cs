namespace ChatApp.Application.DTOs;

public class MessageDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserDisplayName { get; set; }
    public int ChatroomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsBotMessage { get; set; }
}

public class SendMessageDto
{
    public int ChatroomId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class StockCommandMessageDto
{
    public int ChatroomId { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class StockResponseMessageDto
{
    public int ChatroomId { get; set; }
    public string Message { get; set; } = string.Empty;
}
