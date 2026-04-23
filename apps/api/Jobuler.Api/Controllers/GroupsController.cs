using Jobuler.Application.Common;
using Jobuler.Application.Groups.Commands;
using Jobuler.Application.Groups.Queries;
using Jobuler.Domain.Spaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobuler.Api.Controllers;

[ApiController]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissions;

    public GroupsController(IMediator mediator, IPermissionService permissions)
    {
        _mediator = mediator;
        _permissions = permissions;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Group Types ───────────────────────────────────────────────────────────

    [HttpGet("spaces/{spaceId:guid}/group-types")]
    public async Task<IActionResult> ListGroupTypes(Guid spaceId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceView, ct);
        return Ok(await _mediator.Send(new GetGroupTypesQuery(spaceId), ct));
    }

    [HttpPost("spaces/{spaceId:guid}/group-types")]
    public async Task<IActionResult> CreateGroupType(Guid spaceId,
        [FromBody] CreateGroupTypeRequest req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.PeopleManage, ct);
        var id = await _mediator.Send(new CreateGroupTypeCommand(spaceId, req.Name, req.Description), ct);
        return Created("", new { id });
    }

    // ── Groups ────────────────────────────────────────────────────────────────

    [HttpGet("spaces/{spaceId:guid}/groups")]
    public async Task<IActionResult> ListGroups(Guid spaceId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceView, ct);
        return Ok(await _mediator.Send(new GetGroupsQuery(spaceId), ct));
    }

    [HttpPost("spaces/{spaceId:guid}/groups")]
    public async Task<IActionResult> CreateGroup(Guid spaceId,
        [FromBody] CreateGroupRequest req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.PeopleManage, ct);
        var id = await _mediator.Send(
            new CreateGroupCommand(spaceId, req.GroupTypeId, req.Name, req.Description), ct);
        return Created("", new { id });
    }

    [HttpGet("spaces/{spaceId:guid}/groups/{groupId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid spaceId, Guid groupId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceView, ct);
        return Ok(await _mediator.Send(new GetGroupMembersQuery(spaceId, groupId), ct));
    }

    [HttpPost("spaces/{spaceId:guid}/groups/{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid spaceId, Guid groupId,
        [FromBody] AddMemberRequest req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.PeopleManage, ct);
        await _mediator.Send(new AddPersonToGroupCommand(spaceId, groupId, req.PersonId), ct);
        return NoContent();
    }
}

public record CreateGroupTypeRequest(string Name, string? Description);
public record CreateGroupRequest(Guid? GroupTypeId, string Name, string? Description);
public record AddMemberRequest(Guid PersonId);
