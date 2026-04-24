using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

public class GroupMembership : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid PersonId { get; private set; }
    public bool IsOwner { get; private set; }
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    private GroupMembership() { }

    public static GroupMembership Create(Guid spaceId, Guid groupId, Guid personId, bool isOwner = false) =>
        new() { SpaceId = spaceId, GroupId = groupId, PersonId = personId, IsOwner = isOwner };

    public void SetOwner(bool isOwner) { IsOwner = isOwner; }
}
