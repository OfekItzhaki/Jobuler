using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

/// <summary>
/// A qualification type defined by the group admin (e.g. Driver, Sniper, Commander).
/// Members can be assigned qualifications from this list.
/// The solver uses qualifications to enforce "this mission requires a commander" rules.
/// </summary>
public class GroupQualification : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? CreatedByUserId { get; private set; }

    private GroupQualification() { }

    public static GroupQualification Create(
        Guid spaceId, Guid groupId, string name,
        Guid createdByUserId, string? description = null) =>
        new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedByUserId = createdByUserId,
        };

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
        Touch();
    }

    public void Deactivate() { IsActive = false; Touch(); }
    public void Reactivate() { IsActive = true; Touch(); }
}

/// <summary>
/// Assignment of a qualification to a group member.
/// </summary>
public class MemberQualification : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid QualificationId { get; private set; }
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; private set; }

    private MemberQualification() { }

    public static MemberQualification Create(
        Guid spaceId, Guid groupId, Guid personId,
        Guid qualificationId, Guid? assignedByUserId = null) =>
        new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            PersonId = personId,
            QualificationId = qualificationId,
            AssignedByUserId = assignedByUserId,
        };
}
