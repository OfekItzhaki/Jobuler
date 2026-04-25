using FluentValidation;
using Jobuler.Application.Common;
using Jobuler.Domain.Groups;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

// ── Create ────────────────────────────────────────────────────────────────────

public record CreateGroupMessageCommand(
    Guid SpaceId, Guid GroupId, Guid AuthorUserId,
    string Content, bool IsPinned) : IRequest<Guid>;

public class CreateGroupMessageCommandHandler : IRequestHandler<CreateGroupMessageCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public CreateGroupMessageCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<Guid> Handle(CreateGroupMessageCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.AuthorUserId, req.SpaceId, Permissions.PeopleManage, ct);

        var message = GroupMessage.Create(req.SpaceId, req.GroupId, req.AuthorUserId, req.Content, req.IsPinned);
        _db.GroupMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message.Id;
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public record DeleteGroupMessageCommand(
    Guid SpaceId, Guid GroupId, Guid MessageId, Guid RequestingUserId) : IRequest;

public class DeleteGroupMessageCommandHandler : IRequestHandler<DeleteGroupMessageCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public DeleteGroupMessageCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(DeleteGroupMessageCommand req, CancellationToken ct)
    {
        var message = await _db.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == req.MessageId
                                   && m.GroupId == req.GroupId
                                   && m.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        // Admin with people.manage can delete any message; otherwise must be the author
        var hasAdminPermission = await _permissions.HasPermissionAsync(
            req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        if (!hasAdminPermission && message.AuthorUserId != req.RequestingUserId)
            throw new UnauthorizedAccessException("Only the author or an admin can delete this message.");

        _db.GroupMessages.Remove(message);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateGroupMessageCommand(
    Guid SpaceId, Guid GroupId, Guid MessageId,
    Guid RequestingUserId, string Content) : IRequest;

public class UpdateGroupMessageCommandValidator : AbstractValidator<UpdateGroupMessageCommand>
{
    public UpdateGroupMessageCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(5000)
            .Must(c => !string.IsNullOrWhiteSpace(c))
            .WithMessage("Content must be between 1 and 5000 non-blank characters.");
    }
}

public class UpdateGroupMessageCommandHandler : IRequestHandler<UpdateGroupMessageCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public UpdateGroupMessageCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(UpdateGroupMessageCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        var message = await _db.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == req.MessageId
                                   && m.GroupId == req.GroupId
                                   && m.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        message.Update(req.Content, message.IsPinned);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Pin ───────────────────────────────────────────────────────────────────────

public record PinGroupMessageCommand(
    Guid SpaceId, Guid GroupId, Guid MessageId,
    Guid RequestingUserId, bool IsPinned) : IRequest;

public class PinGroupMessageCommandHandler : IRequestHandler<PinGroupMessageCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public PinGroupMessageCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(PinGroupMessageCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        var message = await _db.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == req.MessageId
                                   && m.GroupId == req.GroupId
                                   && m.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Message not found.");

        message.Update(message.Content, req.IsPinned);
        await _db.SaveChangesAsync(ct);
    }
}
