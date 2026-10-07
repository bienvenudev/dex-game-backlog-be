using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/games/{gameId:guid}/objectives")]
public class ObjectivesController(
    AppDbContext dbContext,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ObjectiveResponse>> Create(
        Guid gameId,
        ObjectiveInput input,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var gameExists = await dbContext.Games
            .AsNoTracking()
            .AnyAsync(
                game => game.Id == gameId && game.UserId == userId,
                cancellationToken);

        if (!gameExists)
        {
            return NotFound(new { message = "Game not found." });
        }

        var label = input.Label?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(label))
        {
            return BadRequest(new { message = "Label is required." });
        }

        var duplicate = await dbContext.Objectives
            .AsNoTracking()
            .AnyAsync(
                objective => objective.GameId == gameId &&
                             objective.Label.ToLower() == label.ToLower(),
                cancellationToken);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "An objective with this label already exists."
            });
        }

        var nextPosition = await dbContext.Objectives
            .Where(objective => objective.GameId == gameId)
            .Select(objective => (int?)objective.Position)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTimeOffset.UtcNow;
        var objectiveEntity = new Objective
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Label = label,
            Completed = false,
            Position = nextPosition + 1,
            CompletedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Objectives.Add(objectiveEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ObjectiveResponse(
            objectiveEntity.Id,
            objectiveEntity.GameId,
            objectiveEntity.Label,
            objectiveEntity.Completed,
            objectiveEntity.Position,
            objectiveEntity.CompletedAt,
            objectiveEntity.CreatedAt,
            objectiveEntity.UpdatedAt);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}