using DexGameBacklog.Api.Domain;

namespace DexGameBacklog.Api.Services;

public static class StatusTransitionRules
{
    public static bool IsAllowed(GameStatus currentStatus, GameStatus requestedStatus)
    {
        return currentStatus switch
        {
            GameStatus.UNPLAYED =>
                requestedStatus is GameStatus.PLAYING or
                    GameStatus.FINISHED or
                    GameStatus.ABANDONED,
            GameStatus.PLAYING =>
                requestedStatus is GameStatus.FINISHED or
                    GameStatus.ABANDONED or
                    GameStatus.UNPLAYED,
            GameStatus.FINISHED =>
                requestedStatus is GameStatus.PLAYING or
                    GameStatus.ABANDONED,
            GameStatus.ABANDONED =>
                requestedStatus is GameStatus.PLAYING or
                    GameStatus.FINISHED,
            _ => false
        };
    }
}
