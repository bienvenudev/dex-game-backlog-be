using System.Security.Claims;

namespace DexGameBacklog.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(
                    "The request is unauthenticated.");
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "The user ID claim is missing from the token.");
            }

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new FormatException(
                    "The authenticated user ID claim is malformed.");
            }

            return parsedUserId;
        }
    }
}