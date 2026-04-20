using Jobuler.Domain.Common;

namespace Jobuler.Domain.People;

/// <summary>
/// Explicit time window during which a person is available to be scheduled.
/// </summary>
public class AvailabilityWindow : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public string? Note { get; private set; }

    private AvailabilityWindow() { }

    public static AvailabilityWindow Create(
        Guid spaceId, Guid personId, DateTime startsAt, DateTime endsAt, string? note = null)
    {
        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.");

        return new AvailabilityWindow
        {
            SpaceId = spaceId,
            PersonId = personId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Note = note?.Trim()
        };
    }
}
