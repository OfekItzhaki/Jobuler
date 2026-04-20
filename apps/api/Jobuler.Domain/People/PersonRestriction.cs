using Jobuler.Domain.Common;

namespace Jobuler.Domain.People;

/// <summary>
/// Operational restriction on a person — visible to admins with people.manage.
/// The sensitive reason (medical, personal) is stored separately in SensitiveRestrictionReason
/// and requires restrictions.manage_sensitive permission.
/// </summary>
public class PersonRestriction : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid PersonId { get; private set; }
    public string RestrictionType { get; private set; } = default!;  // e.g. no_kitchen, no_night
    public Guid? TaskTypeId { get; private set; }   // optional: restrict to specific task type
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }
    public string? OperationalNote { get; private set; }  // visible to normal admins
    public Guid? CreatedByUserId { get; private set; }

    private PersonRestriction() { }

    public static PersonRestriction Create(
        Guid spaceId, Guid personId, string restrictionType,
        DateOnly effectiveFrom, DateOnly? effectiveUntil,
        string? operationalNote, Guid? taskTypeId, Guid createdByUserId) =>
        new()
        {
            SpaceId = spaceId,
            PersonId = personId,
            RestrictionType = restrictionType.Trim(),
            TaskTypeId = taskTypeId,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil,
            OperationalNote = operationalNote?.Trim(),
            CreatedByUserId = createdByUserId
        };

    public void Update(DateOnly? effectiveUntil, string? operationalNote)
    {
        EffectiveUntil = effectiveUntil;
        OperationalNote = operationalNote?.Trim();
        Touch();
    }
}
