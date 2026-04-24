using Jobuler.Domain.Groups;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

public record CreateGroupTypeCommand(
    Guid SpaceId, string Name, string? Description) : IRequest<Guid>;

public class CreateGroupTypeCommandHandler : IRequestHandler<CreateGroupTypeCommand, Guid>
{
    private readonly AppDbContext _db;
    public CreateGroupTypeCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateGroupTypeCommand req, CancellationToken ct)
    {
        var gt = GroupType.Create(req.SpaceId, req.Name, req.Description);
        _db.GroupTypes.Add(gt);
        await _db.SaveChangesAsync(ct);
        return gt.Id;
    }
}

public record CreateGroupCommand(
    Guid SpaceId, Guid? GroupTypeId, string Name, string? Description,
    Guid CreatedByUserId) : IRequest<Guid>;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Guid>
{
    private readonly AppDbContext _db;
    public CreateGroupCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateGroupCommand req, CancellationToken ct)
    {
        var person = await _db.People
            .FirstOrDefaultAsync(p => p.SpaceId == req.SpaceId && p.LinkedUserId == req.CreatedByUserId, ct)
            ?? throw new KeyNotFoundException("Creator person not found in this space.");

        var group = Group.Create(req.SpaceId, req.GroupTypeId, req.Name, req.Description, createdByUserId: req.CreatedByUserId);
        _db.Groups.Add(group);

        _db.GroupMemberships.Add(GroupMembership.Create(req.SpaceId, group.Id, person.Id, isOwner: true));

        await _db.SaveChangesAsync(ct);
        return group.Id;
    }
}

public record AddPersonToGroupCommand(
    Guid SpaceId, Guid GroupId, Guid PersonId) : IRequest;

public class AddPersonToGroupCommandHandler : IRequestHandler<AddPersonToGroupCommand>
{
    private readonly AppDbContext _db;
    public AddPersonToGroupCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(AddPersonToGroupCommand req, CancellationToken ct)
    {
        var exists = await _db.GroupMemberships.AnyAsync(
            m => m.GroupId == req.GroupId && m.PersonId == req.PersonId, ct);
        if (exists) return;

        _db.GroupMemberships.Add(GroupMembership.Create(req.SpaceId, req.GroupId, req.PersonId));
        await _db.SaveChangesAsync(ct);
    }
}
