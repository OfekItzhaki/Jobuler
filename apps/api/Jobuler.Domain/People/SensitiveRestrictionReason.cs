using Jobuler.Domain.Common;

namespace Jobuler.Domain.People;

/// <summary>
/// Sensitive reason behind a restriction (e.g. "Hand infection", "Mental health leave").
/// Requires restrictions.manage_sensitive permission to read or write.
/// Stored separately from PersonRestriction to enforce permission separation at the query level.
/// </summary>
public class SensitiveRestrictionReason : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid RestrictionId { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid? CreatedByUserId { get; private set; }

    private SensitiveRestrictionReason() { }

    public static SensitiveRestrictionReason Create(
        Guid spaceId, Guid restrictionId, string reason, Guid createdByUserId) =>
        new()
        {
            SpaceId = spaceId,
            RestrictionId = restrictionId,
            Reason = reason.Trim(),
            CreatedByUserId = createdByUserId
        };

    public void Update(string reason) { Reason = reason.Trim(); Touch(); }
}
