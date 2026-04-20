using FluentValidation;
using Jobuler.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobuler.Api.Controllers;

[ApiController]
[Route("auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var userId = await _mediator.Send(
            new RegisterCommand(req.Email, req.DisplayName, req.Password, req.PreferredLocale ?? "he"), ct);
        return CreatedAtAction(nameof(Register), new { userId });
    }

    /// <summary>Login and receive access + refresh tokens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(req.Email, req.Password), ct);
        return Ok(result);
    }

    /// <summary>Exchange a valid refresh token for a new token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(req.RefreshToken), ct);
        return Ok(result);
    }

    /// <summary>Revoke the current user's refresh token (logout).</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req, CancellationToken ct)
    {
        await _mediator.Send(new RevokeTokenCommand(req.RefreshToken), ct);
        return NoContent();
    }
}

public record RegisterRequest(string Email, string DisplayName, string Password, string? PreferredLocale);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
