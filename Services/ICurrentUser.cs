namespace DexGameBacklog.Api.Services;

public interface ICurrentUser
{
    Guid UserId { get; }
}