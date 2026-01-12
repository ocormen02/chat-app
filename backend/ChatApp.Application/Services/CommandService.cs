using ChatApp.Application.Interfaces;
using System.Text.RegularExpressions;

namespace ChatApp.Application.Services;

public class CommandService : ICommandService
{
    private static readonly Regex StockCommandRegex = new(@"^/stock=([^\s]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool IsStockCommand(string message, out string? stockCode)
    {
        stockCode = null;

        if (string.IsNullOrWhiteSpace(message))
            return false;

        var match = StockCommandRegex.Match(message.Trim());
        if (match.Success)
        {
            stockCode = match.Groups[1].Value;
            return !string.IsNullOrWhiteSpace(stockCode);
        }

        return false;
    }

    public bool ValidateStockCode(string stockCode)
    {
        if (string.IsNullOrWhiteSpace(stockCode))
            return false;

        // Basic validation: should contain letters and optionally numbers/dots
        // Format: e.g., AAPL.US, MSFT.US, etc.
        return Regex.IsMatch(stockCode, @"^[A-Za-z0-9.]+$", RegexOptions.Compiled);
    }
}
