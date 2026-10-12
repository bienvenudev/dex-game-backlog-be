using System.ComponentModel.DataAnnotations;

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
    [property: Required]
    [property: StringLength(200)]
    string Title,
    [property: EnumDataType(typeof(Platform))]
    Platform Platform,
    [property: EnumDataType(typeof(GameStatus))]
    GameStatus Status,
    [property: Range(1, 10)]
    int? Rating,
    [property: StringLength(4000)]
    string? Notes,
    [property: StringLength(2048)]
    string? CoverUrl,
    [property: StringLength(2048)]
    string? BackgroundUrl);

public sealed record ObjectiveInput(
    [property: Required]
    [property: StringLength(200)]
    string Label);

public sealed record StatusUpdateInput(
    [property: EnumDataType(typeof(GameStatus))]
    GameStatus Status,
    [property: StringLength(500)]
    string? Comment);

public sealed record StatusUpdateResponse(
    GameSummaryResponse Game,
    bool HasOutstandingObjectives);

public sealed record ObjectiveUpdateInput(
    [property: Required]
    [property: StringLength(200)]
    string Label);

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