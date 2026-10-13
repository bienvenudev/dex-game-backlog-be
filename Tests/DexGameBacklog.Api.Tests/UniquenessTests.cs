using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using ContractPlatform = DexGameBacklog.Api.Contracts.Platform;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class UniquenessTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Creating_a_game_with_a_duplicate_title_and_platform_returns_conflict()
    {
        await using var context = fixture.CreateContext();
        var userId = await AddUserAsync(context);

        try
        {
            await AddGameAsync(context, userId, "Hollow Knight", DomainPlatform.STEAM);
            var controller = new GamesController(
                context,
                new TestCurrentUser(userId),
                new NoOpGameStatusEventPublisher());

            var action = await controller.Create(
                new GameInput
                {
                    Title = "Hollow Knight",
                    Platform = ContractPlatform.STEAM,
                    Status = ContractGameStatus.UNPLAYED
                },
                CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(action.Result);
            Assert.Equal(1, await context.Games.CountAsync(game => game.UserId == userId));
        }
        finally
        {
            await RemoveUsersAsync(context, userId);
        }
    }

    [Fact]
    public async Task Adding_a_duplicate_objective_label_to_a_game_returns_conflict()
    {
        await using var context = fixture.CreateContext();
        var userId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, userId, "Objective uniqueness game", DomainPlatform.STEAM);
            await AddObjectiveAsync(context, game.Id, "Beat the game on hard");
            var controller = new ObjectivesController(
                context,
                new TestCurrentUser(userId));

            var action = await controller.Create(
                game.Id,
                new ObjectiveInput { Label = "Beat the game on hard" },
                CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(action.Result);
            Assert.Equal(1, await context.Objectives.CountAsync(objective => objective.GameId == game.Id));
        }
        finally
        {
            await RemoveUsersAsync(context, userId);
        }
    }

    private static async Task<Guid> AddUserAsync(AppDbContext context)
    {
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Username = $"uniqueness-{userId:N}"
        });
        await context.SaveChangesAsync();

        return userId;
    }

    private static async Task<Game> AddGameAsync(
        AppDbContext context,
        Guid userId,
        string title,
        DomainPlatform platform)
    {
        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Platform = platform,
            Status = DomainGameStatus.UNPLAYED,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        return game;
    }

    private static async Task AddObjectiveAsync(AppDbContext context, Guid gameId, string label)
    {
        var now = DateTimeOffset.UtcNow;
        context.Objectives.Add(new Objective
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Label = label,
            Position = 1,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
    }

    private static async Task RemoveUsersAsync(AppDbContext context, params Guid[] userIds)
    {
        var users = await context.Users
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync();

        context.Users.RemoveRange(users);
        await context.SaveChangesAsync();
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
