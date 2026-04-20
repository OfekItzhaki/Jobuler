using Jobuler.Domain.Common;

namespace Jobuler.Domain.Scheduling;

public enum AssignmentSource { Solver, Override }

/// <summary>
/// A single person assigned to a task slot within a schedule version.
/// Immutable once the parent version is published.
/// </summary>
public class Assignment : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid ScheduleVersionId { get; private set; }
    public Guid TaskSlotId { get; private set; }
    public Guid PersonId { get; private set; }
    public AssignmentSource Source { get; private set; } = AssignmentSource.Solver;
    public string? ChangeReasonSummary { get; private set; }

    private Assignment() { }

    public static Assignment Create(
        Guid spaceId, Guid scheduleVersionId, Guid taskSlotId,
        Guid personId, AssignmentSource source = AssignmentSource.Solver,
        string? changeReasonSummary = null) =>
        new()
        {
            SpaceId = spaceId,
            ScheduleVersionId = scheduleVersionId,
            TaskSlotId = taskSlotId,
            PersonId = personId,
            Source = source,
            ChangeReasonSummary = changeReasonSummary
        };
}
