using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class ObjectiveCompletionTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Completing_and_reopening_an_objective_updates_its_timestamp()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var objectiveId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = fixture.CreateContext();
        try
        {
            context.Users.Add(new User
            {
                Id = userId,
                Username = $"objective-completion-{userId:N}"
            });
            context.Games.Add(new Game
            {
                Id = gameId,
                UserId = userId,
                Title = $"Objective completion {gameId:N}",
                Platform = DomainPlatform.STEAM,
                Status = DomainGameStatus.UNPLAYED,
                CreatedAt = now,
                UpdatedAt = now
            });
            context.Objectives.Add(new Objective
            {
                Id = objectiveId,
                GameId = gameId,
                Label = "Complete this objective",
                Position = 1,
                CreatedAt = now,
                UpdatedAt = now
            });
            await context.SaveChangesAsync();

            var controller = new ObjectivesController(
                context,
                new TestCurrentUser(userId));

            var completeAction = await controller.Complete(
                gameId,
                objectiveId,
                CancellationToken.None);
            var completedResponse =
                Assert.IsType<OkObjectResult>(completeAction.Result);
            var completedObjective =
                Assert.IsType<ObjectiveResponse>(completedResponse.Value);

            Assert.True(completedObjective.Completed);
            Assert.NotNull(completedObjective.CompletedAt);

            var reopenAction = await controller.Reopen(
                gameId,
                objectiveId,
                CancellationToken.None);
            var reopenedResponse =
                Assert.IsType<OkObjectResult>(reopenAction.Result);
            var reopenedObjective =
                Assert.IsType<ObjectiveResponse>(reopenedResponse.Value);

            Assert.False(reopenedObjective.Completed);
            Assert.Null(reopenedObjective.CompletedAt);
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
