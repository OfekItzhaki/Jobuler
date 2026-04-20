using Jobuler.Domain.Common;

namespace Jobuler.Domain.People;

public enum PresenceState
{
    FreeInBase,
    AtHome,
    OnMission   // auto-derived from assignments; not manually set
}

/// <summary>
/// Tracks where a person physically is over a time window.
/// FreeInBase and AtHome are manually set by admins.
/// OnMission is auto-derived from task slot assignments.
/// </summary>
public class PresenceWindow : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid PersonId { get; private set; }
    public PresenceState State { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public string? Note { get; private set; }
    public bool IsDerived { get; private set; }  // true = auto-derived from assignment

    private PresenceWindow() { }

    public static PresenceWindow CreateManual(
        Guid spaceId, Guid personId, PresenceState state,
        DateTime startsAt, DateTime endsAt, string? note = null)
    {
        if (state == PresenceState.OnMission)
            throw new InvalidOperationException("OnMission state must be derived, not manually set.");
        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.");

        return new PresenceWindow
        {
            SpaceId = spaceId,
            PersonId = personId,
            State = state,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Note = note?.Trim(),
            IsDerived = false
        };
    }

    public static PresenceWindow CreateDerived(
        Guid spaceId, Guid personId, DateTime startsAt, DateTime endsAt) =>
        new()
        {
            SpaceId = spaceId,
            PersonId = personId,
            State = PresenceState.OnMission,
            StartsAt = startsAt,
            EndsAt = endsAt,
            IsDerived = true
        };
}
