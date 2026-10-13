using DexGameBacklog.Api.Domain;

namespace DexGameBacklog.Api.Services;

public sealed record GameStatusChangedEvent(
    Guid GameId,
    Guid UserId,
    GameStatus PreviousStatus,
    GameStatus NewStatus,
    DateTimeOffset ChangedAt,
    string? Comment);

public interface IGameStatusEventPublisher
{
    Task PublishAsync(
        GameStatusChangedEvent statusChangedEvent,
        CancellationToken cancellationToken);
}

public sealed class NoOpGameStatusEventPublisher : IGameStatusEventPublisher
{
    public Task PublishAsync(
        GameStatusChangedEvent statusChangedEvent,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
