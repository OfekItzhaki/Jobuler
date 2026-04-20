using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.People.Commands;

public record UpdatePersonCommand(
    Guid SpaceId, Guid PersonId,
    string FullName, string? DisplayName, string? ProfileImageUrl,
    Guid RequestingUserId) : IRequest;

public class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand>
{
    private readonly AppDbContext _db;

    public UpdatePersonCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(UpdatePersonCommand req, CancellationToken ct)
    {
        var person = await _db.People
            .FirstOrDefaultAsync(p => p.Id == req.PersonId && p.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Person not found.");

        person.Update(req.FullName, req.DisplayName, req.ProfileImageUrl);
        await _db.SaveChangesAsync(ct);
    }
}
