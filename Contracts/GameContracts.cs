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

public sealed record GameInput
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [EnumDataType(typeof(Platform))]
    public Platform Platform { get; init; }

    [EnumDataType(typeof(GameStatus))]
    public GameStatus Status { get; init; }

    [Range(1, 10)]
    public int? Rating { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }

    [StringLength(2048)]
    public string? CoverUrl { get; init; }

    [StringLength(2048)]
    public string? BackgroundUrl { get; init; }
}

public sealed record ObjectiveInput
{
    [Required]
    [StringLength(200)]
    public string Label { get; init; } = string.Empty;
}

public sealed record StatusUpdateInput
{
    [EnumDataType(typeof(GameStatus))]
    public GameStatus Status { get; init; }

    [StringLength(500)]
    public string? Comment { get; init; }
}

public sealed record StatusUpdateResponse(
    GameSummaryResponse Game,
    bool HasOutstandingObjectives);

public sealed record ObjectiveUpdateInput
{
    [Required]
    [StringLength(200)]
    public string Label { get; init; } = string.Empty;
}

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