using Jobuler.Application.Scheduling;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Jobuler.Infrastructure.Scheduling;

/// <summary>
/// In-process solver job queue using a thread-safe ConcurrentQueue.
/// No Redis required — works out of the box for single-server deployments.
/// For multi-server deployments, swap this for RedisSolverJobQueue.
/// </summary>
public class InMemorySolverJobQueue : ISolverJobQueue
{
    // Static so it survives DI scope boundaries (worker + trigger command use different scopes)
    private static readonly ConcurrentQueue<SolverJobMessage> _queue = new();
    private readonly ILogger<InMemorySolverJobQueue> _logger;

    public InMemorySolverJobQueue(ILogger<InMemorySolverJobQueue> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(SolverJobMessage job, CancellationToken ct = default)
    {
        _queue.Enqueue(job);
        _logger.LogInformation("Solver job enqueued (in-memory): run_id={RunId} space_id={SpaceId}", job.RunId, job.SpaceId);
        return Task.CompletedTask;
    }

    public Task<SolverJobMessage?> DequeueAsync(CancellationToken ct = default)
    {
        _queue.TryDequeue(out var job);
        return Task.FromResult(job);
    }
}
