using DexGameBacklog.Api.Contracts;
using DexGameBacklog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DexGameBacklog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.AuthenticateAsync(
            request,
            cancellationToken);

        if (response is null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(response);
    }
}