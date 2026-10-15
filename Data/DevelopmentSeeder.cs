using System.Globalization;
using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Data;

// Development-only sample data, copied from the frontend's old mock data.
// Old string ids ("1".."12") only exist to link games to their objectives and
// to the owner list below; the database uses fresh GUIDs.
public static class DevelopmentSeeder
{
    private static readonly Dictionary<string, string[]> GameIdsByUser = new()
    {
        ["ben"] = ["1", "2", "3", "4", "5", "6", "7", "8"],
        ["jane"] = ["9", "10", "11", "12"]
    };

    public static async Task SeedAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var configuredUsernames = configuration
            .GetSection("Authentication:Users")
            .GetChildren()
            .Select(section => section["Username"])
            .ToHashSet();

        foreach (var (username, oldGameIds) in GameIdsByUser)
        {
            // Only seed accounts that exist in configuration, so a missing user
            // secret doesn't create an orphaned owner.
            if (!configuredUsernames.Contains(username))
            {
                continue;
            }

            var user = await dbContext.Users
                .SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
            if (user is null)
            {
                user = new User { Id = Guid.NewGuid(), Username = username };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Skip users who already have games, so restarting the app never duplicates data.
            var hasGames = await dbContext.Games
                .AnyAsync(game => game.UserId == user.Id, cancellationToken);
            if (hasGames)
            {
                continue;
            }

            var newIdsByOldId = new Dictionary<string, Guid>();
            foreach (var seedGame in SeedGames.Where(game => oldGameIds.Contains(game.OldId)))
            {
                var gameId = Guid.NewGuid();
                newIdsByOldId[seedGame.OldId] = gameId;

                dbContext.Games.Add(new Game
                {
                    Id = gameId,
                    UserId = user.Id,
                    Title = seedGame.Title,
                    Platform = seedGame.Platform,
                    Status = seedGame.Status,
                    Rating = seedGame.Rating,
                    Notes = seedGame.Notes,
                    CoverUrl = seedGame.SteamAppId is { } coverAppId ? SteamCover(coverAppId) : null,
                    BackgroundUrl = seedGame.SteamAppId is { } heroAppId ? SteamHero(heroAppId) : null,
                    StartedAt = seedGame.StartedAt,
                    FinishedAt = seedGame.FinishedAt,
                    CreatedAt = seedGame.CreatedAt,
                    UpdatedAt = seedGame.UpdatedAt
                });
            }

            foreach (var seedObjective in SeedObjectives.Where(objective => newIdsByOldId.ContainsKey(objective.OldGameId)))
            {
                dbContext.Objectives.Add(new Objective
                {
                    Id = Guid.NewGuid(),
                    GameId = newIdsByOldId[seedObjective.OldGameId],
                    Label = seedObjective.Label,
                    Completed = seedObjective.CompletedAt is not null,
                    Position = seedObjective.Position,
                    CompletedAt = seedObjective.CompletedAt,
                    CreatedAt = seedObjective.CreatedAt,
                    UpdatedAt = seedObjective.UpdatedAt
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    // Steam's public CDN serves library art per app id. Fine for sample data; not for shipping.
    private static string SteamCover(int appId) =>
        $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900.jpg";

    private static string SteamHero(int appId) =>
        $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_hero.jpg";

    private static DateTimeOffset At(string iso) =>
        DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);

    private sealed record SeedGame(
        string OldId,
        string Title,
        Platform Platform,
        GameStatus Status,
        int? Rating,
        string? Notes,
        int? SteamAppId,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record SeedObjective(
        string OldGameId,
        string Label,
        int Position,
        DateTimeOffset? CompletedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private static readonly SeedGame[] SeedGames =
    [
        new("1", "The Witcher 3: Wild Hunt", Platform.STEAM, GameStatus.PLAYING, 9,
            "Great game, but a bit long.", 292030,
            At("2023-01-01T00:00:00Z"), null, At("2023-01-01T00:00:00Z"), At("2023-01-01T00:00:00Z")),
        new("2", "Hades", Platform.EPIC, GameStatus.FINISHED, 10,
            "Amazing game, loved it!", 1145360,
            At("2023-02-01T00:00:00Z"), At("2023-02-15T00:00:00Z"), At("2023-02-01T00:00:00Z"), At("2023-02-15T00:00:00Z")),
        new("3", "The Legend of Zelda: Breath of the Wild", Platform.OTHER, GameStatus.ABANDONED, 7,
            "Got lost in the open world and never came back.", null,
            At("2023-03-01T00:00:00Z"), null, At("2023-03-01T00:00:00Z"), At("2023-03-20T00:00:00Z")),
        new("4", "Hollow Knight", Platform.GOG, GameStatus.UNPLAYED, null,
            null, 367520,
            null, null, At("2023-04-01T00:00:00Z"), At("2023-04-01T00:00:00Z")),
        new("5", "Red Dead Redemption 2", Platform.PLAYSTATION, GameStatus.PLAYING, 9,
            null, 1174180,
            At("2024-01-10T00:00:00Z"), null, At("2024-01-10T00:00:00Z"), At("2024-02-01T00:00:00Z")),
        new("6", "Portal 2", Platform.STEAM, GameStatus.FINISHED, 10,
            "Perfect length.", 620,
            At("2022-06-01T00:00:00Z"), At("2022-06-05T00:00:00Z"), At("2022-06-01T00:00:00Z"), At("2022-06-05T00:00:00Z")),
        new("7", "Elden Ring", Platform.STEAM, GameStatus.PLAYING, 8,
            "Stuck on Malenia.", 1245620,
            At("2024-03-01T00:00:00Z"), null, At("2024-03-01T00:00:00Z"), At("2024-05-01T00:00:00Z")),
        new("8", "Celeste", Platform.XBOX, GameStatus.UNPLAYED, null,
            null, 504230,
            null, null, At("2024-04-01T00:00:00Z"), At("2024-04-01T00:00:00Z")),
        new("9", "Stardew Valley", Platform.STEAM, GameStatus.PLAYING, 8,
            null, 413150,
            At("2024-02-01T00:00:00Z"), null, At("2024-02-01T00:00:00Z"), At("2024-06-01T00:00:00Z")),
        new("10", "Disco Elysium", Platform.GOG, GameStatus.ABANDONED, 6,
            "Too much reading for my mood at the time.", 632470,
            At("2023-09-01T00:00:00Z"), null, At("2023-09-01T00:00:00Z"), At("2023-10-01T00:00:00Z")),
        new("11", "Baldur's Gate 3", Platform.STEAM, GameStatus.UNPLAYED, null,
            "Waiting for a long weekend.", 1086940,
            null, null, At("2024-07-01T00:00:00Z"), At("2024-07-01T00:00:00Z")),
        new("12", "Sekiro: Shadows Die Twice", Platform.PLAYSTATION, GameStatus.FINISHED, 9,
            null, 814380,
            At("2023-05-01T00:00:00Z"), At("2023-06-15T00:00:00Z"), At("2023-05-01T00:00:00Z"), At("2023-06-15T00:00:00Z"))
    ];

    private static readonly SeedObjective[] SeedObjectives =
    [
        new("1", "Complete the main story", 1, At("2023-01-15T00:00:00Z"),
            At("2023-01-01T00:00:00Z"), At("2023-01-15T00:00:00Z")),
        new("1", "Finish all side quests", 2, null,
            At("2023-01-01T00:00:00Z"), At("2023-01-01T00:00:00Z")),
        new("1", "Collect all Gwent cards", 3, null,
            At("2023-01-01T00:00:00Z"), At("2023-01-01T00:00:00Z")),
        new("2", "Defeat Hades", 1, At("2023-02-10T00:00:00Z"),
            At("2023-02-01T00:00:00Z"), At("2023-02-10T00:00:00Z")),
        new("2", "Unlock all weapons", 2, At("2023-02-12T00:00:00Z"),
            At("2023-02-01T00:00:00Z"), At("2023-02-12T00:00:00Z"))
    ];
}
