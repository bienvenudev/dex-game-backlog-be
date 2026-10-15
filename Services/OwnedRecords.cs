using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Services;

// Every lookup filters by owner in SQL, so another user's record looks exactly like
// a missing one. Controllers turn a null or false result into 404.
public static class OwnedRecords
{
    public static Task<Game?> FindGameAsync(
        AppDbContext dbContext,
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Games
            .SingleOrDefaultAsync(
                game => game.Id == gameId && game.UserId == userId,
                cancellationToken);

    public static Task<bool> GameExistsAsync(
        AppDbContext dbContext,
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Games
            .AsNoTracking()
            .AnyAsync(game => game.Id == gameId && game.UserId == userId, cancellationToken);

    public static Task<Objective?> FindObjectiveAsync(
        AppDbContext dbContext,
        Guid gameId,
        Guid objectiveId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Objectives
            .SingleOrDefaultAsync(
                objective => objective.Id == objectiveId &&
                             objective.GameId == gameId &&
                             dbContext.Games.Any(game => game.Id == objective.GameId &&
                                                         game.UserId == userId),
                cancellationToken);
}
