using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<TokenResponse> Register(RegisterRequest request)
    {
        var user = authService.Register(request.Email, request.Password);
        if (user is null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        return StatusCode(StatusCodes.Status201Created, authService.CreateTokens(user));
    }

    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> Login(LoginRequest request)
    {
        var user = authService.Login(request.Email, request.Password);
        return user is null
            ? Unauthorized(new { message = "Email or password is incorrect." })
            : Ok(authService.CreateTokens(user));
    }

    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> Refresh(RefreshRequest request)
    {
        var tokens = authService.Refresh(request.RefreshToken);
        return tokens is null
            ? Unauthorized(new { message = "Refresh token is invalid or expired." })
            : Ok(tokens);
    }
}
