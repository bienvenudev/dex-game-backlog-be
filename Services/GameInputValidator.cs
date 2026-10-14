using DexGameBacklog.Api.Contracts;

namespace DexGameBacklog.Api.Services;

// Checks the rules a game must satisfy before it is saved. Returns the first
// problem found, or null when the input is valid, so controllers can turn it
// into a 400 response without repeating the rules.
public static class GameInputValidator
{
    public static string? Validate(GameInput input)
    {
        var title = input.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (title.Length > 200 ||
            !InputValidation.IsValidPlatform(input.Platform) ||
            !InputValidation.IsValidGameStatus(input.Status) ||
            !InputValidation.IsValidText(input.Notes, 4000) ||
            !InputValidation.IsValidText(input.CoverUrl, 2048) ||
            !InputValidation.IsValidText(input.BackgroundUrl, 2048))
        {
            return "One or more game fields are invalid or too long.";
        }

        if (input.Rating is not null &&
            (input.Rating < 1 || input.Rating > 10))
        {
            return "Rating must be a whole number from 1 to 10.";
        }

        if (input.Status == GameStatus.UNPLAYED &&
            input.Rating is not null)
        {
            return "An unplayed game cannot have a rating.";
        }

        return null;
    }
}
