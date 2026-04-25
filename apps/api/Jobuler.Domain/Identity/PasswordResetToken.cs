using Jobuler.Domain.Common;

namespace Jobuler.Domain.Identity;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;
    public bool IsValid => !IsExpired && !IsUsed;

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

    public void MarkUsed() => UsedAt = DateTime.UtcNow;
}
