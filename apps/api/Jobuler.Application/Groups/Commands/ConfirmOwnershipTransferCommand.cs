using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

public record ConfirmOwnershipTransferCommand(string ConfirmationToken) : IRequest;

public class ConfirmOwnershipTransferCommandHandler : IRequestHandler<ConfirmOwnershipTransferCommand>
{
    private readonly AppDbContext _db;
    public ConfirmOwnershipTransferCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(ConfirmOwnershipTransferCommand req, CancellationToken ct)
    {
        var transfer = await _db.PendingOwnershipTransfers
            .FirstOrDefaultAsync(t => t.ConfirmationToken == req.ConfirmationToken, ct)
            ?? throw new InvalidOperationException("Invalid or expired confirmation token.");

        if (transfer.IsExpired)
            throw new InvalidOperationException("This confirmation link has expired.");

        // Swap ownership atomically
        var currentOwnerMembership = await _db.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == transfer.GroupId && m.PersonId == transfer.CurrentOwnerPersonId, ct)
            ?? throw new InvalidOperationException("Current owner membership not found.");

        var newOwnerMembership = await _db.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == transfer.GroupId && m.PersonId == transfer.ProposedOwnerPersonId, ct)
            ?? throw new InvalidOperationException("Proposed owner membership not found.");

        currentOwnerMembership.SetOwner(false);
        newOwnerMembership.SetOwner(true);
        _db.PendingOwnershipTransfers.Remove(transfer);

        await _db.SaveChangesAsync(ct);
    }
}
