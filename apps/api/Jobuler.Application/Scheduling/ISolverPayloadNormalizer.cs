using Jobuler.Application.Scheduling.Models;

namespace Jobuler.Application.Scheduling;

/// <summary>
/// Reads all operational data for a space and builds the normalized
/// SolverInputDto that gets sent to the Python solver service.
/// </summary>
public interface ISolverPayloadNormalizer
{
    Task<SolverInputDto> BuildAsync(
        Guid spaceId,
        Guid runId,
        string triggerMode,
        Guid? baselineVersionId,
        CancellationToken ct = default);
}
