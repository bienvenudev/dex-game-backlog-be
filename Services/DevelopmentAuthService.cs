using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Data;
using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DexGameBacklog.Api.Services;

public sealed class DevelopmentAuthService(
    IConfiguration configuration,
    AppDbContext dbContext) : IAuthService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public async Task<LoginResponse?> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var expectedUsername = configuration["Authentication:Username"]
                               ?? throw new InvalidOperationException(
                                   "Authentication:Username is not configured.");
        var expectedPassword = configuration["Authentication:Password"]
                               ?? throw new InvalidOperationException(
                                   "Authentication:Password is not configured.");

        if (request.Username != expectedUsername ||
            request.Password != expectedPassword)
        {
            return null;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Username == expectedUsername,
                cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = expectedUsername
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = configuration["Jwt:Key"]
                  ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}