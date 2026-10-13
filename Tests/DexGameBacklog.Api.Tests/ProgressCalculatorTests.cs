using DexGameBacklog.Api.Services;

namespace DexGameBacklog.Api.Tests;

public class ProgressCalculatorTests
{
    [Fact]
    public void Calculate_returns_null_percentage_when_game_has_no_objectives()
    {
        var result = ProgressCalculator.Calculate(0, 0);

        Assert.Equal(0, result.Completed);
        Assert.Equal(0, result.Total);
        Assert.Null(result.Percentage);
    }

    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(4, 4, 100)]
    public void Calculate_returns_completed_total_and_rounded_percentage(
        int completed,
        int total,
        int expectedPercentage)
    {
        var result = ProgressCalculator.Calculate(completed, total);

        Assert.Equal(completed, result.Completed);
        Assert.Equal(total, result.Total);
        Assert.Equal(expectedPercentage, result.Percentage);
    }
}
