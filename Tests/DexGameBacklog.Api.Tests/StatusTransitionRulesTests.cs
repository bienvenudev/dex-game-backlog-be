using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;

namespace DexGameBacklog.Api.Tests;

public class StatusTransitionRulesTests
{
    [Theory]
    [InlineData(GameStatus.UNPLAYED, GameStatus.PLAYING)]
    [InlineData(GameStatus.UNPLAYED, GameStatus.FINISHED)]
    [InlineData(GameStatus.UNPLAYED, GameStatus.ABANDONED)]
    [InlineData(GameStatus.PLAYING, GameStatus.UNPLAYED)]
    [InlineData(GameStatus.PLAYING, GameStatus.FINISHED)]
    [InlineData(GameStatus.PLAYING, GameStatus.ABANDONED)]
    [InlineData(GameStatus.FINISHED, GameStatus.PLAYING)]
    [InlineData(GameStatus.FINISHED, GameStatus.ABANDONED)]
    [InlineData(GameStatus.ABANDONED, GameStatus.PLAYING)]
    [InlineData(GameStatus.ABANDONED, GameStatus.FINISHED)]
    public void IsAllowed_returns_true_for_allowed_transitions(
        GameStatus currentStatus,
        GameStatus requestedStatus)
    {
        var result = StatusTransitionRules.IsAllowed(currentStatus, requestedStatus);

        Assert.True(result);
    }

    [Theory]
    [InlineData(GameStatus.FINISHED, GameStatus.UNPLAYED)]
    [InlineData(GameStatus.ABANDONED, GameStatus.UNPLAYED)]
    public void IsAllowed_returns_false_for_rejected_transitions(
        GameStatus currentStatus,
        GameStatus requestedStatus)
    {
        var result = StatusTransitionRules.IsAllowed(currentStatus, requestedStatus);

        Assert.False(result);
    }

    [Theory]
    [InlineData(GameStatus.UNPLAYED)]
    [InlineData(GameStatus.PLAYING)]
    [InlineData(GameStatus.FINISHED)]
    [InlineData(GameStatus.ABANDONED)]
    public void IsAllowed_returns_false_when_status_does_not_change(
        GameStatus currentStatus)
    {
        var result = StatusTransitionRules.IsAllowed(currentStatus, currentStatus);

        Assert.False(result);
    }
}
