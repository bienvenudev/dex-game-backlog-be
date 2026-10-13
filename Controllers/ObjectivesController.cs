using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Mapping;
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

        if (!InputValidation.IsValidText(label, 200))
        {
            return BadRequest(new { message = "Label cannot exceed 200 characters." });
        }

        var objective = await FindOwnedObjective(gameId, objectiveId, userId, cancellationToken);

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

        return Ok(ResponseMapper.ToObjective(objective));
    }

    [HttpPatch("{objectiveId:guid}/complete")]
    public async Task<ActionResult<ObjectiveResponse>> Complete(
        Guid gameId,
        Guid objectiveId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var objective = await FindOwnedObjective(gameId, objectiveId, userId, cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        var now = DateTimeOffset.UtcNow;
        objective.Completed = true;
        objective.CompletedAt ??= now;
        objective.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ResponseMapper.ToObjective(objective));
    }

    [HttpPatch("{objectiveId:guid}/reopen")]
    public async Task<ActionResult<ObjectiveResponse>> Reopen(Guid gameId, Guid objectiveId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var objective = await FindOwnedObjective(gameId, objectiveId, userId, cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        var now = DateTimeOffset.UtcNow;
        objective.Completed = false;
        objective.CompletedAt = null;
        objective.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ResponseMapper.ToObjective(objective));
    }

    [HttpDelete("{objectiveId:guid}")]
    public async Task<IActionResult> Delete(
        Guid gameId,
        Guid objectiveId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var objective = await FindOwnedObjective(gameId, objectiveId, userId, cancellationToken);

        if (objective is null)
        {
            return NotFound(new { message = "Objective not found." });
        }

        dbContext.Objectives.Remove(objective);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("order")]
    public async Task<ActionResult<IReadOnlyList<ObjectiveResponse>>> Reorder(
        Guid gameId,
        ObjectiveOrderInput input,
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
            .Where(objective => objective.GameId == gameId)
            .ToListAsync(cancellationToken);

        var requestedIds = input.ObjectiveIds ?? [];
        var hasDuplicateIds = requestedIds.Count != requestedIds.Distinct().Count();
        var containsExactlyTheSameObjectives =
            requestedIds.Count == objectives.Count &&
            requestedIds.ToHashSet().SetEquals(objectives.Select(objective => objective.Id));

        if (hasDuplicateIds || !containsExactlyTheSameObjectives)
        {
            return BadRequest(new
            {
                message = "ObjectiveIds must contain every objective for the game exactly once."
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var objectivesById = objectives.ToDictionary(objective => objective.Id);

        for (var index = 0; index < requestedIds.Count; index++)
        {
            var objective = objectivesById[requestedIds[index]];
            objective.Position = index + 1;
            objective.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = requestedIds
            .Select(objectiveId => objectivesById[objectiveId])
            .Select(ResponseMapper.ToObjective)
            .ToList();

        return Ok(response);
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

        if (!InputValidation.IsValidText(label, 200))
        {
            return BadRequest(new { message = "Label cannot exceed 200 characters." });
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

        var response = ResponseMapper.ToObjective(objectiveEntity);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    private Task<Objective?> FindOwnedObjective(Guid gameId, Guid objectiveId, Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Objectives
            .SingleOrDefaultAsync(
                candidate => candidate.Id == objectiveId &&
                             candidate.GameId == gameId &&
                             dbContext.Games.Any(game => game.Id == candidate.GameId &&
                                                         game.UserId == userId),
                cancellationToken);
    }
}