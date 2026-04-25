using FluentValidation;
using Jobuler.Application.Common;
using Jobuler.Domain.Groups;
using Jobuler.Domain.Spaces;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record GroupAlertDto(
    Guid Id,
    string Title,
    string Body,
    string Severity,
    DateTime CreatedAt,
    Guid CreatedByPersonId,
    string CreatedByDisplayName);

// ── Create ────────────────────────────────────────────────────────────────────

public record CreateGroupAlertCommand(
    Guid SpaceId,
    Guid GroupId,
    Guid RequestingUserId,
    string Title,
    string Body,
    string Severity) : IRequest<Guid>;

public class CreateGroupAlertCommandValidator : AbstractValidator<CreateGroupAlertCommand>
{
    public CreateGroupAlertCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage("Title must be between 1 and 200 non-blank characters.");

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(b => !string.IsNullOrWhiteSpace(b))
            .WithMessage("Body must be between 1 and 2000 non-blank characters.");

        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => new[] { "info", "warning", "critical" }.Contains(s.ToLowerInvariant()))
            .WithMessage("Severity must be info, warning, or critical.");
    }
}

public class CreateGroupAlertCommandHandler : IRequestHandler<CreateGroupAlertCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public CreateGroupAlertCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<Guid> Handle(CreateGroupAlertCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        if (!Enum.TryParse<AlertSeverity>(req.Severity, ignoreCase: true, out var severity))
            throw new InvalidOperationException($"Invalid severity '{req.Severity}'. Must be info, warning, or critical.");

        var person = await _db.People
            .FirstOrDefaultAsync(p => p.SpaceId == req.SpaceId && p.LinkedUserId == req.RequestingUserId, ct)
            ?? throw new InvalidOperationException("No linked person found for this user in the space.");

        var alert = GroupAlert.Create(req.SpaceId, req.GroupId, req.Title, req.Body, severity, person.Id);
        _db.GroupAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);
        return alert.Id;
    }
}

// ── Get ───────────────────────────────────────────────────────────────────────

public record GetGroupAlertsQuery(
    Guid SpaceId,
    Guid GroupId,
    Guid RequestingUserId) : IRequest<List<GroupAlertDto>>;

public class GetGroupAlertsQueryHandler : IRequestHandler<GetGroupAlertsQuery, List<GroupAlertDto>>
{
    private readonly AppDbContext _db;

    public GetGroupAlertsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<GroupAlertDto>> Handle(GetGroupAlertsQuery req, CancellationToken ct)
    {
        var isMember = await _db.GroupMemberships
            .Join(_db.People, m => m.PersonId, p => p.Id, (m, p) => new { m, p })
            .AnyAsync(x => x.m.GroupId == req.GroupId
                        && x.m.SpaceId == req.SpaceId
                        && x.p.LinkedUserId == req.RequestingUserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this group.");

        var alerts = await _db.GroupAlerts.AsNoTracking()
            .Where(a => a.GroupId == req.GroupId && a.SpaceId == req.SpaceId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var personIds = alerts.Select(a => a.CreatedByPersonId).Distinct().ToList();
        var people = await _db.People.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.DisplayName ?? p.FullName, ct);

        return alerts.Select(a => new GroupAlertDto(
            a.Id,
            a.Title,
            a.Body,
            a.Severity.ToString().ToLowerInvariant(),
            a.CreatedAt,
            a.CreatedByPersonId,
            people.GetValueOrDefault(a.CreatedByPersonId, "Unknown")))
            .ToList();
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public record DeleteGroupAlertCommand(
    Guid SpaceId,
    Guid GroupId,
    Guid AlertId,
    Guid RequestingUserId) : IRequest;

public class DeleteGroupAlertCommandHandler : IRequestHandler<DeleteGroupAlertCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public DeleteGroupAlertCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(DeleteGroupAlertCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        var alert = await _db.GroupAlerts
            .FirstOrDefaultAsync(a => a.Id == req.AlertId
                                   && a.GroupId == req.GroupId
                                   && a.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Alert not found.");

        // Any user with people.manage can delete any alert (no ownership check)
        _db.GroupAlerts.Remove(alert);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateGroupAlertCommand(
    Guid SpaceId,
    Guid GroupId,
    Guid AlertId,
    Guid RequestingUserId,
    string Title,
    string Body,
    string Severity) : IRequest;

public class UpdateGroupAlertCommandValidator : AbstractValidator<UpdateGroupAlertCommand>
{
    public UpdateGroupAlertCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage("Title must be between 1 and 200 non-blank characters.");

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(b => !string.IsNullOrWhiteSpace(b))
            .WithMessage("Body must be between 1 and 2000 non-blank characters.");

        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => new[] { "info", "warning", "critical" }.Contains(s.ToLowerInvariant()))
            .WithMessage("Severity must be info, warning, or critical.");
    }
}

public class UpdateGroupAlertCommandHandler : IRequestHandler<UpdateGroupAlertCommand>
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;

    public UpdateGroupAlertCommandHandler(AppDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task Handle(UpdateGroupAlertCommand req, CancellationToken ct)
    {
        await _permissions.RequirePermissionAsync(req.RequestingUserId, req.SpaceId, Permissions.PeopleManage, ct);

        var alert = await _db.GroupAlerts
            .FirstOrDefaultAsync(a => a.Id == req.AlertId
                                   && a.GroupId == req.GroupId
                                   && a.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Alert not found.");

        if (!Enum.TryParse<AlertSeverity>(req.Severity, ignoreCase: true, out var severity))
            throw new InvalidOperationException($"Invalid severity '{req.Severity}'.");

        alert.Update(req.Title, req.Body, severity);
        await _db.SaveChangesAsync(ct);
    }
}
