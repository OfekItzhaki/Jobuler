using Jobuler.Domain.Groups;
using Jobuler.Domain.Notifications;
using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

/// <summary>
/// Adds a person to a group by phone number.
/// - If a User with that phone exists, links the Person to that User.
/// - If a Person already exists in this space with that phone, reuses it.
/// - Otherwise creates a new Person record with the phone number.
/// - Sends an in-app notification if the user has an account.
/// </summary>
public record AddPersonByPhoneCommand(
    Guid SpaceId,
    Guid GroupId,
    string PhoneNumber,
    Guid RequestingUserId) : IRequest<AddPersonByPhoneResult>;

public record AddPersonByPhoneResult(Guid PersonId, bool IsNewPerson, bool HasLinkedUser);

public class AddPersonByPhoneCommandHandler : IRequestHandler<AddPersonByPhoneCommand, AddPersonByPhoneResult>
{
    private readonly AppDbContext _db;
    public AddPersonByPhoneCommandHandler(AppDbContext db) => _db = db;

    public async Task<AddPersonByPhoneResult> Handle(AddPersonByPhoneCommand req, CancellationToken ct)
    {
        var phone = req.PhoneNumber.Trim();

        // 1. Find user account by phone number
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);

        // 2. Find existing person in this space with that phone or linked to that user
        Person? person = null;
        bool isNew = false;

        if (user is not null)
        {
            person = await _db.People
                .FirstOrDefaultAsync(p => p.SpaceId == req.SpaceId && p.LinkedUserId == user.Id, ct);
        }

        if (person is null)
        {
            person = await _db.People
                .FirstOrDefaultAsync(p => p.SpaceId == req.SpaceId && p.PhoneNumber == phone, ct);
        }

        // 3. Create person if not found
        if (person is null)
        {
            var displayName = user?.DisplayName ?? phone;
            person = Person.Create(req.SpaceId, displayName, null, user?.Id, phone);
            _db.People.Add(person);
            isNew = true;
        }

        await _db.SaveChangesAsync(ct);

        // 4. Add to group if not already a member
        var alreadyMember = await _db.GroupMemberships
            .AnyAsync(m => m.GroupId == req.GroupId && m.PersonId == person.Id, ct);

        if (!alreadyMember)
        {
            _db.GroupMemberships.Add(GroupMembership.Create(req.SpaceId, req.GroupId, person.Id));
        }

        // 5. Create invitation record (use phone as email field for tracking)
        var invitation = GroupInvitation.Create(req.SpaceId, req.GroupId, phone, person.Id, req.RequestingUserId);
        _db.GroupInvitations.Add(invitation);

        // 6. Send in-app notification if user has an account
        if (user is not null)
        {
            var group = await _db.Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == req.GroupId, ct);
            var groupName = group?.Name ?? "קבוצה";

            var notification = Notification.Create(
                req.SpaceId, user.Id,
                "group_added",
                $"נוספת לקבוצה: {groupName}",
                $"הוספת לקבוצה \"{groupName}\" באמצעות מספר הטלפון שלך. אם זו טעות, תוכל לעזוב את הקבוצה.",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    groupId = req.GroupId,
                    optOutToken = invitation.OptOutToken
                }));
            _db.Notifications.Add(notification);
        }

        await _db.SaveChangesAsync(ct);
        return new AddPersonByPhoneResult(person.Id, isNew, user is not null);
    }
}
