using Jobuler.Application.Common;
using Jobuler.Domain.Scheduling;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Scheduling.Commands;

public record PublishVersionCommand(
    Guid SpaceId,
    Guid VersionId,
    Guid RequestingUserId) : IRequest;

public class PublishVersionCommandHandler : IRequestHandler<PublishVersionCommand>
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public PublishVersionCommandHandler(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(PublishVersionCommand req, CancellationToken ct)
    {
        var version = await _db.ScheduleVersions
            .FirstOrDefaultAsync(v => v.Id == req.VersionId && v.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Schedule version not found.");

        // Archive the current published version before publishing the new one
        var currentPublished = await _db.ScheduleVersions
            .Where(v => v.SpaceId == req.SpaceId && v.Status == ScheduleVersionStatus.Published)
            .ToListAsync(ct);

        foreach (var old in currentPublished)
            old.Archive();

        // Publish enforces draft-only rule inside the domain entity
        version.Publish(req.RequestingUserId);
        await _db.SaveChangesAsync(ct);

        // Audit log — required by security rules
        await _audit.LogAsync(
            req.SpaceId, req.RequestingUserId,
            "publish_schedule",
            "schedule_version", req.VersionId,
            afterJson: $"{{\"version_number\":{version.VersionNumber}}}",
            ct: ct);
    }
}
