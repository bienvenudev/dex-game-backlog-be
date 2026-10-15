using DexGameBacklog.Api.Contracts;

namespace DexGameBacklog.Api.Services;

public static class InputValidation
{
    public static bool IsValidPlatform(Platform platform) =>
        Enum.IsDefined(platform);

    public static bool IsValidGameStatus(GameStatus status) =>
        Enum.IsDefined(status);

    public static bool IsValidText(string? value, int maximumLength) =>
        value is null || value.Trim().Length <= maximumLength;
}