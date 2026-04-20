using Jobuler.Domain.Tasks;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.Tasks.Commands;

public record CreateTaskSlotCommand(
    Guid SpaceId, Guid TaskTypeId,
    DateTime StartsAt, DateTime EndsAt,
    int RequiredHeadcount, int Priority,
    List<Guid>? RequiredRoleIds,
    List<Guid>? RequiredQualificationIds,
    string? Location,
    Guid RequestingUserId) : IRequest<Guid>;

public class CreateTaskSlotCommandHandler : IRequestHandler<CreateTaskSlotCommand, Guid>
{
    private readonly AppDbContext _db;

    public CreateTaskSlotCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateTaskSlotCommand req, CancellationToken ct)
    {
        var slot = TaskSlot.Create(
            req.SpaceId, req.TaskTypeId, req.StartsAt, req.EndsAt,
            req.RequiredHeadcount, req.Priority, req.RequestingUserId,
            req.RequiredRoleIds, req.RequiredQualificationIds, req.Location);
        _db.TaskSlots.Add(slot);
        await _db.SaveChangesAsync(ct);
        return slot.Id;
    }
}
