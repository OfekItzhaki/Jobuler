using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Queries;

public record GroupTypeDto(Guid Id, string Name, string? Description, bool IsActive);
public record GroupDto(Guid Id, Guid GroupTypeId, string GroupTypeName, string Name, string? Description, bool IsActive, int MemberCount);
public record GroupMemberDto(Guid PersonId, string FullName, string? DisplayName);

public record GetGroupTypesQuery(Guid SpaceId) : IRequest<List<GroupTypeDto>>;

public class GetGroupTypesQueryHandler : IRequestHandler<GetGroupTypesQuery, List<GroupTypeDto>>
{
    private readonly AppDbContext _db;
    public GetGroupTypesQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<GroupTypeDto>> Handle(GetGroupTypesQuery req, CancellationToken ct) =>
        await _db.GroupTypes.AsNoTracking()
            .Where(g => g.SpaceId == req.SpaceId && g.IsActive)
            .OrderBy(g => g.Name)
            .Select(g => new GroupTypeDto(g.Id, g.Name, g.Description, g.IsActive))
            .ToListAsync(ct);
}

public record GetGroupsQuery(Guid SpaceId) : IRequest<List<GroupDto>>;

public class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, List<GroupDto>>
{
    private readonly AppDbContext _db;
    public GetGroupsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<GroupDto>> Handle(GetGroupsQuery req, CancellationToken ct)
    {
        var groups = await _db.Groups.AsNoTracking()
            .Where(g => g.SpaceId == req.SpaceId && g.IsActive)
            .Join(_db.GroupTypes, g => g.GroupTypeId, t => t.Id,
                (g, t) => new { g, TypeName = t.Name })
            .OrderBy(x => x.TypeName).ThenBy(x => x.g.Name)
            .ToListAsync(ct);

        var memberCounts = await _db.GroupMemberships.AsNoTracking()
            .Where(m => m.SpaceId == req.SpaceId)
            .GroupBy(m => m.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return groups.Select(x => new GroupDto(
            x.g.Id, x.g.GroupTypeId, x.TypeName, x.g.Name, x.g.Description,
            x.g.IsActive, memberCounts.GetValueOrDefault(x.g.Id, 0))).ToList();
    }
}

public record GetGroupMembersQuery(Guid SpaceId, Guid GroupId) : IRequest<List<GroupMemberDto>>;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupMemberDto>>
{
    private readonly AppDbContext _db;
    public GetGroupMembersQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<GroupMemberDto>> Handle(GetGroupMembersQuery req, CancellationToken ct) =>
        await _db.GroupMemberships.AsNoTracking()
            .Where(m => m.GroupId == req.GroupId && m.SpaceId == req.SpaceId)
            .Join(_db.People, m => m.PersonId, p => p.Id,
                (m, p) => new GroupMemberDto(p.Id, p.FullName, p.DisplayName))
            .OrderBy(p => p.FullName)
            .ToListAsync(ct);
}
