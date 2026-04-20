using Jobuler.Domain.Logs;
using Jobuler.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Jobuler.Infrastructure.Logging;

/// <summary>
/// Writes structured system log entries to the database.
/// Used by the solver worker and other background processes to record
/// operational events that admins can view in the logs UI.
/// </summary>
public interface ISystemLogger
{
    Task LogAsync(Guid spaceId, string severity, string eventType, string message,
        string? detailsJson = null, Guid? actorUserId = null, bool isSensitive = false,
        CancellationToken ct = default);
}

public class SystemLogger : ISystemLogger
{
    private readonly AppDbContext _db;
    private readonly ILogger<SystemLogger> _logger;

    public SystemLogger(AppDbContext db, ILogger<SystemLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid spaceId, string severity, string eventType, string message,
        string? detailsJson = null, Guid? actorUserId = null, bool isSensitive = false,
        CancellationToken ct = default)
    {
        // Write to DB for admin UI
        var entry = SystemLog.Create(
            spaceId, severity, eventType, message,
            detailsJson, actorUserId, isSensitive: isSensitive);
        _db.SystemLogs.Add(entry);
        await _db.SaveChangesAsync(ct);

        // Also emit to structured log (Serilog) for external observability
        var level = severity switch
        {
            "warning"  => LogLevel.Warning,
            "error"    => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _          => LogLevel.Information
        };

        _logger.Log(level, "[{EventType}] {Message} space={SpaceId}", eventType, message, spaceId);
    }
}
