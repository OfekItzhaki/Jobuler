using Jobuler.Application.Common;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Scheduling.Commands;

public record DiscardVersionCommand(
    Guid SpaceId,
    Guid VersionId,
    Guid RequestingUserId) : IRequest;

public class DiscardVersionCommandHandler : IRequestHandler<DiscardVersionCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public DiscardVersionCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(DiscardVersionCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.SchedulePublish, ct);

        var version = await _db.ScheduleVersions
            .FirstOrDefaultAsync(v => v.Id == req.VersionId && v.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Schedule version not found.");

        version.Discard(); // throws InvalidOperationException if not Draft
        await _db.SaveChangesAsync(ct);
    }
}
