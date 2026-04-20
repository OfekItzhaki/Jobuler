using Jobuler.Application.Spaces.Commands;
using Jobuler.Application.Spaces.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobuler.Api.Controllers;

[ApiController]
[Route("spaces")]
[Authorize]
public class SpacesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpacesController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List all spaces the current user belongs to.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMySpaces(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMySpacesQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Get a single space by ID.</summary>
    [HttpGet("{spaceId:guid}")]
    public async Task<IActionResult> GetSpace(Guid spaceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSpaceQuery(spaceId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new space. The requesting user becomes the owner.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateSpace([FromBody] CreateSpaceRequest req, CancellationToken ct)
    {
        var spaceId = await _mediator.Send(
            new CreateSpaceCommand(req.Name, req.Description, req.Locale ?? "he", CurrentUserId), ct);
        return CreatedAtAction(nameof(GetSpace), new { spaceId }, new { spaceId });
    }

    /// <summary>Transfer space ownership to another user.</summary>
    [HttpPost("{spaceId:guid}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(
        Guid spaceId, [FromBody] TransferOwnershipRequest req, CancellationToken ct)
    {
        await _mediator.Send(
            new TransferOwnershipCommand(spaceId, req.NewOwnerUserId, CurrentUserId, req.Reason), ct);
        return NoContent();
    }
}

public record CreateSpaceRequest(string Name, string? Description, string? Locale);
public record TransferOwnershipRequest(Guid NewOwnerUserId, string? Reason);
