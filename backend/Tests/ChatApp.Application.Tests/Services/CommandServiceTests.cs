using ChatApp.Application.Services;
using FluentAssertions;
using Xunit;

namespace ChatApp.Application.Tests.Services;

public class CommandServiceTests
{
    private readonly CommandService _commandService;

    public CommandServiceTests()
    {
        _commandService = new CommandService();
    }

    [Theory]
    [InlineData("/stock=AAPL.US", "AAPL.US")]
    [InlineData("/stock=MSFT.US", "MSFT.US")]
    [InlineData("/stock=GOOGL.US", "GOOGL.US")]
    [InlineData("/STOCK=AAPL.US", "AAPL.US")] // Case insensitive
    [InlineData("/stock=aapl.us", "aapl.us")]
    public void IsStockCommand_ValidCommand_ReturnsTrueWithStockCode(string message, string expectedStockCode)
    {
        // Act
        var result = _commandService.IsStockCommand(message, out string? stockCode);

        // Assert
        result.Should().BeTrue();
        stockCode.Should().Be(expectedStockCode);
    }

    [Theory]
    [InlineData("Hello world")]
    [InlineData("/stock")]
    [InlineData("/stock=")]
    [InlineData("stock=AAPL.US")]
    [InlineData("/stock=AAPL.US extra text")]
    [InlineData("")]
    [InlineData(null)]
    public void IsStockCommand_InvalidCommand_ReturnsFalse(string? message)
    {
        // Act
        var result = _commandService.IsStockCommand(message ?? string.Empty, out string? stockCode);

        // Assert
        result.Should().BeFalse();
        stockCode.Should().BeNull();
    }

    [Theory]
    [InlineData("AAPL.US", true)]
    [InlineData("MSFT.US", true)]
    [InlineData("GOOGL.US", true)]
    [InlineData("AAPL", true)]
    [InlineData("123", true)]
    [InlineData("AA123.US", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("AAPL.US!", false)] // Contains invalid character
    [InlineData("AAPL US", false)] // Contains space
    [InlineData("AAPL-US", false)] // Contains hyphen
    public void ValidateStockCode_ValidatesCorrectly(string? stockCode, bool expectedResult)
    {
        // Act
        var result = _commandService.ValidateStockCode(stockCode ?? string.Empty);

        // Assert
        result.Should().Be(expectedResult);
    }
}
