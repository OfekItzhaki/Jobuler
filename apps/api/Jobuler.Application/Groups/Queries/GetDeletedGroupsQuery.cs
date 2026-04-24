using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Queries;

public record DeletedGroupDto(Guid Id, string Name, DateTime DeletedAt);
public record GetDeletedGroupsQuery(Guid SpaceId, Guid RequestingUserId) : IRequest<List<DeletedGroupDto>>;

public class GetDeletedGroupsQueryHandler : IRequestHandler<GetDeletedGroupsQuery, List<DeletedGroupDto>>
{
    private readonly AppDbContext _db;
    public GetDeletedGroupsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<DeletedGroupDto>> Handle(GetDeletedGroupsQuery req, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Get groups owned by this user that are soft-deleted within 30 days
        return await _db.Groups
            .Where(g => g.SpaceId == req.SpaceId && g.DeletedAt != null && g.DeletedAt > cutoff)
            .Join(_db.GroupMemberships.Where(m => m.IsOwner),
                g => g.Id, m => m.GroupId, (g, m) => new { g, m })
            .Join(_db.People.Where(p => p.LinkedUserId == req.RequestingUserId),
                x => x.m.PersonId, p => p.Id, (x, p) => x.g)
            .Select(g => new DeletedGroupDto(g.Id, g.Name, g.DeletedAt!.Value))
            .ToListAsync(ct);
    }
}
