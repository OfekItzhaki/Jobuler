# Step 077 — Solver Empty Draft Fix

## Phase
Phase 3 — Scheduling Core (bugfix)

## Purpose
When the solver returned an infeasible result or zero assignments, the worker was still persisting a `Discarded` version row to the database. This meant the UI could surface an empty draft to admins — a state that should never be visible. The fix ensures no version row is written at all when there is nothing useful to show.

## Root Cause
In `SolverWorkerService.ProcessNextJobAsync`, the original code called `version.Discard()` (setting `Status = Discarded`) but then unconditionally called `db.ScheduleVersions.Add(version)` and saved it. The `Discarded` row was persisted, and any query that didn't explicitly filter it out would return it as an empty draft.

## What Was Built

### Modified
- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverWorkerService.cs`**
  - Replaced the `shouldDiscard` + `version.Discard()` + unconditional `Add` pattern with a conditional block:
    - If `!shouldDiscard` (feasible AND assignments > 0): persist the version, assignments, diff summary, and mark run completed/timed-out as before.
    - If `shouldDiscard`: skip all DB writes for the version entirely, update only the run status (failed or timed-out), then fall through to the unified notification block.
  - Unified the notification/logging block so both paths (success and discard) produce the correct in-app notification and system log entry.
  - Removed the intermediate `goto NotifyAndFinish` approach in favour of a clean `if/else` structure.

## Key Decisions
- **No version row at all** when the result is unusable — not a `Discarded` row, not a `Draft` row with zero assignments. The DB stays clean.
- The `ScheduleVersion.Discard()` domain method is still used by the orphaned-run cleanup path (startup cleanup of runs interrupted by an API restart), which is correct — those versions were already persisted and need to be marked discarded.
- Notification and system log still fire in both paths so admins always know what happened.

## How It Connects
- `SolverWorkerService` is the only writer of `ScheduleVersion` rows from the solver path. This fix is self-contained.
- The orphaned-run cleanup at startup (`CleanupOrphanedRunsAsync`) is unaffected — it correctly discards versions that were already saved before the process died.
- `GetScheduleVersionsQuery` is unchanged; it no longer needs to filter `Discarded` rows because they are no longer created by this path.

## How to Run / Verify
1. Kill any running API process, then start fresh:
   ```
   dotnet run --project Jobuler.Api/Jobuler.Api.csproj
   ```
2. Trigger a solver run via the admin UI or `POST /api/schedule-runs`.
3. If the solver returns infeasible or zero assignments, confirm:
   - No new `schedule_versions` row appears in the DB.
   - The `schedule_runs` row is marked `Failed` with a descriptive reason.
   - An in-app notification is sent to space admins explaining the failure.
4. If the solver returns a valid result, confirm a `Draft` version appears with assignments.

## What Comes Next
- Consider adding a DB-level check constraint or application-layer guard to prevent `Draft` versions with zero assignments from ever being published.

## Git Commit
```bash
git add -A && git commit -m "fix(solver): never persist empty draft when solver returns infeasible or zero assignments"
```
