namespace Jobuler.Application.Common;

/// <summary>
/// Abstraction for user-facing notifications (e.g., password reset delivery).
/// Separate from <see cref="IEmailSender"/> which handles system emails (ownership transfer).
/// Swap the implementation in DI to enable WhatsApp, SMS, or real email delivery
/// without changing any business logic.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Sends a password reset token to the user.
    /// </summary>
    /// <param name="to">Phone number or email address of the recipient.</param>
    /// <param name="token">The raw (unhashed) reset token to deliver.</param>
    Task SendPasswordResetAsync(string to, string token, CancellationToken ct = default);
}
