using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Services;

namespace DexGameBacklog.Api.Tests;

public class InputValidationTests
{
    [Fact]
    public void IsValidPlatform_returns_false_for_an_undefined_value()
    {
        var result = InputValidation.IsValidPlatform((Platform)99);

        Assert.False(result);
    }

    [Fact]
    public void IsValidGameStatus_returns_false_for_an_undefined_value()
    {
        var result = InputValidation.IsValidGameStatus((GameStatus)99);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  12345  ", true)]
    [InlineData("12345", true)]
    [InlineData("123456", false)]
    public void IsValidText_checks_trimmed_length(
        string? value,
        bool expected)
    {
        var result = InputValidation.IsValidText(value, 5);

        Assert.Equal(expected, result);
    }
}
