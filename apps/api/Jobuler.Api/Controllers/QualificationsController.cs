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
[Route("spaces/{spaceId:guid}/groups/{groupId:guid}/qualifications")]
[Authorize]
public class QualificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissions;

    public QualificationsController(IMediator mediator, IPermissionService permissions)
    { _mediator = mediator; _permissions = permissions; }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List all active qualifications for a group.</summary>
    [HttpGet]
    public async Task<IActionResult> List(Guid spaceId, Guid groupId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceView, ct);
        return Ok(await _mediator.Send(new GetGroupQualificationsQuery(spaceId, groupId), ct));
    }

    /// <summary>Create a new qualification type for this group.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(Guid spaceId, Guid groupId,
        [FromBody] QualificationRequest req, CancellationToken ct)
    {
        var id = await _mediator.Send(
            new CreateGroupQualificationCommand(spaceId, groupId, req.Name, req.Description, CurrentUserId), ct);
        return Created($"/spaces/{spaceId}/groups/{groupId}/qualifications/{id}", new { id });
    }

    /// <summary>Update a qualification's name/description.</summary>
    [HttpPut("{qualificationId:guid}")]
    public async Task<IActionResult> Update(Guid spaceId, Guid groupId, Guid qualificationId,
        [FromBody] QualificationRequest req, CancellationToken ct)
    {
        await _mediator.Send(
            new UpdateGroupQualificationCommand(spaceId, groupId, qualificationId, req.Name, req.Description, CurrentUserId), ct);
        return NoContent();
    }

    /// <summary>Deactivate (soft-delete) a qualification.</summary>
    [HttpDelete("{qualificationId:guid}")]
    public async Task<IActionResult> Deactivate(Guid spaceId, Guid groupId, Guid qualificationId, CancellationToken ct)
    {
        await _mediator.Send(
            new DeactivateGroupQualificationCommand(spaceId, groupId, qualificationId, CurrentUserId), ct);
        return NoContent();
    }

    /// <summary>List all member qualification assignments for this group.</summary>
    [HttpGet("members")]
    public async Task<IActionResult> ListMemberQualifications(Guid spaceId, Guid groupId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceView, ct);
        return Ok(await _mediator.Send(new GetMemberQualificationsQuery(spaceId, groupId), ct));
    }

    /// <summary>Assign a qualification to a member.</summary>
    [HttpPost("members/{personId:guid}")]
    public async Task<IActionResult> Assign(Guid spaceId, Guid groupId, Guid personId,
        [FromBody] AssignQualificationRequest req, CancellationToken ct)
    {
        await _mediator.Send(
            new AssignMemberQualificationCommand(spaceId, groupId, personId, req.QualificationId, CurrentUserId), ct);
        return NoContent();
    }

    /// <summary>Remove a qualification from a member.</summary>
    [HttpDelete("members/{personId:guid}/{qualificationId:guid}")]
    public async Task<IActionResult> Remove(Guid spaceId, Guid groupId, Guid personId, Guid qualificationId, CancellationToken ct)
    {
        await _mediator.Send(
            new RemoveMemberQualificationCommand(spaceId, groupId, personId, qualificationId, CurrentUserId), ct);
        return NoContent();
    }
}

public record QualificationRequest(string Name, string? Description = null);
public record AssignQualificationRequest(Guid QualificationId);
