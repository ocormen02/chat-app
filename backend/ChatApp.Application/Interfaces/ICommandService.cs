namespace ChatApp.Application.Interfaces;

public interface ICommandService
{
    bool IsStockCommand(string message, out string? stockCode);
    bool ValidateStockCode(string stockCode);
}
