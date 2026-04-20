using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.Spaces.Commands;

public class CreateSpaceCommandHandler : IRequestHandler<CreateSpaceCommand, Guid>
{
    private readonly AppDbContext _db;

    public CreateSpaceCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        var space = Space.Create(request.Name, request.RequestingUserId, request.Description, request.Locale);
        _db.Spaces.Add(space);

        // Owner automatically gets full membership and all permissions
        var membership = SpaceMembership.Create(space.Id, request.RequestingUserId);
        _db.SpaceMemberships.Add(membership);

        foreach (var perm in AllPermissions())
        {
            _db.SpacePermissionGrants.Add(
                SpacePermissionGrant.Grant(space.Id, request.RequestingUserId, perm, request.RequestingUserId));
        }

        await _db.SaveChangesAsync(ct);
        return space.Id;
    }

    private static IEnumerable<string> AllPermissions() =>
    [
        Permissions.SpaceView,
        Permissions.SpaceAdminMode,
        Permissions.PeopleManage,
        Permissions.ConstraintsManage,
        Permissions.RestrictionsManageSensitive,
        Permissions.TasksManage,
        Permissions.ScheduleRecalculate,
        Permissions.SchedulePublish,
        Permissions.ScheduleRollback,
        Permissions.PermissionsManage,
        Permissions.OwnershipTransfer,
        Permissions.LogsViewSensitive,
    ];
}
