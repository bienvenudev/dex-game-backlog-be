using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class StatusHistoryTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Moving_a_game_to_playing_records_start_time_and_history()
    {
        await using var context = fixture.CreateContext();
        var userId = await AddUserAsync(context);

        try
        {
            var game = await AddGameAsync(context, userId, "Status history game");
            var controller = new GamesController(
                context,
                new TestCurrentUser(userId),
                new GameStatusService(context, new NoOpGameStatusEventPublisher()));

            var action = await controller.UpdateStatus(
                game.Id,
                new StatusUpdateInput
                {
                    Status = ContractGameStatus.PLAYING,
                    Comment = "Started playing"
                },
                CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(action.Result);
            var response = Assert.IsType<StatusUpdateResponse>(okResult.Value);
            Assert.Equal(ContractGameStatus.PLAYING, response.Game.Status);

            await using var verify = fixture.CreateContext();

            var savedGame = await verify.Games
                .AsNoTracking()
                .SingleAsync(item => item.Id == game.Id);
            Assert.NotNull(savedGame.StartedAt);
            Assert.Null(savedGame.FinishedAt);

            var history = await verify.GameStatusHistories
                .AsNoTracking()
                .SingleAsync(item => item.GameId == game.Id);
            Assert.Equal(DomainGameStatus.UNPLAYED, history.PreviousStatus);
            Assert.Equal(DomainGameStatus.PLAYING, history.NewStatus);
            Assert.Equal(userId, history.ChangedBy);
            Assert.Equal("Started playing", history.Comment);
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
            Username = $"status-history-{userId:N}"
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
            Title = title,
            Platform = DomainPlatform.STEAM,
            Status = DomainGameStatus.UNPLAYED,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        return game;
    }

    private static async Task RemoveUsersAsync(AppDbContext context, params Guid[] userIds)
    {
        // History rows restrict deletion of their changing user, so they go first.
        var histories = await context.GameStatusHistories
            .Where(history => userIds.Contains(history.ChangedBy))
            .ToListAsync();
        context.GameStatusHistories.RemoveRange(histories);

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
