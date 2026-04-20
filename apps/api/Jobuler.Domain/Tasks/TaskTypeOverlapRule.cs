using Jobuler.Domain.Common;

namespace Jobuler.Domain.Tasks;

/// <summary>
/// Explicit overlap compatibility between two task types.
/// If no rule exists and TaskType.AllowsOverlap is false, overlap is forbidden.
/// </summary>
public class TaskTypeOverlapRule : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid TaskTypeAId { get; private set; }
    public Guid TaskTypeBId { get; private set; }
    public bool OverlapAllowed { get; private set; }

    private TaskTypeOverlapRule() { }

    public static TaskTypeOverlapRule Create(
        Guid spaceId, Guid taskTypeAId, Guid taskTypeBId, bool overlapAllowed) =>
        new()
        {
            SpaceId = spaceId,
            TaskTypeAId = taskTypeAId,
            TaskTypeBId = taskTypeBId,
            OverlapAllowed = overlapAllowed
        };
}
