using Jobuler.Application.Common;
using Jobuler.Domain.Logs;
using Jobuler.Infrastructure.Persistence;

namespace Jobuler.Infrastructure.Logging;

public class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;

    public AuditLogger(AppDbContext db) => _db = db;

    public async Task LogAsync(
        Guid? spaceId, Guid? actorUserId, string action,
        string? entityType = null, Guid? entityId = null,
        string? beforeJson = null, string? afterJson = null,
        string? ipAddress = null, CancellationToken ct = default)
    {
        var entry = AuditLog.Create(
            spaceId, actorUserId, action,
            entityType, entityId,
            beforeJson, afterJson, ipAddress);

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
