using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

public record SoftDeleteGroupCommand(Guid SpaceId, Guid GroupId, Guid RequestingUserId) : IRequest;

public class SoftDeleteGroupCommandHandler : IRequestHandler<SoftDeleteGroupCommand>
{
    private readonly AppDbContext _db;
    public SoftDeleteGroupCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(SoftDeleteGroupCommand req, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == req.GroupId && g.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        var ownerMembership = await _db.GroupMemberships
            .Join(_db.People, m => m.PersonId, p => p.Id, (m, p) => new { m, p })
            .FirstOrDefaultAsync(x => x.m.GroupId == req.GroupId && x.m.IsOwner && x.p.LinkedUserId == req.RequestingUserId, ct);
        if (ownerMembership is null)
            throw new UnauthorizedAccessException("Only the group owner can delete the group.");

        group.SoftDelete();
        await _db.SaveChangesAsync(ct);
    }
}
