using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using ContractPlatform = DexGameBacklog.Api.Contracts.Platform;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class OwnershipTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Reading_another_users_game_returns_not_found()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, ownerId, "Owner's game");
            var controller = new GamesController(
                context,
                new TestCurrentUser(otherId),
                new NoOpGameStatusEventPublisher());

            var action = await controller.GetById(game.Id, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(action.Result);
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    [Fact]
    public async Task Updating_another_users_game_returns_not_found()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, ownerId, "Owner's game");
            var controller = new GamesController(
                context,
                new TestCurrentUser(otherId),
                new NoOpGameStatusEventPublisher());

            var action = await controller.Update(
                game.Id,
                new GameInput
                {
                    Title = "Changed by another user",
                    Platform = ContractPlatform.STEAM,
                    Status = ContractGameStatus.UNPLAYED
                },
                CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(action.Result);
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    [Fact]
    public async Task Deleting_another_users_game_returns_not_found_and_keeps_the_game()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, ownerId, "Owner's game");
            var controller = new GamesController(
                context,
                new TestCurrentUser(otherId),
                new NoOpGameStatusEventPublisher());

            var action = await controller.Delete(game.Id, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(action);
            Assert.True(await context.Games.AnyAsync(item => item.Id == game.Id));
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    [Fact]
    public async Task Adding_an_objective_to_another_users_game_returns_not_found()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, ownerId, "Owner's game");
            var controller = new ObjectivesController(
                context,
                new TestCurrentUser(otherId));

            var action = await controller.Create(
                game.Id,
                new ObjectiveInput { Label = "Sneaky objective" },
                CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(action.Result);
            Assert.False(await context.Objectives.AnyAsync(item => item.GameId == game.Id));
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    [Fact]
    public async Task Reading_an_objective_through_another_users_game_returns_not_found()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, ownerId, "Owner's game");
            var objective = await AddObjectiveAsync(context, game.Id, "Owner's objective");
            var controller = new ObjectivesController(
                context,
                new TestCurrentUser(otherId));

            var action = await controller.GetById(game.Id, objective.Id, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(action.Result);
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    [Fact]
    public async Task Library_list_excludes_other_users_games()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            await AddGameAsync(context, ownerId, "Owner's game");
            var otherGame = await AddGameAsync(context, otherId, "Other user's game");
            var controller = new GamesController(
                context,
                new TestCurrentUser(otherId),
                new NoOpGameStatusEventPublisher())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var action = await controller.Get(new GameQueryParameters(), CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(action.Result);
            var games = Assert.IsAssignableFrom<IReadOnlyList<GameSummaryResponse>>(okResult.Value);
            var game = Assert.Single(games);
            Assert.Equal(otherGame.Id, game.Id);
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    [Fact]
    public async Task Dashboard_totals_exclude_other_users_games()
    {
        await using var context = fixture.CreateContext();
        var ownerId = await AddUserAsync(context);
        var otherId = await AddUserAsync(context);

        try
        {
            await AddGameAsync(context, ownerId, "Owner's game");
            await AddGameAsync(context, ownerId, "Owner's second game");
            var otherGame = await AddGameAsync(context, otherId, "Other user's game");
            var controller = new DashboardController(
                context,
                new TestCurrentUser(otherId));

            var action = await controller.GetSummary(CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(action.Result);
            var summary = Assert.IsType<DashboardSummaryResponse>(okResult.Value);
            Assert.Equal(1, summary.TotalGames);
            var recentGame = Assert.Single(summary.RecentlyUpdatedGames);
            Assert.Equal(otherGame.Id, recentGame.Id);
        }
        finally
        {
            await RemoveUsersAsync(context, ownerId, otherId);
        }
    }

    private static async Task<Guid> AddUserAsync(AppDbContext context)
    {
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Username = $"ownership-{userId:N}"
        });
        await context.SaveChangesAsync();

        return userId;
    }

    private static async Task<Game> AddGameAsync(AppDbContext context, Guid userId, string title)
    {
        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = $"{title} {Guid.NewGuid():N}",
            Platform = DomainPlatform.STEAM,
            Status = DomainGameStatus.UNPLAYED,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        return game;
    }

    private static async Task<Objective> AddObjectiveAsync(AppDbContext context, Guid gameId, string label)
    {
        var now = DateTimeOffset.UtcNow;
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Label = label,
            Position = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        return objective;
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
