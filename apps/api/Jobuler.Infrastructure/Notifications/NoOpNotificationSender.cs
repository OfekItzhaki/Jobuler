using Jobuler.Application.Common;
using Microsoft.Extensions.Logging;

namespace Jobuler.Infrastructure.Notifications;

/// <summary>
/// No-op implementation of <see cref="INotificationSender"/>.
/// Logs the reset token at Warning level so developers can copy it from the console
/// during development without needing a real notification provider configured.
/// </summary>
public class NoOpNotificationSender : INotificationSender
{
    private readonly ILogger<NoOpNotificationSender> _logger;

    public NoOpNotificationSender(ILogger<NoOpNotificationSender> logger)
        => _logger = logger;

    public Task SendPasswordResetAsync(string to, string token, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[NoOp] Password reset for {To}: token={Token}",
            to, token);
        return Task.CompletedTask;
    }
}
