using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

public record CancelOwnershipTransferCommand(Guid SpaceId, Guid GroupId, Guid RequestingUserId) : IRequest;

public class CancelOwnershipTransferCommandHandler : IRequestHandler<CancelOwnershipTransferCommand>
{
    private readonly AppDbContext _db;
    public CancelOwnershipTransferCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(CancelOwnershipTransferCommand req, CancellationToken ct)
    {
        var ownerMembership = await _db.GroupMemberships
            .Join(_db.People, m => m.PersonId, p => p.Id, (m, p) => new { m, p })
            .FirstOrDefaultAsync(x => x.m.GroupId == req.GroupId && x.m.IsOwner && x.p.LinkedUserId == req.RequestingUserId, ct);
        if (ownerMembership is null)
            throw new UnauthorizedAccessException("Only the group owner can cancel an ownership transfer.");

        var transfer = await _db.PendingOwnershipTransfers
            .FirstOrDefaultAsync(t => t.GroupId == req.GroupId, ct)
            ?? throw new KeyNotFoundException("No pending ownership transfer found for this group.");

        _db.PendingOwnershipTransfers.Remove(transfer);
        await _db.SaveChangesAsync(ct);
    }
}
