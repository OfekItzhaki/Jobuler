using FluentValidation;
using Jobuler.Application.Auth;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;

    public ResetPasswordCommandHandler(AppDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task Handle(ResetPasswordCommand req, CancellationToken ct)
    {
        if (req.NewPassword.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters.");

        var tokenHash = _jwt.HashToken(req.Token);

        var resetToken = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (resetToken is null || !resetToken.IsValid)
            throw new InvalidOperationException("Invalid or expired reset token.");

        var user = await _db.Users.FindAsync([resetToken.UserId], ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Hash new password with BCrypt work factor 12
        user.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12));

        // Mark token as used
        resetToken.MarkUsed();

        // Revoke all existing refresh tokens for this user
        var refreshTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var rt in refreshTokens)
            rt.Revoke();

        await _db.SaveChangesAsync(ct);
    }
}
