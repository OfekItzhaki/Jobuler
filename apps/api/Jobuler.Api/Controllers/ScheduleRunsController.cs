using Jobuler.Application.Common;
using Jobuler.Application.Scheduling.Commands;
using Jobuler.Domain.Spaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobuler.Api.Controllers;

[ApiController]
[Route("spaces/{spaceId:guid}/schedule-runs")]
[Authorize]
public class ScheduleRunsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissions;

    public ScheduleRunsController(IMediator mediator, IPermissionService permissions)
    {
        _mediator = mediator;
        _permissions = permissions;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Trigger a solver run. Returns the RunId immediately — solve happens asynchronously.
    /// Poll GET /schedule-runs/{runId} to check status.
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(
        Guid spaceId, [FromBody] TriggerSolverRequest req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(
            CurrentUserId, spaceId, Permissions.ScheduleRecalculate, ct);

        var runId = await _mediator.Send(
            new TriggerSolverCommand(spaceId, req.TriggerMode ?? "standard", CurrentUserId), ct);

        return Accepted(new { runId });
    }
}

public record TriggerSolverRequest(string? TriggerMode);
