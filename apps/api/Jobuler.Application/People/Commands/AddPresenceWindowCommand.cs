using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.People.Commands;

public record AddPresenceWindowCommand(
    Guid SpaceId, Guid PersonId,
    string State,  // free_in_base | at_home
    DateTime StartsAt, DateTime EndsAt,
    string? Note, Guid RequestingUserId) : IRequest<Guid>;

public class AddPresenceWindowCommandHandler
    : IRequestHandler<AddPresenceWindowCommand, Guid>
{
    private readonly AppDbContext _db;
    public AddPresenceWindowCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(AddPresenceWindowCommand req, CancellationToken ct)
    {
        var state = req.State switch
        {
            "at_home"      => PresenceState.AtHome,
            "free_in_base" => PresenceState.FreeInBase,
            _ => throw new ArgumentException($"Invalid presence state: {req.State}")
        };

        var window = PresenceWindow.CreateManual(
            req.SpaceId, req.PersonId, state,
            req.StartsAt, req.EndsAt, req.Note);

        _db.PresenceWindows.Add(window);
        await _db.SaveChangesAsync(ct);
        return window.Id;
    }
}
