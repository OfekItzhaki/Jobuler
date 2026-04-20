using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.Spaces.Commands;

public record CreateSpaceRoleCommand(
    Guid SpaceId, string Name, string? Description,
    Guid RequestingUserId) : IRequest<Guid>;

public class CreateSpaceRoleCommandHandler : IRequestHandler<CreateSpaceRoleCommand, Guid>
{
    private readonly AppDbContext _db;
    public CreateSpaceRoleCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateSpaceRoleCommand req, CancellationToken ct)
    {
        var role = SpaceRole.Create(req.SpaceId, req.Name, req.RequestingUserId, req.Description);
        _db.SpaceRoles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role.Id;
    }
}
