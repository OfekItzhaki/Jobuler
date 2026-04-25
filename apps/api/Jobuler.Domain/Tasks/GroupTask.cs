using Jobuler.Domain.Common;

namespace Jobuler.Domain.Tasks;

/// <summary>
/// Flat, group-scoped task entity. Replaces the two-level TaskType + TaskSlot model
/// for new functionality. Legacy tables are retained for backward compatibility.
///
/// allows_double_shift: the same person can be assigned to this task twice in a row.
/// allows_overlap: a person assigned to this task can also be assigned to another
///                 task at the same time (e.g. "כוננות חירום" + "סיור").
/// </summary>
public class GroupTask : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public decimal DurationHours { get; private set; }
    public int RequiredHeadcount { get; private set; } = 1;
    public TaskBurdenLevel BurdenLevel { get; private set; } = TaskBurdenLevel.Neutral;
    public bool AllowsDoubleShift { get; private set; } = false;
    public bool AllowsOverlap { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private GroupTask() { }

    public static GroupTask Create(
        Guid spaceId,
        Guid groupId,
        string name,
        DateTime startsAt,
        DateTime endsAt,
        decimal durationHours,
        int requiredHeadcount,
        TaskBurdenLevel burdenLevel,
        bool allowsDoubleShift,
        bool allowsOverlap,
        Guid createdByUserId) =>
        new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            Name = name.Trim(),
            StartsAt = startsAt,
            EndsAt = endsAt,
            DurationHours = durationHours,
            RequiredHeadcount = requiredHeadcount,
            BurdenLevel = burdenLevel,
            AllowsDoubleShift = allowsDoubleShift,
            AllowsOverlap = allowsOverlap,
            CreatedByUserId = createdByUserId
        };

    public void Update(
        string name,
        DateTime startsAt,
        DateTime endsAt,
        decimal durationHours,
        int requiredHeadcount,
        TaskBurdenLevel burdenLevel,
        bool allowsDoubleShift,
        bool allowsOverlap,
        Guid updatedByUserId)
    {
        Name = name.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
        DurationHours = durationHours;
        RequiredHeadcount = requiredHeadcount;
        BurdenLevel = burdenLevel;
        AllowsDoubleShift = allowsDoubleShift;
        AllowsOverlap = allowsOverlap;
        UpdatedByUserId = updatedByUserId;
        Touch();
    }

    public void Deactivate(Guid updatedByUserId)
    {
        IsActive = false;
        UpdatedByUserId = updatedByUserId;
        Touch();
    }
}
