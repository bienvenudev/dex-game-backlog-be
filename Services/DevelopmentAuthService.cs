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

        // where is the expectedusername and expectedpassword coming from and how are we sure they are right so that in the next line we are saying != request.Username?
        
        if (request.Username != expectedUsername ||
            request.Password != expectedPassword)
        {
            return null;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Username == expectedUsername,
                cancellationToken); // does this mean the username should be unique bcs in the next line if the user is null then we create them but what about 2 people with the same username?

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = expectedUsername
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken); // why are we passing cancellation token here?
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        }; // wth is a claim and why are we creating 2 claims here?

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured."); // where does the key come from? is it the thing in my appsettings.development.json?
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
