using Jobuler.Domain.Common;

namespace Jobuler.Domain.Scheduling;

/// <summary>
/// Pre-computed diff between a schedule version and its baseline.
/// Stored for fast UI display — avoids recomputing the diff on every page load.
/// </summary>
public class AssignmentChangeSummary : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid VersionId { get; private set; }
    public Guid? BaselineVersionId { get; private set; }
    public int AddedCount { get; private set; }
    public int RemovedCount { get; private set; }
    public int ChangedCount { get; private set; }
    public decimal? StabilityScore { get; private set; }
    public string? DiffJson { get; private set; }
    public DateTime ComputedAt { get; private set; }

    private AssignmentChangeSummary() { }

    public static AssignmentChangeSummary Create(
        Guid spaceId, Guid versionId, Guid? baselineVersionId,
        int added, int removed, int changed,
        decimal? stabilityScore, string? diffJson) =>
        new()
        {
            SpaceId = spaceId,
            VersionId = versionId,
            BaselineVersionId = baselineVersionId,
            AddedCount = added,
            RemovedCount = removed,
            ChangedCount = changed,
            StabilityScore = stabilityScore,
            DiffJson = diffJson,
            ComputedAt = DateTime.UtcNow
        };
}
