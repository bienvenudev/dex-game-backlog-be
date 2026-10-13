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
        [FromQuery] GameQueryParameters query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1 ||
            query.PageSize is < 1 or > 100 ||
            query.MinRating is < 1 or > 10 ||
            query.MaxRating is < 1 or > 10 ||
            (query.MinRating is not null &&
             query.MaxRating is not null &&
             query.MinRating > query.MaxRating) ||
            (query.Platform is not null &&
             !InputValidation.IsValidPlatform(query.Platform.Value)) ||
            (query.Status is not null &&
             !InputValidation.IsValidGameStatus(query.Status.Value)) ||
            !IsSupportedSort(query.SortBy) ||
            !IsSupportedSortDirection(query.SortDirection))
        {
            return BadRequest(new
            {
                message = "One or more game list query parameters are invalid."
            });
        }

        var userId = currentUser.UserId;

        var gamesQuery = dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            gamesQuery = gamesQuery.Where(game =>
                EF.Functions.ILike(game.Title, $"%{search}%"));
        }

        if (query.Platform is not null)
        {
            gamesQuery = gamesQuery.Where(game =>
                game.Platform == (DomainPlatform)query.Platform.Value);
        }

        if (query.Status is not null)
        {
            gamesQuery = gamesQuery.Where(game =>
                game.Status == (DomainGameStatus)query.Status.Value);
        }

        if (query.MinRating is not null)
        {
            gamesQuery = gamesQuery.Where(game => game.Rating >= query.MinRating);
        }

        if (query.MaxRating is not null)
        {
            gamesQuery = gamesQuery.Where(game => game.Rating <= query.MaxRating);
        }

        var projectedGames = gamesQuery
            .Select(game => new GameListItem
            {
                Game = game,
                Total = dbContext.Objectives.Count(objective => objective.GameId == game.Id),
                Completed = dbContext.Objectives.Count(objective => objective.GameId == game.Id &&
                                                                    objective.Completed)
            });

        if (query.HasObjectives is not null)
        {
            projectedGames = query.HasObjectives.Value
                ? projectedGames.Where(item => item.Total > 0)
                : projectedGames.Where(item => item.Total == 0);
        }

        if (query.FullyComplete is not null)
        {
            projectedGames = query.FullyComplete.Value
                ? projectedGames.Where(item => item.Total > 0 &&
                                               item.Completed == item.Total)
                : projectedGames.Where(item => item.Total == 0 ||
                                               item.Completed < item.Total);
        }

        projectedGames = ApplyGameOrdering(
            projectedGames,
            query.SortBy,
            query.SortDirection);

        var totalCount = await projectedGames.CountAsync(cancellationToken);
        var games = await projectedGames
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", query.Page.ToString());
        Response.Headers.Append("X-Page-Size", query.PageSize.ToString());

        return Ok(response);
    }

    private static IQueryable<GameListItem> ApplyGameOrdering(
        IQueryable<GameListItem> games,
        string sortBy,
        string sortDirection)
    {
        var descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "title" => descending
                ? games.OrderByDescending(item => item.Game.Title).ThenBy(item => item.Game.Id)
                : games.OrderBy(item => item.Game.Title).ThenBy(item => item.Game.Id),
            "rating" => descending
                ? games.OrderByDescending(item => item.Game.Rating).ThenBy(item => item.Game.Id)
                : games.OrderBy(item => item.Game.Rating).ThenBy(item => item.Game.Id),
            "createdat" => descending
                ? games.OrderByDescending(item => item.Game.CreatedAt).ThenBy(item => item.Game.Id)
                : games.OrderBy(item => item.Game.CreatedAt).ThenBy(item => item.Game.Id),
            _ => descending
                ? games.OrderByDescending(item => item.Game.UpdatedAt).ThenBy(item => item.Game.Id)
                : games.OrderBy(item => item.Game.UpdatedAt).ThenBy(item => item.Game.Id)
        };
    }

    private static bool IsSupportedSort(string sortBy) =>
        sortBy.Equals("title", StringComparison.OrdinalIgnoreCase) ||
        sortBy.Equals("rating", StringComparison.OrdinalIgnoreCase) ||
        sortBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase) ||
        sortBy.Equals("updatedAt", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedSortDirection(string sortDirection) =>
        sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
        sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

    private sealed class GameListItem
    {
        public Game Game { get; init; } = null!;
        public int Total { get; init; }
        public int Completed { get; init; }
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

        var objectiveInputs = input.Objectives ?? [];
        var objectiveLabels = objectiveInputs
            .Select(objective => objective?.Label?.Trim() ?? string.Empty)
            .ToList();

        if (objectiveInputs.Any(objective => objective is null) ||
            objectiveLabels.Any(string.IsNullOrWhiteSpace) ||
            objectiveLabels.Any(label => label.Length > 200) ||
            objectiveLabels.Count != objectiveLabels
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count())
        {
            return BadRequest(new
            {
                message = "Objective labels are required, must be at most 200 characters, and must be unique."
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        dbContext.Games.Add(newGame);
        dbContext.Objectives.AddRange(
            objectiveLabels.Select((label, index) => new Objective
            {
                Id = Guid.NewGuid(),
                GameId = newGame.Id,
                Label = label,
                Completed = false,
                Position = index + 1,
                CreatedAt = now,
                UpdatedAt = now
            }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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
            new ProgressResponse(
                Completed: 0,
                Total: objectiveLabels.Count,
                Percentage: objectiveLabels.Count == 0 ? null : 0)
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

        if (!StatusTransitionRules.IsAllowed(game.Status, requestedStatus))
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

}