using Jobuler.Domain.Common;

namespace Jobuler.Domain.Spaces;

public class OwnershipTransferHistory : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid PreviousOwnerId { get; private set; }
    public Guid NewOwnerId { get; private set; }
    public Guid TransferredByUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime TransferredAt { get; private set; } = DateTime.UtcNow;

    private OwnershipTransferHistory() { }

    public static OwnershipTransferHistory Record(
        Guid spaceId, Guid previousOwnerId, Guid newOwnerId,
        Guid transferredByUserId, string? reason = null) =>
        new()
        {
            SpaceId = spaceId,
            PreviousOwnerId = previousOwnerId,
            NewOwnerId = newOwnerId,
            TransferredByUserId = transferredByUserId,
            Reason = reason
        };
}
