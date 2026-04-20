using Jobuler.Application.Common;
using Jobuler.Domain.Scheduling;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Scheduling.Commands;

public record RollbackVersionCommand(
    Guid SpaceId,
    Guid TargetVersionId,
    Guid RequestingUserId) : IRequest<Guid>;

public class RollbackVersionCommandHandler : IRequestHandler<RollbackVersionCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public RollbackVersionCommandHandler(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Guid> Handle(RollbackVersionCommand req, CancellationToken ct)
    {
        var target = await _db.ScheduleVersions
            .FirstOrDefaultAsync(v =>
                v.Id == req.TargetVersionId &&
                v.SpaceId == req.SpaceId &&
                v.Status == ScheduleVersionStatus.Published, ct)
            ?? throw new KeyNotFoundException(
                "Target version not found or is not a published version.");

        var nextVersion = await _db.ScheduleVersions
            .Where(v => v.SpaceId == req.SpaceId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;
        nextVersion++;

        var rollbackVersion = ScheduleVersion.CreateRollback(
            req.SpaceId, nextVersion, req.TargetVersionId, req.RequestingUserId);

        _db.ScheduleVersions.Add(rollbackVersion);
        await _db.SaveChangesAsync(ct);

        var sourceAssignments = await _db.Assignments.AsNoTracking()
            .Where(a => a.ScheduleVersionId == req.TargetVersionId && a.SpaceId == req.SpaceId)
            .ToListAsync(ct);

        var newAssignments = sourceAssignments.Select(a => Assignment.Create(
            req.SpaceId, rollbackVersion.Id, a.TaskSlotId, a.PersonId,
            AssignmentSource.Solver, "Rollback from version " + target.VersionNumber))
            .ToList();

        _db.Assignments.AddRange(newAssignments);
        target.MarkRolledBack();
        await _db.SaveChangesAsync(ct);

        // Audit log — required by security rules
        await _audit.LogAsync(
            req.SpaceId, req.RequestingUserId,
            "rollback_schedule",
            "schedule_version", req.TargetVersionId,
            afterJson: $"{{\"new_version_id\":\"{rollbackVersion.Id}\"}}",
            ct: ct);

        return rollbackVersion.Id;
    }
}

public record RollbackVersionCommand(
    Guid SpaceId,
    Guid TargetVersionId,   // the published version to roll back to
    Guid RequestingUserId) : IRequest<Guid>;  // returns new version Id

public class RollbackVersionCommandHandler : IRequestHandler<RollbackVersionCommand, Guid>
{
    private readonly AppDbContext _db;

    public RollbackVersionCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(RollbackVersionCommand req, CancellationToken ct)
    {
        // Verify the target version exists, belongs to this space, and was published
        var target = await _db.ScheduleVersions
            .FirstOrDefaultAsync(v =>
                v.Id == req.TargetVersionId &&
                v.SpaceId == req.SpaceId &&
                v.Status == ScheduleVersionStatus.Published, ct)
            ?? throw new KeyNotFoundException(
                "Target version not found or is not a published version.");

        // Rollback = create a new draft version pointing back to the target
        // Never mutate the target version — immutability is preserved
        var nextVersion = await _db.ScheduleVersions
            .Where(v => v.SpaceId == req.SpaceId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;
        nextVersion++;

        var rollbackVersion = ScheduleVersion.CreateRollback(
            req.SpaceId, nextVersion, req.TargetVersionId, req.RequestingUserId);

        _db.ScheduleVersions.Add(rollbackVersion);
        await _db.SaveChangesAsync(ct); // get rollbackVersion.Id

        // Copy assignments from the target version into the new rollback version
        var sourceAssignments = await _db.Assignments.AsNoTracking()
            .Where(a => a.ScheduleVersionId == req.TargetVersionId && a.SpaceId == req.SpaceId)
            .ToListAsync(ct);

        var newAssignments = sourceAssignments.Select(a => Assignment.Create(
            req.SpaceId, rollbackVersion.Id, a.TaskSlotId, a.PersonId,
            AssignmentSource.Solver, "Rollback from version " + target.VersionNumber))
            .ToList();

        _db.Assignments.AddRange(newAssignments);

        // Mark the target as rolled-back (status change only, data untouched)
        target.MarkRolledBack();

        await _db.SaveChangesAsync(ct);

        return rollbackVersion.Id;
    }
}
