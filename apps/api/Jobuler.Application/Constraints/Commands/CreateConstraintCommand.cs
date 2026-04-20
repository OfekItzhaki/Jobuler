using Jobuler.Domain.Constraints;
using Jobuler.Infrastructure.Persistence;
using MediatR;

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

    public CreateConstraintCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateConstraintCommand req, CancellationToken ct)
    {
        var rule = ConstraintRule.Create(
            req.SpaceId, req.ScopeType, req.ScopeId,
            req.Severity, req.RuleType, req.RulePayloadJson,
            req.RequestingUserId, req.EffectiveFrom, req.EffectiveUntil);

        _db.ConstraintRules.Add(rule);
        await _db.SaveChangesAsync(ct);
        return rule.Id;
    }
}
