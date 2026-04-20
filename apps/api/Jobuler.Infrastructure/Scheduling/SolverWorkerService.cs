using Jobuler.Application.Scheduling;
using Jobuler.Domain.Scheduling;
using Jobuler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Jobuler.Infrastructure.Scheduling;

/// <summary>
/// Background worker that dequeues solver jobs from Redis,
/// calls the Python solver, and stores the resulting draft schedule version.
/// Runs as a hosted service alongside the API process.
/// </summary>
public class SolverWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SolverWorkerService> _logger;

    public SolverWorkerService(IServiceScopeFactory scopeFactory, ILogger<SolverWorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Solver worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unhandled error in solver worker loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var queue      = scope.ServiceProvider.GetRequiredService<ISolverJobQueue>();
        var normalizer = scope.ServiceProvider.GetRequiredService<ISolverPayloadNormalizer>();
        var client     = scope.ServiceProvider.GetRequiredService<ISolverClient>();
        var db         = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await queue.DequeueAsync(ct);
        if (job is null)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return;
        }

        _logger.LogInformation("Processing solver job: run_id={RunId}", job.RunId);

        // Load the run record — must exist (created by the trigger command)
        var run = await db.ScheduleRuns
            .FirstOrDefaultAsync(r => r.Id == job.RunId && r.SpaceId == job.SpaceId, ct);

        if (run is null)
        {
            _logger.LogWarning("Solver job references unknown run_id={RunId}. Skipping.", job.RunId);
            return;
        }

        // Idempotency: skip if already processed
        if (run.Status is ScheduleRunStatus.Completed or ScheduleRunStatus.TimedOut or ScheduleRunStatus.Failed)
        {
            _logger.LogInformation("Run {RunId} already processed (status={Status}). Skipping.", job.RunId, run.Status);
            return;
        }

        try
        {
            // Build solver input
            var input = await normalizer.BuildAsync(
                job.SpaceId, job.RunId, job.TriggerMode, job.BaselineVersionId, ct);

            var inputHash = ComputeHash(input);
            run.MarkRunning(inputHash);
            await db.SaveChangesAsync(ct);

            // Call solver
            var output = await client.SolveAsync(input, ct);

            // Determine next version number for this space
            var nextVersion = await db.ScheduleVersions
                .Where(v => v.SpaceId == job.SpaceId)
                .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;
            nextVersion++;

            // Create draft version
            var summaryJson = JsonSerializer.Serialize(new
            {
                feasible = output.Feasible,
                timed_out = output.TimedOut,
                stability = output.StabilityMetrics,
                explanation = output.ExplanationFragments,
                uncovered_slots = output.UncoveredSlotIds.Count,
                hard_conflicts = output.HardConflicts.Count
            });

            var version = ScheduleVersion.CreateDraft(
                job.SpaceId, nextVersion, job.BaselineVersionId,
                job.RunId, job.RequestedByUserId, summaryJson);

            db.ScheduleVersions.Add(version);
            await db.SaveChangesAsync(ct); // get version.Id

            // Persist assignments
            var assignments = output.Assignments.Select(a => Assignment.Create(
                job.SpaceId, version.Id,
                Guid.Parse(a.SlotId), Guid.Parse(a.PersonId),
                a.Source == "override" ? AssignmentSource.Override : AssignmentSource.Solver))
                .ToList();

            db.Assignments.AddRange(assignments);

            // Compute and store diff summary
            var baseline = job.BaselineVersionId.HasValue
                ? await db.Assignments.AsNoTracking()
                    .Where(a => a.ScheduleVersionId == job.BaselineVersionId.Value)
                    .Select(a => new { a.TaskSlotId, a.PersonId })
                    .ToListAsync(ct)
                : new();

            var newSet      = assignments.Select(a => (a.TaskSlotId, a.PersonId)).ToHashSet();
            var baselineSet = baseline.Select(a => (a.TaskSlotId, a.PersonId)).ToHashSet();

            var added   = newSet.Except(baselineSet).Count();
            var removed = baselineSet.Except(newSet).Count();
            var changed = Math.Min(added, removed); // approximation

            var diffSummary = AssignmentChangeSummary.Create(
                job.SpaceId, version.Id, job.BaselineVersionId,
                added, removed, changed,
                (decimal?)output.StabilityMetrics.TotalStabilityPenalty,
                JsonSerializer.Serialize(output.StabilityMetrics));

            db.AssignmentChangeSummaries.Add(diffSummary);

            // Mark run completed
            if (output.TimedOut)
                run.MarkTimedOut(summaryJson);
            else
                run.MarkCompleted(summaryJson);

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Solver job completed: run_id={RunId} version={Version} feasible={Feasible} assignments={Count}",
                job.RunId, nextVersion, output.Feasible, assignments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Solver job failed: run_id={RunId}", job.RunId);
            run.MarkFailed(ex.Message);
            await db.SaveChangesAsync(ct);
        }
    }

    private static string ComputeHash(object input)
    {
        var json = JsonSerializer.Serialize(input);
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
