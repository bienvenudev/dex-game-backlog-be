using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Services;

// Counts a game's objectives in SQL. Loading every objective just to count them
// would be wasteful, and this is the one place the counting query lives.
public static class GameProgressQuery
{
    public static async Task<ProgressResponse> GetAsync(
        AppDbContext dbContext,
        Guid gameId,
        CancellationToken cancellationToken)
    {
        // A game with no objectives produces no row, so the counts default to zero.
        var counts = await dbContext.Objectives
            .AsNoTracking()
            .Where(objective => objective.GameId == gameId)
            .GroupBy(objective => objective.GameId)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(objective => objective.Completed)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return ProgressCalculator.Calculate(counts?.Completed ?? 0, counts?.Total ?? 0);
    }
}
