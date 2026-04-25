using Jobuler.Application.Common;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Constraints.Commands;

public record DeleteConstraintCommand(
    Guid SpaceId,
    Guid ConstraintId,
    Guid RequestingUserId) : IRequest;

public class DeleteConstraintCommandHandler : IRequestHandler<DeleteConstraintCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public DeleteConstraintCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(DeleteConstraintCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.ConstraintsManage, ct);

        var rule = await _db.ConstraintRules
            .FirstOrDefaultAsync(c => c.Id == req.ConstraintId && c.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Constraint not found.");

        rule.Deactivate(req.RequestingUserId);
        await _db.SaveChangesAsync(ct);
    }
}
