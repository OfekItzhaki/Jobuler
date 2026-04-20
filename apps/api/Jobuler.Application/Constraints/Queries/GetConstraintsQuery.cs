using Jobuler.Domain.Constraints;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Constraints.Queries;

public record ConstraintDto(
    Guid Id, ConstraintScopeType ScopeType, Guid? ScopeId,
    ConstraintSeverity Severity, string RuleType, string RulePayloadJson,
    bool IsActive, DateOnly? EffectiveFrom, DateOnly? EffectiveUntil);

public record GetConstraintsQuery(Guid SpaceId, bool ActiveOnly = true) : IRequest<List<ConstraintDto>>;

public class GetConstraintsQueryHandler : IRequestHandler<GetConstraintsQuery, List<ConstraintDto>>
{
    private readonly AppDbContext _db;

    public GetConstraintsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<ConstraintDto>> Handle(GetConstraintsQuery req, CancellationToken ct)
    {
        var query = _db.ConstraintRules.AsNoTracking().Where(c => c.SpaceId == req.SpaceId);
        if (req.ActiveOnly) query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.ScopeType).ThenBy(c => c.RuleType)
            .Select(c => new ConstraintDto(c.Id, c.ScopeType, c.ScopeId,
                c.Severity, c.RuleType, c.RulePayloadJson,
                c.IsActive, c.EffectiveFrom, c.EffectiveUntil))
            .ToListAsync(ct);
    }
}
