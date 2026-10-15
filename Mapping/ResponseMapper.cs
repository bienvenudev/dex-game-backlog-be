using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Domain;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using ContractPlatform = DexGameBacklog.Api.Contracts.Platform;

namespace DexGameBacklog.Api.Mapping;

// Converts entities that are already in memory into response shapes. Queries that
// project straight to a DTO in SQL keep their own Select, because EF Core must
// translate that expression.
public static class ResponseMapper
{
    public static GameSummaryResponse ToSummary(Game game, ProgressResponse progress) =>
        new(
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
            progress);

    public static ObjectiveResponse ToObjective(Objective objective) =>
        new(
            objective.Id,
            objective.GameId,
            objective.Label,
            objective.Completed,
            objective.Position,
            objective.CompletedAt,
            objective.CreatedAt,
            objective.UpdatedAt);
}
