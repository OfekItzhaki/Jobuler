using FluentValidation;
using Jobuler.Application.Common;
using Jobuler.Domain.Constraints;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jobuler.Application.Constraints.Commands;

public record UpdateConstraintCommand(
    Guid SpaceId,
    Guid ConstraintId,
    Guid RequestingUserId,
    string RulePayloadJson,
    string? Severity,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveUntil) : IRequest;

public class UpdateConstraintCommandValidator : AbstractValidator<UpdateConstraintCommand>
{
    public UpdateConstraintCommandValidator()
    {
        RuleFor(x => x.RulePayloadJson)
            .NotEmpty()
            .Must(json =>
            {
                try { JsonDocument.Parse(json); return true; }
                catch { return false; }
            })
            .WithMessage("RulePayloadJson must be valid JSON.");

        RuleFor(x => x)
            .Must(x => x.EffectiveUntil == null || x.EffectiveFrom == null || x.EffectiveUntil >= x.EffectiveFrom)
            .WithMessage("effectiveUntil must be on or after effectiveFrom.");
    }
}

public class UpdateConstraintCommandHandler : IRequestHandler<UpdateConstraintCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public UpdateConstraintCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(UpdateConstraintCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.ConstraintsManage, ct);

        var rule = await _db.ConstraintRules
            .FirstOrDefaultAsync(c => c.Id == req.ConstraintId && c.SpaceId == req.SpaceId && c.IsActive, ct)
            ?? throw new KeyNotFoundException("Constraint not found.");

        var severity = req.Severity != null && Enum.TryParse<ConstraintSeverity>(req.Severity, true, out var s) ? s : (ConstraintSeverity?)null;
        rule.Update(req.RulePayloadJson, severity, req.EffectiveUntil, req.RequestingUserId);
        await _db.SaveChangesAsync(ct);
    }
}
