using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using ContractPlatform = DexGameBacklog.Api.Contracts.Platform;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;
using DomainPlatform = DexGameBacklog.Api.Domain.Platform;

namespace DexGameBacklog.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GamesController(
    AppDbContext dbContext,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GameSummaryResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var games = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new
            {
                Game = game,
                Total = dbContext.Objectives.Count(objective => objective.GameId == game.Id),
                Completed = dbContext.Objectives.Count(objective => objective.GameId == game.Id &&
                                                                    objective.Completed)
            })
            .ToListAsync(cancellationToken);

        var response = games
            .Select(item =>
            {
                return new GameSummaryResponse(
                    item.Game.Id,
                    item.Game.Title,
                    (ContractPlatform)item.Game.Platform,
                    (ContractGameStatus)item.Game.Status,
                    item.Game.Rating,
                    item.Game.Notes,
                    item.Game.CoverUrl,
                    item.Game.BackgroundUrl,
                    item.Game.StartedAt,
                    item.Game.FinishedAt,
                    item.Game.CreatedAt,
                    item.Game.UpdatedAt,
                    ProgressCalculator.Calculate(item.Completed, item.Total));
            })
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<GameSummaryResponse>> Create(
        [FromBody] GameInput input,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var title = input.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { message = "Title is required." });
        }

        if (title.Length > 200 ||
            !InputValidation.IsValidPlatform(input.Platform) ||
            !InputValidation.IsValidGameStatus(input.Status) ||
            !InputValidation.IsValidText(input.Notes, 4000) ||
            !InputValidation.IsValidText(input.CoverUrl, 2048) ||
            !InputValidation.IsValidText(input.BackgroundUrl, 2048))
        {
            return BadRequest(new
            {
                message = "One or more game fields are invalid or too long."
            });
        }

        if (input.Rating is not null &&
            (input.Rating < 1 || input.Rating > 10))
        {
            return BadRequest(new
            {
                message = "Rating must be a whole number from 1 to 10."
            });
        }

        if (input.Status == ContractGameStatus.UNPLAYED &&
            input.Rating is not null)
        {
            return BadRequest(new
            {
                message = "An unplayed game cannot have a rating."
            });
        }

        var isDuplicate = await dbContext.Games
            .AsNoTracking()
            .AnyAsync(g => g.UserId == userId &&
                           g.Title.ToLower() == title.ToLower() &&
                           g.Platform == (DomainPlatform)input.Platform,
                cancellationToken);

        if (isDuplicate)
        {
            return Conflict(new
            {
                message = $"You already have '{title}' on this platform in your backlog."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var newGame = new Game
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Platform = (DomainPlatform)input.Platform,
            Rating = input.Rating,
            Notes = input.Notes,
            CoverUrl = input.CoverUrl,
            BackgroundUrl = input.BackgroundUrl,
            Status = (DomainGameStatus)input.Status,

            CreatedAt = now,
            UpdatedAt = now,
            StartedAt = input.Status is ContractGameStatus.PLAYING or
                ContractGameStatus.FINISHED
                ? now
                : null,
            FinishedAt = input.Status == ContractGameStatus.FINISHED
                ? now
                : null
        };

        dbContext.Games.Add(newGame);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GameSummaryResponse(
            newGame.Id,
            newGame.Title,
            (ContractPlatform)newGame.Platform,
            (ContractGameStatus)newGame.Status,
            newGame.Rating,
            newGame.Notes,
            newGame.CoverUrl,
            newGame.BackgroundUrl,
            newGame.StartedAt,
            newGame.FinishedAt,
            newGame.CreatedAt,
            newGame.UpdatedAt,
            new ProgressResponse(Completed: 0, Total: 0, Percentage: null)
        );

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{gameId:guid}")]
    public async Task<ActionResult<GameDetailResponse>> GetById(
        Guid gameId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var game = await dbContext.Games
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == gameId &&
                             candidate.UserId == userId,
                cancellationToken);

        if (game is null)
        {
            return NotFound(new { message = "Game not found." });
        }

        var objectives = await dbContext.Objectives
            .AsNoTracking()
            .Where(objective => objective.GameId == game.Id)
            .OrderBy(objective => objective.Position)
            .ToListAsync(cancellationToken);

        var completed = objectives.Count(objective => objective.Completed);
        var total = objectives.Count;

        var response = new GameDetailResponse(
            game.Id,
            game.Title,
            (ContractPlatform)game.Platform,
            (ContractGameStatus)game.Status,
            game.Rating,
            game.Notes,
            game.CoverUrl,
            game.BackgroundUrl,
            game.StartedAt,
            game.FinishedAt,
            game.CreatedAt,
            game.UpdatedAt,
            objectives.Select(objective => new ObjectiveResponse(
                objective.Id,
                objective.GameId,
                objective.Label,
                objective.Completed,
                objective.Position,
                objective.CompletedAt,
                objective.CreatedAt,
                objective.UpdatedAt)).ToList(),
            ProgressCalculator.Calculate(completed, total));

        return Ok(response);
    }

    [HttpGet("{gameId:guid}/progress")]
    public async Task<ActionResult<ProgressResponse>> GetProgress(
        Guid gameId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var gameExists = await dbContext.Games
            .AsNoTracking()
            .AnyAsync(game => game.Id == gameId && game.UserId == userId, cancellationToken);

        if (!gameExists)
        {
            return NotFound(new { message = "Game not found." });
        }

        var progress = await dbContext.Objectives
            .AsNoTracking()
            .Where(objective => objective.GameId == gameId)
            .GroupBy(objective => objective.GameId)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(objective => objective.Completed)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return Ok(ProgressCalculator.Calculate(progress?.Completed ?? 0, progress?.Total ?? 0));
    }

    [HttpGet("{gameId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryResponse>>> GetStatusHistory(
        Guid gameId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var gameExists = await dbContext.Games
            .AsNoTracking()
            .AnyAsync(game => game.Id == gameId && game.UserId == userId, cancellationToken);

        if (!gameExists)
        {
            return NotFound(new { message = "Game not found." });
        }

        var history = await dbContext.GameStatusHistories
            .AsNoTracking()
            .Where(entry => entry.GameId == gameId)
            .OrderBy(entry => entry.ChangedAt)
            .Select(entry => new StatusHistoryResponse(
                entry.Id,
                entry.GameId,
                (ContractGameStatus)entry.PreviousStatus,
                (ContractGameStatus)entry.NewStatus,
                entry.ChangedBy,
                entry.ChangedAt,
                entry.Comment))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    [HttpPut("{gameId:guid}")]
    public async Task<ActionResult<GameSummaryResponse>> Update(
        Guid gameId,
        [FromBody] GameInput input,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var title = input.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { message = "Title is required." });
        }

        if (title.Length > 200 ||
            !InputValidation.IsValidPlatform(input.Platform) ||
            !InputValidation.IsValidGameStatus(input.Status) ||
            !InputValidation.IsValidText(input.Notes, 4000) ||
            !InputValidation.IsValidText(input.CoverUrl, 2048) ||
            !InputValidation.IsValidText(input.BackgroundUrl, 2048))
        {
            return BadRequest(new
            {
                message = "One or more game fields are invalid or too long."
            });
        }

        if (input.Rating is not null &&
            (input.Rating < 1 || input.Rating > 10))
        {
            return BadRequest(new
            {
                message = "Rating must be a whole number from 1 to 10."
            });
        }

        if (input.Status == ContractGameStatus.UNPLAYED &&
            input.Rating is not null)
        {
            return BadRequest(new
            {
                message = "An unplayed game cannot have a rating."
            });
        }

        var game = await dbContext.Games
            .SingleOrDefaultAsync(
                candidate => candidate.Id == gameId &&
                             candidate.UserId == userId,
                cancellationToken);

        if (game is null)
        {
            return NotFound(new { message = "Game not found." });
        }

        if (game.Status != (DomainGameStatus)input.Status)
        {
            return Conflict(new
            {
                message = "Use the status endpoint to change a game's status."
            });
        }

        var isDuplicate = await dbContext.Games
            .AsNoTracking()
            .AnyAsync(candidate => candidate.UserId == userId &&
                                   candidate.Id != gameId &&
                                   candidate.Title.ToLower() == title.ToLower() &&
                                   candidate.Platform == (DomainPlatform)input.Platform,
                cancellationToken);

        if (isDuplicate)
        {
            return Conflict(new
            {
                message = $"You already have '{title}' on this platform in your backlog."
            });
        }

        game.Title = title;
        game.Platform = (DomainPlatform)input.Platform;
        game.Rating = input.Status == ContractGameStatus.UNPLAYED
            ? null
            : input.Rating;
        game.Notes = input.Notes;
        game.CoverUrl = input.CoverUrl;
        game.BackgroundUrl = input.BackgroundUrl;
        game.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var progress = await dbContext.Objectives
            .Where(objective => objective.GameId == game.Id)
            .GroupBy(objective => objective.GameId)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(objective => objective.Completed)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var total = progress?.Total ?? 0;
        var completed = progress?.Completed ?? 0;

        return Ok(new GameSummaryResponse(
            game.Id,
            game.Title,
            (ContractPlatform)game.Platform,
            (ContractGameStatus)game.Status,
            game.Rating,
            game.Notes,
            game.CoverUrl,
            game.BackgroundUrl,
            game.StartedAt,
            game.FinishedAt,
            game.CreatedAt,
            game.UpdatedAt,
            ProgressCalculator.Calculate(completed, total)));
    }

    [HttpDelete("{gameId:guid}")]
    public async Task<IActionResult> Delete(
        Guid gameId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var game = await dbContext.Games
            .SingleOrDefaultAsync(
                candidate => candidate.Id == gameId &&
                             candidate.UserId == userId,
                cancellationToken);

        if (game is null)
        {
            return NotFound(new { message = "Game not found." });
        }

        dbContext.Games.Remove(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{gameId:guid}/status")]
    public async Task<ActionResult<StatusUpdateResponse>> UpdateStatus(
        Guid gameId,
        [FromBody] StatusUpdateInput input,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var game = await dbContext.Games
            .SingleOrDefaultAsync(
                candidate => candidate.Id == gameId &&
                             candidate.UserId == userId,
                cancellationToken);

        if (game is null)
        {
            return NotFound(new { message = "Game not found." });
        }

        var requestedStatus = (DomainGameStatus)input.Status;

        if (!InputValidation.IsValidGameStatus(input.Status))
        {
            return BadRequest(new { message = "Status is invalid." });
        }

        if (game.Status == requestedStatus)
        {
            return Conflict(new
            {
                message = "The game already has this status."
            });
        }

        if (!IsAllowedTransition(game.Status, requestedStatus))
        {
            return Conflict(new
            {
                message = "This status transition is not allowed."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var previousStatus = game.Status;
        game.Status = requestedStatus;

        if (requestedStatus == DomainGameStatus.PLAYING &&
            game.StartedAt is null)
        {
            game.StartedAt = now;
        }

        if (requestedStatus == DomainGameStatus.FINISHED)
        {
            game.FinishedAt = now;
        }

        if (requestedStatus == DomainGameStatus.UNPLAYED)
        {
            game.Rating = null;
        }

        game.UpdatedAt = now;

        var history = new GameStatusHistory
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            PreviousStatus = previousStatus,
            NewStatus = requestedStatus,
            ChangedBy = userId,
            ChangedAt = now,
            Comment = input.Comment?.Trim()
        };

        var outstandingObjectives = await dbContext.Objectives
            .AsNoTracking()
            .AnyAsync(
                objective => objective.GameId == gameId &&
                             !objective.Completed,
                cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        dbContext.GameStatusHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var progress = await dbContext.Objectives
            .AsNoTracking()
            .Where(objective => objective.GameId == game.Id)
            .GroupBy(objective => objective.GameId)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(objective => objective.Completed)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var total = progress?.Total ?? 0;
        var completed = progress?.Completed ?? 0;

        var gameResponse = new GameSummaryResponse(
            game.Id,
            game.Title,
            (ContractPlatform)game.Platform,
            (ContractGameStatus)game.Status,
            game.Rating,
            game.Notes,
            game.CoverUrl,
            game.BackgroundUrl,
            game.StartedAt,
            game.FinishedAt,
            game.CreatedAt,
            game.UpdatedAt,
            ProgressCalculator.Calculate(completed, total));

        return Ok(new StatusUpdateResponse(gameResponse, outstandingObjectives));
    }

    private static bool IsAllowedTransition(
        DomainGameStatus currentStatus,
        DomainGameStatus requestedStatus)
    {
        return currentStatus switch
        {
            DomainGameStatus.UNPLAYED =>
                requestedStatus is DomainGameStatus.PLAYING or
                    DomainGameStatus.FINISHED or
                    DomainGameStatus.ABANDONED,
            DomainGameStatus.PLAYING =>
                requestedStatus is DomainGameStatus.FINISHED or
                    DomainGameStatus.ABANDONED or
                    DomainGameStatus.UNPLAYED,
            DomainGameStatus.FINISHED =>
                requestedStatus is DomainGameStatus.PLAYING or
                    DomainGameStatus.ABANDONED,
            DomainGameStatus.ABANDONED =>
                requestedStatus is DomainGameStatus.PLAYING or
                    DomainGameStatus.FINISHED,
            _ => false
        };
    }
}