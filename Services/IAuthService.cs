using DexGameBacklog.Api.Contracts;

namespace DexGameBacklog.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
