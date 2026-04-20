using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

/// <summary>
/// Dynamic group type per space (squad, unit, platoon, company, etc.).
/// Types are data, not hardcoded enums.
/// </summary>
public class GroupType : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private GroupType() { }

    public static GroupType Create(Guid spaceId, string name, string? description = null) =>
        new() { SpaceId = spaceId, Name = name.Trim(), Description = description?.Trim() };

    public void Update(string name, string? description) { Name = name.Trim(); Description = description?.Trim(); }
    public void Deactivate() => IsActive = false;
}
