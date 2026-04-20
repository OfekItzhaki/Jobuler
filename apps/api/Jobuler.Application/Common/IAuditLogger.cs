namespace Jobuler.Application.Common;

/// <summary>
/// Writes append-only audit log entries for security-sensitive actions.
/// Required for: publish, rollback, permission grant/revoke, ownership transfer,
/// sensitive restriction create/update/delete, login failures.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        Guid? spaceId,
        Guid? actorUserId,
        string action,
        string? entityType = null,
        Guid? entityId = null,
        string? beforeJson = null,
        string? afterJson = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}
