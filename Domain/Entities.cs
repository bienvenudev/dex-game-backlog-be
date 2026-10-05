namespace DexGameBacklog.Api.Domain;

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

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class Game
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public GameStatus Status { get; set; }
    public int? Rating { get; set; }
    public string? Notes { get; set; }
    public string? CoverUrl { get; set; }
    public string? BackgroundUrl { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Objective
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public int Position { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class GameStatusHistory
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public GameStatus PreviousStatus { get; set; }
    public GameStatus NewStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? Comment { get; set; }
}
