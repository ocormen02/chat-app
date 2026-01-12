namespace ChatApp.Application.Interfaces;

public interface IRabbitMQService
{
    Task PublishStockCommandAsync(int chatroomId, string stockCode, string userId);
    Task PublishStockResponseAsync(int chatroomId, string message);
}
