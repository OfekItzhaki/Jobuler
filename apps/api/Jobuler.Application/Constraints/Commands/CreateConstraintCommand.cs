using Jobuler.Application.Common;
using Jobuler.Domain.Constraints;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Constraints.Commands;

public record CreateConstraintCommand(
    Guid SpaceId,
    ConstraintScopeType ScopeType,
    Guid? ScopeId,
    ConstraintSeverity Severity,
    string RuleType,
    string RulePayloadJson,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveUntil,
    Guid RequestingUserId) : IRequest<Guid>;

public class CreateConstraintCommandHandler : IRequestHandler<CreateConstraintCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public CreateConstraintCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<Guid> Handle(CreateConstraintCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.ConstraintsManage, ct);

        // ── Role scope validation ─────────────────────────────────────────────
        if (req.ScopeType == ConstraintScopeType.Role)
        {
            if (req.ScopeId is null)
                throw new ArgumentException("ScopeId is required for role-scoped constraints.");

            var roleExists = await _db.SpaceRoles.AsNoTracking()
                .AnyAsync(r => r.Id == req.ScopeId.Value
                    && r.SpaceId == req.SpaceId
                    && r.IsActive, ct);

            if (!roleExists)
                throw new KeyNotFoundException("Role not found in this space.");
        }

        // ── Person scope validation ───────────────────────────────────────────
        if (req.ScopeType == ConstraintScopeType.Person)
        {
            if (req.ScopeId is null)
                throw new ArgumentException("ScopeId is required for person-scoped constraints.");

            var person = await _db.People.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == req.ScopeId.Value
                    && p.SpaceId == req.SpaceId, ct)
                ?? throw new KeyNotFoundException("Person not found in this space.");

            if (person.LinkedUserId is null || person.InvitationStatus != "accepted")
                throw new DomainValidationException(
                    "Personal constraints can only be applied to registered members.");
        }

        var rule = ConstraintRule.Create(
            req.SpaceId, req.ScopeType, req.ScopeId,
            req.Severity, req.RuleType, req.RulePayloadJson,
            req.RequestingUserId, req.EffectiveFrom, req.EffectiveUntil);

        _db.ConstraintRules.Add(rule);
        await _db.SaveChangesAsync(ct);
        return rule.Id;
    }
}
