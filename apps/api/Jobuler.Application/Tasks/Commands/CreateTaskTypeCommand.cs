using Jobuler.Domain.Tasks;
using Jobuler.Infrastructure.Persistence;
using MediatR;

namespace Jobuler.Application.Tasks.Commands;

public record CreateTaskTypeCommand(
    Guid SpaceId, string Name, string? Description,
    TaskBurdenLevel BurdenLevel, int DefaultPriority,
    bool AllowsOverlap, Guid RequestingUserId) : IRequest<Guid>;

public class CreateTaskTypeCommandHandler : IRequestHandler<CreateTaskTypeCommand, Guid>
{
    private readonly AppDbContext _db;

    public CreateTaskTypeCommandHandler(AppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateTaskTypeCommand req, CancellationToken ct)
    {
        var taskType = TaskType.Create(req.SpaceId, req.Name, req.BurdenLevel,
            req.RequestingUserId, req.Description, req.DefaultPriority, req.AllowsOverlap);
        _db.TaskTypes.Add(taskType);
        await _db.SaveChangesAsync(ct);
        return taskType.Id;
    }
}
