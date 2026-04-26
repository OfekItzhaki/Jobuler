using Jobuler.Domain.Groups;
using Jobuler.Domain.Notifications;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

/// <summary>
/// Adds an existing Person (already in the space) to a group by their PersonId.
/// Used when creating a name-only person and immediately adding them to a group.
/// </summary>
public record AddPersonToGroupByIdCommand(
    Guid SpaceId,
    Guid GroupId,
    Guid PersonId,
    Guid RequestingUserId) : IRequest;

public class AddPersonToGroupByIdCommandHandler : IRequestHandler<AddPersonToGroupByIdCommand>
{
    private readonly AppDbContext _db;

    public AddPersonToGroupByIdCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(AddPersonToGroupByIdCommand req, CancellationToken ct)
    {
        // Verify person exists in this space
        var person = await _db.People
            .FirstOrDefaultAsync(p => p.Id == req.PersonId && p.SpaceId == req.SpaceId && p.IsActive, ct)
            ?? throw new KeyNotFoundException("Person not found in this space.");

        // Idempotent — skip if already a member
        var alreadyMember = await _db.GroupMemberships
            .AnyAsync(m => m.GroupId == req.GroupId && m.PersonId == req.PersonId, ct);

        if (!alreadyMember)
        {
            _db.GroupMemberships.Add(GroupMembership.Create(req.SpaceId, req.GroupId, req.PersonId));

            // Notify linked user if they have an account
            if (person.LinkedUserId.HasValue)
            {
                var group = await _db.Groups.AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == req.GroupId, ct);
                var groupName = group?.Name ?? "קבוצה";

                _db.Notifications.Add(Notification.Create(
                    req.SpaceId, person.LinkedUserId.Value,
                    "group.member_added",
                    $"נוספת לקבוצה: {groupName}",
                    $"הוספת לקבוצה \"{groupName}\".",
                    System.Text.Json.JsonSerializer.Serialize(new { groupId = req.GroupId })));
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
