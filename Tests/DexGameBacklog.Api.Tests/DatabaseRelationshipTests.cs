using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Tests;

public sealed class DatabaseRelationshipTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Deleting_a_game_deletes_its_objectives()
    {
        await using var context = fixture.CreateContext();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var objectiveId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        context.Users.Add(new User
        {
            Id = userId,
            Username = $"cascade-test-{userId:N}"
        });
        context.Games.Add(new Game
        {
            Id = gameId,
            UserId = userId,
            Title = $"Cascade test {gameId:N}",
            Platform = Platform.STEAM,
            Status = GameStatus.UNPLAYED,
            CreatedAt = now,
            UpdatedAt = now
        });
        context.Objectives.Add(new Objective
        {
            Id = objectiveId,
            GameId = gameId,
            Label = "Delete with game",
            Position = 1,
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();

        var game = await context.Games.SingleAsync(item => item.Id == gameId);
        context.Games.Remove(game);
        await context.SaveChangesAsync();

        Assert.False(await context.Objectives.AnyAsync(item => item.Id == objectiveId));
        Assert.True(await context.Users.AnyAsync(item => item.Id == userId));

        var user = await context.Users.SingleAsync(item => item.Id == userId);
        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }
}
