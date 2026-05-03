using Jobuler.Application.Common;
using Jobuler.Domain.Groups;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record GroupQualificationDto(Guid Id, string Name, string? Description, bool IsActive);
public record MemberQualificationDto(Guid Id, Guid PersonId, Guid QualificationId, string QualificationName);

// ── Create qualification ──────────────────────────────────────────────────────

public record CreateGroupQualificationCommand(
    Guid SpaceId, Guid GroupId, string Name, string? Description,
    Guid RequestingUserId) : IRequest<Guid>;

public class CreateGroupQualificationCommandHandler : IRequestHandler<CreateGroupQualificationCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public CreateGroupQualificationCommandHandler(AppDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    public async Task<Guid> Handle(CreateGroupQualificationCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        // Reactivate if deactivated with same name
        var existing = await _db.GroupQualifications
            .FirstOrDefaultAsync(q => q.SpaceId == req.SpaceId && q.GroupId == req.GroupId
                && q.Name == req.Name.Trim(), ct);

        if (existing is not null)
        {
            if (existing.IsActive)
                throw new ConflictException($"A qualification named '{req.Name.Trim()}' already exists.");
            existing.Update(req.Name, req.Description);
            existing.Reactivate();
            await _db.SaveChangesAsync(ct);
            return existing.Id;
        }

        var qual = GroupQualification.Create(req.SpaceId, req.GroupId, req.Name, req.RequestingUserId, req.Description);
        _db.GroupQualifications.Add(qual);
        await _db.SaveChangesAsync(ct);
        return qual.Id;
    }
}

// ── Update qualification ──────────────────────────────────────────────────────

public record UpdateGroupQualificationCommand(
    Guid SpaceId, Guid GroupId, Guid QualificationId,
    string Name, string? Description, Guid RequestingUserId) : IRequest;

public class UpdateGroupQualificationCommandHandler : IRequestHandler<UpdateGroupQualificationCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public UpdateGroupQualificationCommandHandler(AppDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    public async Task Handle(UpdateGroupQualificationCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);
        var qual = await _db.GroupQualifications
            .FirstOrDefaultAsync(q => q.Id == req.QualificationId && q.GroupId == req.GroupId, ct)
            ?? throw new KeyNotFoundException("Qualification not found.");
        qual.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Deactivate qualification ──────────────────────────────────────────────────

public record DeactivateGroupQualificationCommand(
    Guid SpaceId, Guid GroupId, Guid QualificationId, Guid RequestingUserId) : IRequest;

public class DeactivateGroupQualificationCommandHandler : IRequestHandler<DeactivateGroupQualificationCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public DeactivateGroupQualificationCommandHandler(AppDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    public async Task Handle(DeactivateGroupQualificationCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);
        var qual = await _db.GroupQualifications
            .FirstOrDefaultAsync(q => q.Id == req.QualificationId && q.GroupId == req.GroupId, ct)
            ?? throw new KeyNotFoundException("Qualification not found.");
        qual.Deactivate();
        await _db.SaveChangesAsync(ct);
    }
}

// ── Assign qualification to member ────────────────────────────────────────────

public record AssignMemberQualificationCommand(
    Guid SpaceId, Guid GroupId, Guid PersonId, Guid QualificationId,
    Guid RequestingUserId) : IRequest<Guid>;

public class AssignMemberQualificationCommandHandler : IRequestHandler<AssignMemberQualificationCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public AssignMemberQualificationCommandHandler(AppDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    public async Task<Guid> Handle(AssignMemberQualificationCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        var already = await _db.MemberQualifications
            .AnyAsync(q => q.PersonId == req.PersonId && q.QualificationId == req.QualificationId, ct);
        if (already) return Guid.Empty; // idempotent

        var mq = MemberQualification.Create(req.SpaceId, req.GroupId, req.PersonId, req.QualificationId, req.RequestingUserId);
        _db.MemberQualifications.Add(mq);
        await _db.SaveChangesAsync(ct);
        return mq.Id;
    }
}

// ── Remove qualification from member ─────────────────────────────────────────

public record RemoveMemberQualificationCommand(
    Guid SpaceId, Guid GroupId, Guid PersonId, Guid QualificationId,
    Guid RequestingUserId) : IRequest;

public class RemoveMemberQualificationCommandHandler : IRequestHandler<RemoveMemberQualificationCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public RemoveMemberQualificationCommandHandler(AppDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    public async Task Handle(RemoveMemberQualificationCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);
        var mq = await _db.MemberQualifications
            .FirstOrDefaultAsync(q => q.PersonId == req.PersonId
                && q.QualificationId == req.QualificationId
                && q.GroupId == req.GroupId, ct);
        if (mq is null) return;
        _db.MemberQualifications.Remove(mq);
        await _db.SaveChangesAsync(ct);
    }
}
