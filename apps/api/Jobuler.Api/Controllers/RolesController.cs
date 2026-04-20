using Jobuler.Application.Common;
using Jobuler.Application.Spaces.Commands;
using Jobuler.Application.Spaces.Queries;
using Jobuler.Domain.Spaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobuler.Api.Controllers;

[ApiController]
[Route("spaces/{spaceId:guid}/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissions;

    public RolesController(IMediator mediator, IPermissionService permissions)
    {
        _mediator = mediator;
        _permissions = permissions;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(Guid spaceId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceView, ct);
        return Ok(await _mediator.Send(new GetSpaceRolesQuery(spaceId), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid spaceId,
        [FromBody] CreateRoleRequest req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.PeopleManage, ct);
        var id = await _mediator.Send(
            new CreateSpaceRoleCommand(spaceId, req.Name, req.Description, CurrentUserId), ct);
        return Created($"/spaces/{spaceId}/roles/{id}", new { id });
    }
}

public record CreateRoleRequest(string Name, string? Description);
