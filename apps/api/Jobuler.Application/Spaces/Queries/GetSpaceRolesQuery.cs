using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Spaces.Queries;

public record SpaceRoleDto(Guid Id, string Name, string? Description, bool IsActive);

public record GetSpaceRolesQuery(Guid SpaceId) : IRequest<List<SpaceRoleDto>>;

public class GetSpaceRolesQueryHandler : IRequestHandler<GetSpaceRolesQuery, List<SpaceRoleDto>>
{
    private readonly AppDbContext _db;
    public GetSpaceRolesQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<SpaceRoleDto>> Handle(GetSpaceRolesQuery req, CancellationToken ct) =>
        await _db.SpaceRoles.AsNoTracking()
            .Where(r => r.SpaceId == req.SpaceId)
            .OrderBy(r => r.Name)
            .Select(r => new SpaceRoleDto(r.Id, r.Name, r.Description, r.IsActive))
            .ToListAsync(ct);
}
