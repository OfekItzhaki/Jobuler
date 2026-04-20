using Jobuler.Application.Common;
using Jobuler.Application.Logs.Queries;
using Jobuler.Domain.Spaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobuler.Api.Controllers;

[ApiController]
[Route("spaces/{spaceId:guid}/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissions;

    public LogsController(IMediator mediator, IPermissionService permissions)
    {
        _mediator = mediator;
        _permissions = permissions;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        Guid spaceId,
        [FromQuery] string? severity,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceAdminMode, ct);

        var includeSensitive = await _permissions.HasPermissionAsync(
            CurrentUserId, spaceId, Permissions.LogsViewSensitive, ct);

        var result = await _mediator.Send(new GetSystemLogsQuery(
            spaceId, severity, eventType, from, to,
            includeSensitive, page, pageSize), ct);

        return Ok(result);
    }
}
