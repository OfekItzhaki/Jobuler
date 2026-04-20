using Jobuler.Domain.Common;

namespace Jobuler.Domain.Spaces;

/// <summary>
/// Dynamic operational role within a space (Soldier, Medic, Squad Commander, etc.).
/// Roles are data, not hardcoded enums.
/// </summary>
public class SpaceRole : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? CreatedByUserId { get; private set; }

    private SpaceRole() { }

    public static SpaceRole Create(Guid spaceId, string name, Guid createdByUserId, string? description = null) =>
        new()
        {
            SpaceId = spaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedByUserId = createdByUserId
        };

    public void Update(string name, string? description) { Name = name.Trim(); Description = description?.Trim(); Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
}
