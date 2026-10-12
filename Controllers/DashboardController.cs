using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;

namespace DexGameBacklog.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(
    AppDbContext dbContext,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
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
                Completed = dbContext.Objectives.Count(objective =>
                    objective.GameId == game.Id && objective.Completed)
            })
            .OrderByDescending(item => item.Game.UpdatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var statusCounts = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .GroupBy(game => game.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => (ContractGameStatus)item.Status, item => item.Count, cancellationToken);

        var averageRating = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId && game.Rating.HasValue)
            .AverageAsync(game => (double?)game.Rating!.Value, cancellationToken);

        var fullyCompleteGames = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new
            {
                Total = dbContext.Objectives.Count(objective => objective.GameId == game.Id),
                Completed = dbContext.Objectives.Count(objective =>
                    objective.GameId == game.Id && objective.Completed)
            })
            .CountAsync(item => item.Total > 0 && item.Completed == item.Total, cancellationToken);

        var gamesWithNoObjectives = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new
            {
                Total = dbContext.Objectives.Count(objective => objective.GameId == game.Id)
            })
            .CountAsync(item => item.Total == 0, cancellationToken);

        var response = new DashboardSummaryResponse(
            TotalGames: await dbContext.Games.CountAsync(game => game.UserId == userId, cancellationToken),
            GamesByStatus: statusCounts,
            FullyCompleteGames: fullyCompleteGames,
            GamesWithNoObjectives: gamesWithNoObjectives,
            AverageRating: averageRating,
            RecentlyUpdatedGames: games
                .Select(item => new GameSummaryResponse(
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
                    ProgressCalculator.Calculate(item.Completed, item.Total)))
                .ToList());

        return Ok(response);
    }
}