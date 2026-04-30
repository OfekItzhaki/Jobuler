# Step 079 — Solver End-to-End Fix (Complete)

## Phase
Phase 3 — Scheduling Core (bugfix)

## Purpose
The solver pipeline was completely broken — triggering a run produced either an empty draft or a run stuck in `Running` forever. This step traces and fixes every bug in the chain from trigger → Redis → worker → solver → DB.

## Root Causes Found (in order of discovery)

### 1. Hard `>=` headcount constraint → INFEASIBLE (Python solver)
`add_headcount_constraints` enforced `sum(assignments) >= required_headcount` as a hard CP-SAT constraint. Any slot that couldn't be fully staffed made the entire model INFEASIBLE, returning `feasible=false, assignments=[]`. The worker then discarded the version and the admin saw nothing.

**Fix**: Changed to `<= required_headcount` (cap over-staffing only). Shortfalls are handled by the soft coverage objective (weight=1000), which produces partial results instead of INFEASIBLE.

### 2. `blocked`/`on_mission` presence states ignored (Python solver)
`add_availability_constraints` only checked `state == "at_home"`. States `blocked` and `on_mission` were silently ignored — those people got assigned freely.

**Fix**: All three states (`blocked`, `at_home`, `on_mission`) are now treated as hard blocks via a `BLOCKING_STATES` set.

### 3. `SolverPayloadNormalizer` crashed in tests
`BuildAsync` called `ExecuteSqlRawAsync` unconditionally, throwing on in-memory DB.

**Fix**: Added `if (_db.Database.IsRelational())` guard, matching the pattern already used in the worker.

### 4. Redis job deserialization failure (C# worker)
`RedisSolverJobQueue.DequeueAsync` called `JsonSerializer.Deserialize<SolverJobMessage>(value!)` without `PropertyNameCaseInsensitive = true`. The `SolverJobMessage` record's constructor parameters are camelCase but the serialized JSON uses PascalCase, causing a `JsonException` on every dequeue.

**Fix**: Added `JsonSerializerOptions` with `PropertyNameCaseInsensitive = true` to both `EnqueueAsync` and `DequeueAsync`. Added a try/catch in `DequeueAsync` to log and skip malformed messages rather than crashing the worker loop.

### 5. `stability_score` column overflow (PostgreSQL)
`assignment_change_summaries.stability_score` was `NUMERIC(5,2)` — max value 999.99. With real data (8 people, 82 slots), the solver returns stability penalties of 2000–8000+, causing a `DbUpdateException: 22003 numeric field overflow` on every save.

**Fix**: Widened column to `NUMERIC(18,2)` via migration `026_fix_stability_score_precision.sql`. Updated EF configuration to match.

## What Was Built

### Modified — Python solver
- **`apps/solver/solver/constraints.py`** — headcount `>=` → `<=`, all blocking presence states enforced
- **`apps/solver/solver/engine.py`** — conflict analysis uses `BLOCKING_STATES`

### Modified — .NET API
- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverPayloadNormalizer.cs`** — `IsRelational()` guard on RLS call
- **`apps/api/Jobuler.Infrastructure/Scheduling/RedisSolverJobQueue.cs`** — `PropertyNameCaseInsensitive = true`, try/catch in `DequeueAsync`
- **`apps/api/Jobuler.Infrastructure/Persistence/Configurations/SchedulingConfiguration.cs`** — explicit `numeric(18,2)` for `stability_score`
- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverWorkerService.cs`** — removed empty-draft discard logic that was persisting `Discarded` versions; now skips DB write entirely when result is unusable

### Added — Migrations
- **`infra/migrations/026_fix_stability_score_precision.sql`** — `ALTER TABLE assignment_change_summaries ALTER COLUMN stability_score TYPE NUMERIC(18,2)`

### Added — Tests
- **`apps/api/Jobuler.Tests/Application/SolverEndToEndTests.cs`** — 8 tests hitting the live solver directly
- **`apps/api/Jobuler.Tests/Integration/SolverWorkerPipelineTests.cs`** — 5 tests: normalizer → solver → result verification with real domain entities

## Test Results
**299/299 tests passing**

## How to Verify
1. Ensure Memurai (Redis) and the Python solver are running
2. Start the API: `dotnet run --project Jobuler.Api/Jobuler.Api.csproj`
3. Trigger a solver run from the group settings tab
4. The run should complete in ~10–30 seconds and show a draft with assignments
5. Run tests: `dotnet test Jobuler.Tests/Jobuler.Tests.csproj`

## What Comes Next
- The solver now produces partial results — the UI should surface uncovered slots clearly
- Personal and role constraints spec can proceed
- Consider adding a DB-level check to prevent publishing versions with zero assignments

## Git Commit
```bash
git add -A && git commit -m "fix(solver): 5-bug chain fix — headcount constraint, presence states, Redis deserialization, stability_score overflow, empty draft persistence"
```
