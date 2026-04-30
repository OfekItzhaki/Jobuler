# Step 073 — Solver Worker RLS Session Variable Fix

## Phase
Phase 4 — Scheduling Engine

## Purpose
The solver worker was hanging indefinitely on every run. The background worker
dequeued a job and immediately queried `schedule_runs` without first setting the
PostgreSQL session variable `app.current_space_id`. All tenant-scoped tables have
Row Level Security (RLS) policies that filter by this variable. With it unset the
query returned zero rows, causing the worker to log "unknown run_id" and skip the
job — or in some cases block waiting for a result that never came.

The fix sets both `app.current_space_id` and `app.current_user_id` immediately
after dequeuing, before any DB query is executed.

## What was built

### Modified files

- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverWorkerService.cs`**
  Added `ExecuteSqlRawAsync` call to set `app.current_space_id` and
  `app.current_user_id` right after dequeuing the job, before the first
  `db.ScheduleRuns` query.

- **`apps/api/Jobuler.Tests/Application/AdminManagementHandlerTests.cs`**
  Fixed `UpdateConstraintCommand` constructor calls — added the missing `Severity`
  (`null`) parameter that was added to the record in a previous step but not
  reflected in the tests.

- **`apps/api/Jobuler.Tests/Application/ConstraintPropertyTests.cs`**
  Same fix — added missing `Severity` parameter to all `UpdateConstraintCommand`
  instantiations in property-based tests.

## Key decisions

- Set session variables **before** any DB access in the worker scope, not inside
  `SolverPayloadNormalizer`. The normalizer already did this for its own queries,
  but the worker's earlier queries (loading the run record, idempotency check) ran
  before the normalizer was called.
- Used `TRUE` as the third argument to `set_config` so the variable is
  transaction-local and automatically cleared when the connection returns to the
  pool — no risk of leaking one space's context into another job.

## How it connects

The `SolverWorkerService` is a `BackgroundService` that runs outside the HTTP
pipeline. Unlike controllers (which go through `TenantContextMiddleware`), the
worker must set RLS session variables manually. This is the same pattern already
used in `TriggerSolverCommand` and `SolverPayloadNormalizer`.

## How to run / verify

1. Start Memurai/Redis, the Python solver (`uvicorn main:app --workers 2`), and
   the API (`dotnet run`).
2. POST to `/spaces/{spaceId}/schedule-runs/trigger` with a valid JWT.
3. Poll `GET /spaces/{spaceId}/schedule-runs/{runId}` — should reach `Completed`
   within ~5 seconds with `feasible: true`.

Confirmed working: run `c5697e9e` completed in 2486 ms, feasible, 0 uncovered
slots, 0 hard conflicts.

## What comes next

- Manual override assignments (urgent double-shift feature)
- Live person status panel (who's on mission / at home)

## Git commit

```bash
git add -A && git commit -m "fix(solver): set RLS session vars before first DB query in worker"
```
