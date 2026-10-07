using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                    (Platform)item.Game.Platform,
                    (GameStatus)item.Game.Status,
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
}
