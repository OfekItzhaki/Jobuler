using Jobuler.Domain.Tasks;
using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Tasks.Queries;

public record TaskTypeDto(
    Guid Id, string Name, string? Description,
    TaskBurdenLevel BurdenLevel, int DefaultPriority,
    bool AllowsOverlap, bool IsActive);

public record TaskSlotDto(
    Guid Id, Guid TaskTypeId, string TaskTypeName,
    DateTime StartsAt, DateTime EndsAt,
    int RequiredHeadcount, int Priority, string Status);

public record GetTaskTypesQuery(Guid SpaceId) : IRequest<List<TaskTypeDto>>;

public class GetTaskTypesQueryHandler : IRequestHandler<GetTaskTypesQuery, List<TaskTypeDto>>
{
    private readonly AppDbContext _db;

    public GetTaskTypesQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<TaskTypeDto>> Handle(GetTaskTypesQuery req, CancellationToken ct) =>
        await _db.TaskTypes.AsNoTracking()
            .Where(t => t.SpaceId == req.SpaceId && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TaskTypeDto(t.Id, t.Name, t.Description,
                t.BurdenLevel, t.DefaultPriority, t.AllowsOverlap, t.IsActive))
            .ToListAsync(ct);
}

public record GetTaskSlotsQuery(Guid SpaceId, DateTime? From, DateTime? To) : IRequest<List<TaskSlotDto>>;

public class GetTaskSlotsQueryHandler : IRequestHandler<GetTaskSlotsQuery, List<TaskSlotDto>>
{
    private readonly AppDbContext _db;

    public GetTaskSlotsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<TaskSlotDto>> Handle(GetTaskSlotsQuery req, CancellationToken ct)
    {
        var query = _db.TaskSlots.AsNoTracking()
            .Where(s => s.SpaceId == req.SpaceId && s.Status == TaskSlotStatus.Active);

        if (req.From.HasValue) query = query.Where(s => s.EndsAt >= req.From.Value);
        if (req.To.HasValue)   query = query.Where(s => s.StartsAt <= req.To.Value);

        return await query
            .Join(_db.TaskTypes, s => s.TaskTypeId, t => t.Id,
                (s, t) => new TaskSlotDto(s.Id, s.TaskTypeId, t.Name,
                    s.StartsAt, s.EndsAt, s.RequiredHeadcount, s.Priority, s.Status.ToString()))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);
    }
}
