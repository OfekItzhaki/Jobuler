using Jobuler.Domain.Common;

namespace Jobuler.Domain.Spaces;

/// <summary>
/// Dynamic operational role within a space or group (Soldier, Medic, Squad Commander, etc.).
/// Roles are data, not hardcoded enums.
/// When GroupId is set, the role belongs to that group only.
/// When GroupId is null, the role is space-level (legacy / shared).
/// </summary>
public class SpaceRole : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid? GroupId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? CreatedByUserId { get; private set; }

    private SpaceRole() { }

    /// <summary>Creates a space-level role (not scoped to a group).</summary>
    public static SpaceRole Create(Guid spaceId, string name, Guid createdByUserId, string? description = null) =>
        new()
        {
            SpaceId = spaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedByUserId = createdByUserId
        };

    /// <summary>Creates a group-scoped role visible only within that group.</summary>
    public static SpaceRole CreateForGroup(Guid spaceId, Guid groupId, string name, Guid createdByUserId, string? description = null) =>
        new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedByUserId = createdByUserId
        };

    public void Update(string name, string? description) { Name = name.Trim(); Description = description?.Trim(); Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
}
