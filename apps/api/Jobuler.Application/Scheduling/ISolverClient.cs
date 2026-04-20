using Jobuler.Application.Scheduling.Models;

namespace Jobuler.Application.Scheduling;

/// <summary>
/// HTTP client contract for calling the Python solver service.
/// The API never calls this directly from a controller — only from the background worker.
/// </summary>
public interface ISolverClient
{
    Task<SolverOutputDto> SolveAsync(SolverInputDto input, CancellationToken ct = default);
}
