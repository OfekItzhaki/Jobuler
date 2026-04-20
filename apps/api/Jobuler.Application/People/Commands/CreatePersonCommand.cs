using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.People.Commands;

public record CreatePersonCommand(
    Guid SpaceId,
    string FullName,
    string? DisplayName,
    Guid? LinkedUserId,
    Guid RequestingUserId) : IRequest<Guid>;

public class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, Guid>
{
    private readonly AppDbContext _db;

    public CreatePersonCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreatePersonCommand req, CancellationToken ct)
    {
        var person = Person.Create(req.SpaceId, req.FullName, req.DisplayName, req.LinkedUserId);
        _db.People.Add(person);
        await _db.SaveChangesAsync(ct);
        return person.Id;
    }
}
