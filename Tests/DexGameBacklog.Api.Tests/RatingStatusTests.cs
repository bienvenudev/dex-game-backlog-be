using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class RatingStatusTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Returning_a_rated_game_to_unplayed_clears_its_rating()
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
                Username = $"rating-status-{userId:N}"
            });
            context.Games.Add(new Game
            {
                Id = gameId,
                UserId = userId,
                Title = $"Rating status {gameId:N}",
                Platform = DomainPlatform.STEAM,
                Status = DomainGameStatus.PLAYING,
                Rating = 8,
                CreatedAt = now,
                UpdatedAt = now,
                StartedAt = now
            });
            await context.SaveChangesAsync();

            var controller = new GamesController(
                context,
                new TestCurrentUser(userId),
                new GameStatusService(context, new NoOpGameStatusEventPublisher()));

            var action = await controller.UpdateStatus(
                gameId,
                new StatusUpdateInput
                {
                    Status = ContractGameStatus.UNPLAYED
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(action.Result);

            var game = await context.Games.SingleAsync(item => item.Id == gameId);
            Assert.Equal(DomainGameStatus.UNPLAYED, game.Status);
            Assert.Null(game.Rating);
        }
        finally
        {
            var histories = await context.GameStatusHistories
                .Where(item => item.ChangedBy == userId)
                .ToListAsync();
            context.GameStatusHistories.RemoveRange(histories);

            var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
            if (user is not null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
