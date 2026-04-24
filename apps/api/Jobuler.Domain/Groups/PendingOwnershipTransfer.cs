using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

public class PendingOwnershipTransfer : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid CurrentOwnerPersonId { get; private set; }
    public Guid ProposedOwnerPersonId { get; private set; }
    public string ConfirmationToken { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    // CreatedAt is inherited from Entity base class

    private PendingOwnershipTransfer() { }

    public static PendingOwnershipTransfer Create(
        Guid spaceId, Guid groupId,
        Guid currentOwnerPersonId, Guid proposedOwnerPersonId)
    {
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                           .ToLowerInvariant();
        var now = DateTime.UtcNow;
        return new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            CurrentOwnerPersonId = currentOwnerPersonId,
            ProposedOwnerPersonId = proposedOwnerPersonId,
            ConfirmationToken = token,
            ExpiresAt = now.AddHours(48)
        };
    }
}
