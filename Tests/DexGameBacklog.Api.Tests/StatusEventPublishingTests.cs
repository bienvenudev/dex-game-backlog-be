using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;

namespace DexGameBacklog.Api.Tests;

public sealed class StatusEventPublishingTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Successful_status_change_publishes_the_saved_status_event()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = fixture.CreateContext();
        try
        {
            context.Users.Add(new User
            {
                Id = userId,
                Username = $"event-test-{userId:N}"
            });
            context.Games.Add(new Game
            {
                Id = gameId,
                UserId = userId,
                Title = $"Event test {gameId:N}",
                Platform = DomainPlatform.STEAM,
                Status = DomainGameStatus.UNPLAYED,
                CreatedAt = now,
                UpdatedAt = now
            });
            await context.SaveChangesAsync();

            var publisher = new RecordingPublisher();
            var controller = new GamesController(
                context,
                new TestCurrentUser(userId),
                publisher);

            var action = await controller.UpdateStatus(
                gameId,
                new StatusUpdateInput
                {
                    Status = ContractGameStatus.PLAYING,
                    Comment = "Started playing"
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(action.Result);
            var statusEvent = Assert.Single(publisher.Events);
            Assert.Equal(gameId, statusEvent.GameId);
            Assert.Equal(userId, statusEvent.UserId);
            Assert.Equal(DomainGameStatus.UNPLAYED, statusEvent.PreviousStatus);
            Assert.Equal(DomainGameStatus.PLAYING, statusEvent.NewStatus);
            Assert.Equal("Started playing", statusEvent.Comment);

            var history = await context.GameStatusHistories
                .SingleAsync(item => item.GameId == gameId);
            Assert.Equal(statusEvent.NewStatus, history.NewStatus);
        }
        finally
        {
            var histories = await context.GameStatusHistories
                .Where(item => item.ChangedBy == userId)
                .ToListAsync();
            context.GameStatusHistories.RemoveRange(histories);

            var games = await context.Games
                .Where(item => item.UserId == userId)
                .ToListAsync();
            context.Games.RemoveRange(games);

            var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
            if (user is not null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }
    }

    private sealed class RecordingPublisher : IGameStatusEventPublisher
    {
        public List<GameStatusChangedEvent> Events { get; } = [];

        public Task PublishAsync(
            GameStatusChangedEvent statusChangedEvent,
            CancellationToken cancellationToken)
        {
            Events.Add(statusChangedEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
