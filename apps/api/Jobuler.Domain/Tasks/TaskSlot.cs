using Jobuler.Domain.Common;

namespace Jobuler.Domain.Tasks;

public enum TaskSlotStatus { Active, Cancelled, Completed }

public class TaskSlot : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid TaskTypeId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public int RequiredHeadcount { get; private set; } = 1;
    public int Priority { get; private set; } = 5;
    public List<Guid> RequiredRoleIds { get; private set; } = [];
    public List<Guid> RequiredQualificationIds { get; private set; } = [];
    public TaskSlotStatus Status { get; private set; } = TaskSlotStatus.Active;
    public string? Location { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    private TaskSlot() { }

    public static TaskSlot Create(
        Guid spaceId, Guid taskTypeId, DateTime startsAt, DateTime endsAt,
        int requiredHeadcount, int priority, Guid createdByUserId,
        List<Guid>? requiredRoleIds = null, List<Guid>? requiredQualificationIds = null,
        string? location = null)
    {
        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.");

        return new TaskSlot
        {
            SpaceId = spaceId,
            TaskTypeId = taskTypeId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            RequiredHeadcount = requiredHeadcount,
            Priority = priority,
            RequiredRoleIds = requiredRoleIds ?? [],
            RequiredQualificationIds = requiredQualificationIds ?? [],
            Location = location?.Trim(),
            CreatedByUserId = createdByUserId
        };
    }

    public void Cancel() { Status = TaskSlotStatus.Cancelled; Touch(); }
}
