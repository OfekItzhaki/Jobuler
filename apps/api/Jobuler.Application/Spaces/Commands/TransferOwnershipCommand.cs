using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Spaces.Commands;

public record TransferOwnershipCommand(
    Guid SpaceId,
    Guid NewOwnerUserId,
    Guid RequestingUserId,
    string? Reason) : IRequest;

public class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand>
{
    private readonly AppDbContext _db;

    public TransferOwnershipCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(TransferOwnershipCommand request, CancellationToken ct)
    {
        var space = await _db.Spaces.FirstOrDefaultAsync(s => s.Id == request.SpaceId, ct)
            ?? throw new KeyNotFoundException("Space not found.");

        if (space.OwnerUserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the current owner can transfer ownership.");

        var history = OwnershipTransferHistory.Record(
            request.SpaceId,
            space.OwnerUserId,
            request.NewOwnerUserId,
            request.RequestingUserId,
            request.Reason);

        space.TransferOwnership(request.NewOwnerUserId);

        _db.OwnershipTransferHistory.Add(history);
        await _db.SaveChangesAsync(ct);
    }
}
