using Jobuler.Application.Common;
using Jobuler.Application.Constraints.Commands;
using Jobuler.Application.Constraints.Queries;
using Jobuler.Domain.Constraints;
using Jobuler.Domain.Spaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobuler.Api.Controllers;

[ApiController]
[Route("spaces/{spaceId:guid}/constraints")]
[Authorize]
public class ConstraintsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissions;

    public ConstraintsController(IMediator mediator, IPermissionService permissions)
    {
        _mediator = mediator;
        _permissions = permissions;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(Guid spaceId, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.SpaceAdminMode, ct);
        return Ok(await _mediator.Send(new GetConstraintsQuery(spaceId), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid spaceId,
        [FromBody] CreateConstraintRequest req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(CurrentUserId, spaceId, Permissions.ConstraintsManage, ct);
        var id = await _mediator.Send(new CreateConstraintCommand(
            spaceId,
            Enum.Parse<ConstraintScopeType>(req.ScopeType, true),
            req.ScopeId,
            Enum.Parse<ConstraintSeverity>(req.Severity, true),
            req.RuleType, req.RulePayloadJson,
            req.EffectiveFrom, req.EffectiveUntil,
            CurrentUserId), ct);
        return Created($"/spaces/{spaceId}/constraints/{id}", new { id });
    }

    [HttpPut("{constraintId:guid}")]
    public async Task<IActionResult> Update(Guid spaceId, Guid constraintId,
        [FromBody] UpdateConstraintRequest req, CancellationToken ct)
    {
        await _mediator.Send(new UpdateConstraintCommand(
            spaceId, constraintId, CurrentUserId,
            req.RulePayloadJson, req.Severity, req.EffectiveFrom, req.EffectiveUntil), ct);
        return NoContent();
    }

    [HttpDelete("{constraintId:guid}")]
    public async Task<IActionResult> Delete(Guid spaceId, Guid constraintId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteConstraintCommand(spaceId, constraintId, CurrentUserId), ct);
        return NoContent();
    }
}

public record CreateConstraintRequest(
    string ScopeType, Guid? ScopeId, string Severity,
    string RuleType, string RulePayloadJson,
    DateOnly? EffectiveFrom, DateOnly? EffectiveUntil);

public record UpdateConstraintRequest(
    string RulePayloadJson,
    string? Severity,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveUntil);
