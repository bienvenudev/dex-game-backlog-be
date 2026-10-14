using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Services;

// Checked before a write so the API can answer 409 with a clear message. The unique
// indexes in the database remain the last line of defence.
public static class UniquenessChecks
{
    // Pass the game's own id when renaming it, so the game doesn't clash with itself.
    public static Task<bool> IsGameTitleTakenAsync(
        AppDbContext dbContext,
        Guid userId,
        string title,
        Platform platform,
        Guid? exceptGameId,
        CancellationToken cancellationToken)
    {
        var games = dbContext.Games
            .AsNoTracking()
            .Where(game => game.UserId == userId &&
                           game.Title.ToLower() == title.ToLower() &&
                           game.Platform == platform);

        if (exceptGameId is { } gameId)
        {
            games = games.Where(game => game.Id != gameId);
        }

        return games.AnyAsync(cancellationToken);
    }

    // Pass the objective's own id when relabelling it, for the same reason.
    public static Task<bool> IsObjectiveLabelTakenAsync(
        AppDbContext dbContext,
        Guid gameId,
        string label,
        Guid? exceptObjectiveId,
        CancellationToken cancellationToken)
    {
        var objectives = dbContext.Objectives
            .AsNoTracking()
            .Where(objective => objective.GameId == gameId &&
                                objective.Label.ToLower() == label.ToLower());

        if (exceptObjectiveId is { } objectiveId)
        {
            objectives = objectives.Where(objective => objective.Id != objectiveId);
        }

        return objectives.AnyAsync(cancellationToken);
    }
}
