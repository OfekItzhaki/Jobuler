using FluentValidation;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Commands;

public record RenameGroupCommand(Guid SpaceId, Guid GroupId, Guid RequestingUserId, string NewName) : IRequest;

public class RenameGroupCommandValidator : AbstractValidator<RenameGroupCommand>
{
    public RenameGroupCommandValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .MaximumLength(100)
            .Must(n => !string.IsNullOrWhiteSpace(n))
            .WithMessage("Group name must be between 1 and 100 non-blank characters.");
    }
}

public class RenameGroupCommandHandler : IRequestHandler<RenameGroupCommand>
{
    private readonly AppDbContext _db;
    public RenameGroupCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(RenameGroupCommand req, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == req.GroupId && g.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        var ownerMembership = await _db.GroupMemberships
            .Join(_db.People, m => m.PersonId, p => p.Id, (m, p) => new { m, p })
            .FirstOrDefaultAsync(x => x.m.GroupId == req.GroupId && x.m.IsOwner && x.p.LinkedUserId == req.RequestingUserId, ct);
        if (ownerMembership is null)
            throw new UnauthorizedAccessException("Only the group owner can rename the group.");

        group.Rename(req.NewName);
        await _db.SaveChangesAsync(ct);
    }
}
