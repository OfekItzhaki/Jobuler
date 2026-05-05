using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

public class PersonRoleAssignment : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? GroupId { get; private set; }
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;

    private PersonRoleAssignment() { }

    public static PersonRoleAssignment Create(Guid spaceId, Guid personId, Guid roleId, Guid? groupId = null) =>
        new() { SpaceId = spaceId, PersonId = personId, RoleId = roleId, GroupId = groupId };
}
