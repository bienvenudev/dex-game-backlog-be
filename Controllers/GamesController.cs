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
                Total = dbContext.Objectives.Count(
                    objective => objective.GameId == game.Id),
                Completed = dbContext.Objectives.Count(
                    objective => objective.GameId == game.Id &&
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
}
