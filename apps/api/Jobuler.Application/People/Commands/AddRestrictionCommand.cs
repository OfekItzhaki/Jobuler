using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.People.Commands;

public record AddRestrictionCommand(
    Guid SpaceId,
    Guid PersonId,
    string RestrictionType,
    Guid? TaskTypeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil,
    string? OperationalNote,
    string? SensitiveReason,   // null = no sensitive reason provided
    Guid RequestingUserId) : IRequest<Guid>;

public class AddRestrictionCommandHandler : IRequestHandler<AddRestrictionCommand, Guid>
{
    private readonly AppDbContext _db;

    public AddRestrictionCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(AddRestrictionCommand req, CancellationToken ct)
    {
        var restriction = PersonRestriction.Create(
            req.SpaceId, req.PersonId, req.RestrictionType,
            req.EffectiveFrom, req.EffectiveUntil,
            req.OperationalNote, req.TaskTypeId, req.RequestingUserId);

        _db.PersonRestrictions.Add(restriction);

        // Sensitive reason stored separately — caller must have already verified
        // restrictions.manage_sensitive permission before passing a non-null reason
        if (!string.IsNullOrWhiteSpace(req.SensitiveReason))
        {
            var sensitive = SensitiveRestrictionReason.Create(
                req.SpaceId, restriction.Id, req.SensitiveReason, req.RequestingUserId);
            _db.SensitiveRestrictionReasons.Add(sensitive);
        }

        await _db.SaveChangesAsync(ct);
        return restriction.Id;
    }
}
