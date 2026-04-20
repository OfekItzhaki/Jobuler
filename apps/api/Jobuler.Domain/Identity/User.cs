using Jobuler.Domain.Common;

namespace Jobuler.Domain.Identity;

public class User : AuditableEntity
{
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public string PreferredLocale { get; private set; } = "he";
    public string? ProfileImageUrl { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // EF Core constructor
    private User() { }

    public static User Create(string email, string displayName, string passwordHash, string locale = "he")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email = email.ToLowerInvariant().Trim(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            PreferredLocale = locale
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void UpdateProfile(string displayName, string? profileImageUrl, string locale)
    {
        DisplayName = displayName.Trim();
        ProfileImageUrl = profileImageUrl;
        PreferredLocale = locale;
        Touch();
    }

    public void Deactivate() { IsActive = false; Touch(); }
}
