using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using ContractGameStatus = DexGameBacklog.Api.Contracts.GameStatus;
using DomainGameStatus = DexGameBacklog.Api.Domain.GameStatus;

namespace DexGameBacklog.Api.Services;

public enum StatusChangeOutcome
{
    Changed,
    GameNotFound,
    InvalidStatus,
    AlreadyHasStatus,
    TransitionNotAllowed
}

public sealed record StatusChangeResult(
    StatusChangeOutcome Outcome,
    Game? Game = null,
    bool HasOutstandingObjectives = false);

// Owns the status-change rules: transitions, timestamps, rating reset, history, and
// the published event. Controllers only translate the outcome into an HTTP response.
public sealed class GameStatusService(
    AppDbContext dbContext,
    IGameStatusEventPublisher statusEventPublisher)
{
    public async Task<StatusChangeResult> ChangeStatusAsync(
        Guid userId,
        Guid gameId,
        ContractGameStatus status,
        string? comment,
        CancellationToken cancellationToken)
    {
        var game = await OwnedRecords.FindGameAsync(dbContext, gameId, userId, cancellationToken);

        if (game is null)
        {
            return new StatusChangeResult(StatusChangeOutcome.GameNotFound);
        }

        if (!InputValidation.IsValidGameStatus(status))
        {
            return new StatusChangeResult(StatusChangeOutcome.InvalidStatus);
        }

        var requestedStatus = (DomainGameStatus)status;

        if (game.Status == requestedStatus)
        {
            return new StatusChangeResult(StatusChangeOutcome.AlreadyHasStatus);
        }

        if (!StatusTransitionRules.IsAllowed(game.Status, requestedStatus))
        {
            return new StatusChangeResult(StatusChangeOutcome.TransitionNotAllowed);
        }

        var now = DateTimeOffset.UtcNow;
        var previousStatus = game.Status;
        game.Status = requestedStatus;

        if (requestedStatus == DomainGameStatus.PLAYING &&
            game.StartedAt is null)
        {
            game.StartedAt = now;
        }

        if (requestedStatus == DomainGameStatus.FINISHED)
        {
            game.FinishedAt = now;
        }

        // A rating is only valid on a game that has been played, so returning to
        // UNPLAYED clears it in the same transaction as the status change.
        if (requestedStatus == DomainGameStatus.UNPLAYED)
        {
            game.Rating = null;
        }

        game.UpdatedAt = now;

        var history = new GameStatusHistory
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            PreviousStatus = previousStatus,
            NewStatus = requestedStatus,
            ChangedBy = userId,
            ChangedAt = now,
            Comment = comment?.Trim()
        };

        var hasOutstandingObjectives = await dbContext.Objectives
            .AsNoTracking()
            .AnyAsync(
                objective => objective.GameId == gameId &&
                             !objective.Completed,
                cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        dbContext.GameStatusHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await statusEventPublisher.PublishAsync(
            new GameStatusChangedEvent(
                game.Id,
                userId,
                previousStatus,
                requestedStatus,
                now,
                history.Comment),
            cancellationToken);

        return new StatusChangeResult(
            StatusChangeOutcome.Changed,
            game,
            hasOutstandingObjectives);
    }
}
