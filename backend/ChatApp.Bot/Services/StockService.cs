using System.Globalization;

namespace ChatApp.Bot.Services;

public interface IStockService
{
    Task<(bool Success, string? Symbol, decimal? Price, string? ErrorMessage)> GetStockQuoteAsync(string stockCode);
}

public class StockService : IStockService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockService> _logger;

    public StockService(HttpClient httpClient, ILogger<StockService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool Success, string? Symbol, decimal? Price, string? ErrorMessage)> GetStockQuoteAsync(string stockCode)
    {
        try
        {
            // Clean stock code
            var cleanStockCode = stockCode.Trim().ToUpperInvariant();
            
            // Build Stooq API URL
            var url = $"https://stooq.com/q/l/?s={Uri.EscapeDataString(cleanStockCode)}&f=sd2t2ohlcv&h&e=csv";

            _logger.LogInformation("Fetching stock quote from Stooq API: StockCode={StockCode}, URL={Url}", cleanStockCode, url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"Stooq API returned status code: {response.StatusCode}";
                _logger.LogWarning("Stooq API error: {Error}", errorMsg);
                return (false, null, null, errorMsg);
            }

            var csvContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                var errorMsg = "Empty response from Stooq API";
                _logger.LogWarning("Stooq API returned empty response");
                return (false, null, null, errorMsg);
            }

            // Parse CSV
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length < 2)
            {
                var errorMsg = "Invalid CSV format: not enough lines";
                _logger.LogWarning("Invalid CSV format: {CsvContent}", csvContent);
                return (false, null, null, errorMsg);
            }

            // Skip header line, get data line
            var dataLine = lines[1];
            var columns = dataLine.Split(',');

            // CSV format: Symbol,Date,Time,Open,High,Low,Close,Volume
            if (columns.Length < 8)
            {
                var errorMsg = $"Invalid CSV format: expected 8 columns, got {columns.Length}";
                _logger.LogWarning("Invalid CSV format: {DataLine}", dataLine);
                return (false, null, null, errorMsg);
            }

            var symbol = columns[0].Trim().Trim('"');
            var closePriceStr = columns[6].Trim().Trim('"'); // Close price is at index 6

            // Check if price is valid (not "N/D" or empty)
            if (string.IsNullOrWhiteSpace(closePriceStr) || 
                closePriceStr.Equals("N/D", StringComparison.OrdinalIgnoreCase) ||
                !decimal.TryParse(closePriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var closePrice))
            {
                var errorMsg = $"Invalid or unavailable stock price for symbol: {symbol}";
                _logger.LogWarning("Invalid stock price: Symbol={Symbol}, Price={PriceStr}", symbol, closePriceStr);
                return (false, symbol, null, errorMsg);
            }

            if (closePrice <= 0)
            {
                var errorMsg = $"Invalid stock price (must be positive): {closePrice}";
                _logger.LogWarning("Invalid stock price (non-positive): Symbol={Symbol}, Price={Price}", symbol, closePrice);
                return (false, symbol, null, errorMsg);
            }

            _logger.LogInformation("Successfully retrieved stock quote: Symbol={Symbol}, Price={Price}", symbol, closePrice);
            return (true, symbol, closePrice, null);
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"HTTP error when calling Stooq API: {ex.Message}";
            _logger.LogError(ex, "HTTP error fetching stock quote");
            return (false, null, null, errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error fetching stock quote: {ex.Message}";
            _logger.LogError(ex, "Error fetching stock quote");
            return (false, null, null, errorMsg);
        }
    }
}
