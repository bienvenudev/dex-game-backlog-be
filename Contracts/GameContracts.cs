namespace DexGameBacklog.Api.Contracts;

public enum Platform
{
    STEAM,
    EPIC,
    GOG,
    XBOX,
    PLAYSTATION,
    OTHER
}

public enum GameStatus
{
    UNPLAYED,
    PLAYING,
    FINISHED,
    ABANDONED
}

public sealed record ProgressResponse(
    int Completed,
    int Total,
    int? Percentage);

public sealed record ObjectiveResponse(
    Guid Id,
    Guid GameId,
    string Label,
    bool Completed,
    int Position,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record GameSummaryResponse(
    Guid Id,
    string Title,
    Platform Platform,
    GameStatus Status,
    int? Rating,
    string? Notes,
    string? CoverUrl,
    string? BackgroundUrl,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ProgressResponse Progress);

public sealed record GameDetailResponse(
    Guid Id,
    string Title,
    Platform Platform,
    GameStatus Status,
    int? Rating,
    string? Notes,
    string? CoverUrl,
    string? BackgroundUrl,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ObjectiveResponse> Objectives,
    ProgressResponse Progress);

public sealed record GameInput(
    string Title,
    Platform Platform,
    GameStatus Status,
    int? Rating,
    string? Notes,
    string? CoverUrl,
    string? BackgroundUrl);

public sealed record ObjectiveInput(string Label);

public sealed record StatusUpdateInput(
    GameStatus Status,
    string? Comment);

public sealed record ObjectiveUpdateInput(string Label);

public sealed record ObjectiveOrderInput(
    IReadOnlyList<Guid> ObjectiveIds);

public sealed record StatusHistoryResponse(
    Guid Id,
    Guid GameId,
    GameStatus PreviousStatus,
    GameStatus NewStatus,
    Guid ChangedBy,
    DateTimeOffset ChangedAt,
    string? Comment);

public sealed record DashboardSummaryResponse(
    int TotalGames,
    IReadOnlyDictionary<GameStatus, int> GamesByStatus,
    int FullyCompleteGames,
    int GamesWithNoObjectives,
    double? AverageRating,
    IReadOnlyList<GameSummaryResponse> RecentlyUpdatedGames);
