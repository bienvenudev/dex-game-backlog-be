using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Controllers;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractPlatform = DexGameBacklog.Api.Contracts.Platform;

namespace DexGameBacklog.Api.Tests;

public sealed class TransactionalGameCreationTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Creating_a_game_with_objectives_persists_ordered_objectives()
    {
        var userId = Guid.NewGuid();
        var input = new GameInput
        {
            Title = $"Transactional game {userId:N}",
            Platform = ContractPlatform.STEAM,
            Objectives =
            [
                new ObjectiveInput { Label = "First objective" },
                new ObjectiveInput { Label = "Second objective" }
            ]
        };

        await using var context = fixture.CreateContext();
        try
        {
            context.Users.Add(new User
            {
                Id = userId,
                Username = $"transactional-{userId:N}"
            });
            await context.SaveChangesAsync();

            var controller = new GamesController(context, new TestCurrentUser(userId));
            var action = await controller.Create(input, CancellationToken.None);
            var response = Assert.IsType<ObjectResult>(action.Result);

            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);

            var game = await context.Games.SingleAsync(item => item.UserId == userId);
            var objectives = await context.Objectives
                .Where(item => item.GameId == game.Id)
                .OrderBy(item => item.Position)
                .ToListAsync();

            Assert.Equal(["First objective", "Second objective"],
                objectives.Select(item => item.Label));
            Assert.Equal([1, 2], objectives.Select(item => item.Position));
        }
        finally
        {
            await CleanupUserAsync(context, userId);
        }
    }

    [Fact]
    public async Task Invalid_objective_input_does_not_persist_the_game()
    {
        var userId = Guid.NewGuid();
        var title = $"Rolled back game {userId:N}";
        var input = new GameInput
        {
            Title = title,
            Platform = ContractPlatform.STEAM,
            Objectives =
            [
                new ObjectiveInput { Label = "Valid objective" },
                new ObjectiveInput { Label = "valid objective" }
            ]
        };

        await using var context = fixture.CreateContext();
        try
        {
            context.Users.Add(new User
            {
                Id = userId,
                Username = $"rollback-{userId:N}"
            });
            await context.SaveChangesAsync();

            var controller = new GamesController(context, new TestCurrentUser(userId));
            var action = await controller.Create(input, CancellationToken.None);
            var response = Assert.IsType<BadRequestObjectResult>(action.Result);

            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
            Assert.False(await context.Games.AnyAsync(item => item.UserId == userId));
            Assert.False(await context.Objectives.AnyAsync(item =>
                context.Games
                    .Where(game => game.UserId == userId)
                    .Select(game => game.Id)
                    .Contains(item.GameId)));
        }
        finally
        {
            await CleanupUserAsync(context, userId);
        }
    }

    private static async Task CleanupUserAsync(AppDbContext context, Guid userId)
    {
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
        if (user is not null)
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
