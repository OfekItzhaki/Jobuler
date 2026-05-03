using Jobuler.Application.Scheduling.Models;

namespace Jobuler.Application.Scheduling;

/// <summary>
/// Reads all operational data for a space (optionally scoped to a single group)
/// and builds the normalized SolverInputDto that gets sent to the Python solver service.
/// When groupId is provided, only that group's members and tasks are included.
/// </summary>
public interface ISolverPayloadNormalizer
{
    Task<SolverInputDto> BuildAsync(
        Guid spaceId,
        Guid runId,
        string triggerMode,
        Guid? baselineVersionId,
        Guid? groupId = null,
        CancellationToken ct = default);
}
