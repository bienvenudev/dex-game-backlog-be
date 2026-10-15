using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class ObjectiveReorderingTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Reordering_objectives_updates_every_position()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var firstObjectiveId = Guid.NewGuid();
        var secondObjectiveId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = fixture.CreateContext();
        try
        {
            context.Users.Add(new User
            {
                Id = userId,
                Username = $"objective-order-{userId:N}"
            });
            context.Games.Add(new Game
            {
                Id = gameId,
                UserId = userId,
                Title = $"Objective order {gameId:N}",
                Platform = DomainPlatform.STEAM,
                Status = DomainGameStatus.UNPLAYED,
                CreatedAt = now,
                UpdatedAt = now
            });
            context.Objectives.AddRange(
                new Objective
                {
                    Id = firstObjectiveId,
                    GameId = gameId,
                    Label = "First",
                    Position = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Objective
                {
                    Id = secondObjectiveId,
                    GameId = gameId,
                    Label = "Second",
                    Position = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            await context.SaveChangesAsync();

            var controller = new ObjectivesController(
                context,
                new TestCurrentUser(userId));

            var action = await controller.Reorder(
                gameId,
                new ObjectiveOrderInput([secondObjectiveId, firstObjectiveId]),
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(action.Result);

            var objectives = await context.Objectives
                .Where(item => item.GameId == gameId)
                .ToDictionaryAsync(item => item.Id);
            Assert.Equal(1, objectives[secondObjectiveId].Position);
            Assert.Equal(2, objectives[firstObjectiveId].Position);
        }
        finally
        {
            var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
            if (user is not null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Failed_reorder_leaves_the_original_order_untouched()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var firstObjectiveId = Guid.NewGuid();
        var secondObjectiveId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = fixture.CreateContext();
        try
        {
            context.Users.Add(new User
            {
                Id = userId,
                Username = $"failed-order-{userId:N}"
            });
            context.Games.Add(new Game
            {
                Id = gameId,
                UserId = userId,
                Title = $"Failed order {gameId:N}",
                Platform = DomainPlatform.STEAM,
                Status = DomainGameStatus.UNPLAYED,
                CreatedAt = now,
                UpdatedAt = now
            });
            context.Objectives.AddRange(
                new Objective
                {
                    Id = firstObjectiveId,
                    GameId = gameId,
                    Label = "First",
                    Position = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Objective
                {
                    Id = secondObjectiveId,
                    GameId = gameId,
                    Label = "Second",
                    Position = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            await context.SaveChangesAsync();

            var controller = new ObjectivesController(
                context,
                new TestCurrentUser(userId));

            var action = await controller.Reorder(
                gameId,
                new ObjectiveOrderInput([firstObjectiveId]),
                CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(action.Result);

            var positions = await context.Objectives
                .Where(item => item.GameId == gameId)
                .ToDictionaryAsync(item => item.Id, item => item.Position);
            Assert.Equal(1, positions[firstObjectiveId]);
            Assert.Equal(2, positions[secondObjectiveId]);
        }
        finally
        {
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
