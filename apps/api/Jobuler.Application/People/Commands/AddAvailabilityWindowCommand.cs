using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.People.Commands;

public record AddAvailabilityWindowCommand(
    Guid SpaceId, Guid PersonId,
    DateTime StartsAt, DateTime EndsAt,
    string? Note, Guid RequestingUserId) : IRequest<Guid>;

public class AddAvailabilityWindowCommandHandler
    : IRequestHandler<AddAvailabilityWindowCommand, Guid>
{
    private readonly AppDbContext _db;
    public AddAvailabilityWindowCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(AddAvailabilityWindowCommand req, CancellationToken ct)
    {
        var window = AvailabilityWindow.Create(
            req.SpaceId, req.PersonId, req.StartsAt, req.EndsAt, req.Note);
        _db.AvailabilityWindows.Add(window);
        await _db.SaveChangesAsync(ct);
        return window.Id;
    }
}
