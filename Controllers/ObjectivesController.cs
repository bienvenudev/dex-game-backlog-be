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
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ObjectiveResponse>>> Get(
        Guid gameId,
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

        var objectives = await dbContext.Objectives
            .AsNoTracking()
            .Where(objective => objective.GameId == gameId)
            .OrderBy(objective => objective.Position)
            .Select(objective => new ObjectiveResponse(
                objective.Id,
                objective.GameId,
                objective.Label,
                objective.Completed,
                objective.Position,
                objective.CompletedAt,
                objective.CreatedAt,
                objective.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(objectives);
    }

    [HttpGet("{objectiveId:guid}")]
    public async Task<ActionResult<ObjectiveResponse>> GetById(
        Guid gameId,
        Guid objectiveId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var objective = await dbContext.Objectives
            .AsNoTracking()
            .Where(candidate => candidate.Id == objectiveId &&
                                candidate.GameId == gameId &&
                                dbContext.Games.Any(game => game.Id == candidate.GameId &&
                                                            game.UserId == userId))
            .Select(candidate => new ObjectiveResponse(
                candidate.Id,
                candidate.GameId,
                candidate.Label,
                candidate.Completed,
                candidate.Position,
                candidate.CompletedAt,
                candidate.CreatedAt,
                candidate.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        return Ok(objective);
    }

    [HttpPut("{objectiveId:guid}")]
    public async Task<ActionResult<ObjectiveResponse>> Update(
        Guid gameId,
        Guid objectiveId,
        ObjectiveUpdateInput input,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var label = input.Label?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(label))
        {
            return BadRequest(new { message = "Label is required." });
        }

        var objective = await dbContext.Objectives
            .SingleOrDefaultAsync(
                candidate => candidate.Id == objectiveId &&
                             candidate.GameId == gameId &&
                             dbContext.Games.Any(game => game.Id == candidate.GameId &&
                                                         game.UserId == userId),
                cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        var duplicate = await dbContext.Objectives
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.GameId == gameId &&
                             candidate.Id != objectiveId &&
                             candidate.Label.ToLower() == label.ToLower(),
                cancellationToken);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "An objective with this label already exists."
            });
        }

        objective.Label = label;
        objective.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ObjectiveResponse(
            objective.Id,
            objective.GameId,
            objective.Label,
            objective.Completed,
            objective.Position,
            objective.CompletedAt,
            objective.CreatedAt,
            objective.UpdatedAt));
    }

    [HttpPatch("{objectiveId:guid}/complete")]
    public async Task<ActionResult<ObjectiveResponse>> Complete(
        Guid gameId,
        Guid objectiveId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var objective = await dbContext.Objectives
            .SingleOrDefaultAsync(
                candidate => candidate.Id == objectiveId &&
                             candidate.GameId == gameId &&
                             dbContext.Games.Any(game => game.Id == candidate.GameId &&
                                                         game.UserId == userId),
                cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        var now = DateTimeOffset.UtcNow;
        objective.Completed = true;
        objective.CompletedAt ??= now;
        objective.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ObjectiveResponse(
            objective.Id,
            objective.GameId,
            objective.Label,
            objective.Completed,
            objective.Position,
            objective.CompletedAt,
            objective.CreatedAt,
            objective.UpdatedAt));
    }

    [HttpPatch("{objectiveId:guid}/reopen")]
    public async Task<ActionResult<ObjectiveResponse>> Reopen(Guid gameId, Guid objectiveId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var objective = await dbContext.Objectives
            .SingleOrDefaultAsync(candidate => candidate.Id == objectiveId && candidate.GameId == gameId &&
                                               dbContext.Games.Any(game =>
                                                   game.Id == candidate.GameId && game.UserId == userId),
                cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        var now = DateTimeOffset.UtcNow;
        objective.Completed = false;
        objective.CompletedAt = null;
        objective.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ObjectiveResponse(
            objective.Id,
            objective.GameId,
            objective.Label,
            objective.Completed,
            objective.Position,
            objective.CompletedAt,
            objective.CreatedAt,
            objective.UpdatedAt));
    }

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