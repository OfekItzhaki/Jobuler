using Jobuler.Application.Groups.Commands;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Queries;

// ── List qualifications for a group ──────────────────────────────────────────

public record GetGroupQualificationsQuery(Guid SpaceId, Guid GroupId) : IRequest<List<GroupQualificationDto>>;

public class GetGroupQualificationsQueryHandler : IRequestHandler<GetGroupQualificationsQuery, List<GroupQualificationDto>>
{
    private readonly AppDbContext _db;
    public GetGroupQualificationsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<GroupQualificationDto>> Handle(GetGroupQualificationsQuery req, CancellationToken ct) =>
        await _db.GroupQualifications.AsNoTracking()
            .Where(q => q.SpaceId == req.SpaceId && q.GroupId == req.GroupId && q.IsActive)
            .OrderBy(q => q.Name)
            .Select(q => new GroupQualificationDto(q.Id, q.Name, q.Description, q.IsActive))
            .ToListAsync(ct);
}

// ── List member qualifications for a group ────────────────────────────────────

public record GetMemberQualificationsQuery(Guid SpaceId, Guid GroupId) : IRequest<List<MemberQualificationDto>>;

public class GetMemberQualificationsQueryHandler : IRequestHandler<GetMemberQualificationsQuery, List<MemberQualificationDto>>
{
    private readonly AppDbContext _db;
    public GetMemberQualificationsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<MemberQualificationDto>> Handle(GetMemberQualificationsQuery req, CancellationToken ct)
    {
        var assignments = await _db.MemberQualifications.AsNoTracking()
            .Where(mq => mq.SpaceId == req.SpaceId && mq.GroupId == req.GroupId)
            .ToListAsync(ct);

        var qualIds = assignments.Select(a => a.QualificationId).Distinct().ToList();
        var quals = await _db.GroupQualifications.AsNoTracking()
            .Where(q => qualIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.Name, ct);

        return assignments.Select(a => new MemberQualificationDto(
            a.Id, a.PersonId, a.QualificationId,
            quals.GetValueOrDefault(a.QualificationId, "Unknown")))
            .ToList();
    }
}
