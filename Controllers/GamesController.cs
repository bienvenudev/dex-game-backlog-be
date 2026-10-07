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
                var percentage = item.Total == 0
                    ? null
                    : (int?)Math.Round(
                        (double)item.Completed / item.Total * 100,
                        MidpointRounding.AwayFromZero);

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
                    new ProgressResponse(item.Completed, item.Total, percentage));
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
}
