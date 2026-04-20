# Step 010 — Phase 3: Scheduling Core

## Phase
Phase 3 — Scheduling Core

## Purpose
Build the full scheduling pipeline: domain entities for schedule runs/versions/assignments, solver payload normalization, Redis job queue, background worker, and complete CP-SAT hard constraints with stability and fairness objectives. After this step, an admin can trigger a solve, the system queues it, the worker calls the Python solver, and a draft schedule version is stored.

## What was built

### Domain entities

| File | Description |
|---|---|
| `Domain/Scheduling/ScheduleRun.cs` | Tracks a solver execution: status, timing, input hash, result summary |
| `Domain/Scheduling/ScheduleVersion.cs` | Immutable schedule snapshot; `Publish()` enforces draft-only rule |
| `Domain/Scheduling/Assignment.cs` | Single person↔slot assignment owned by a version |
| `Domain/Scheduling/AssignmentChangeSummary.cs` | Pre-computed diff between a version and its baseline |
| `Domain/Scheduling/FairnessCounter.cs` | Rolling fairness ledger per person — persisted across solver runs |

### Application layer

| File | Description |
|---|---|
| `Application/Scheduling/ISolverPayloadNormalizer.cs` | Interface: builds `SolverInputDto` from DB data |
| `Application/Scheduling/ISolverClient.cs` | Interface: HTTP call to Python solver |
| `Application/Scheduling/ISolverJobQueue.cs` | Interface: enqueue/dequeue solver jobs via Redis |
| `Application/Scheduling/Models/SolverInputDto.cs` | Full typed DTO matching Python `SolverInput` Pydantic model |
| `Application/Scheduling/Models/SolverOutputDto.cs` | Full typed DTO matching Python `SolverOutput` Pydantic model |
| `Application/Scheduling/Commands/TriggerSolverCommand.cs` | Creates a `ScheduleRun`, enqueues the job, returns `RunId` immediately |

### Infrastructure

| File | Description |
|---|---|
| `Infrastructure/Scheduling/SolverPayloadNormalizer.cs` | Reads people, slots, constraints, availability, fairness from DB; builds `SolverInputDto` |
| `Infrastructure/Scheduling/SolverHttpClient.cs` | `HttpClient` wrapper; POST /solve with snake_case JSON serialization |
| `Infrastructure/Scheduling/RedisSolverJobQueue.cs` | Redis list-based queue (`jobuler:solver:jobs`); idempotent dequeue |
| `Infrastructure/Scheduling/SolverWorkerService.cs` | `BackgroundService`; dequeues jobs, calls solver, stores draft version + assignments + diff |
| `Infrastructure/Persistence/Configurations/SchedulingConfiguration.cs` | EF Core Fluent mappings for all scheduling entities |

### API

| File | Description |
|---|---|
| `Api/Controllers/ScheduleRunsController.cs` | `POST /spaces/{id}/schedule-runs/trigger` — requires `schedule.recalculate` permission |

### Python solver

| File | Description |
|---|---|
| `solver/constraints.py` | All hard constraints: headcount, no-overlap, min-rest, qualifications, roles, restrictions, kitchen limits, availability/presence |
| `solver/objectives.py` | Weighted soft objectives: coverage, stability (time-bucketed), fairness burden distribution |
| `solver/engine.py` | Wires constraints + objectives; extracts assignments, stability metrics, fairness metrics, explanation fragments |

## Key decisions

### Solver is always async
The API controller returns `202 Accepted` with a `runId` immediately. The worker processes the job in the background. This prevents HTTP timeouts on large solve runs and matches the spec's async requirement.

### Idempotency via run status check
The worker checks `run.Status` before processing. If a job is already `Completed`, `TimedOut`, or `Failed`, it skips it. This handles duplicate queue messages safely.

### Stability weights are always in the payload
`SolverPayloadNormalizer` always includes `StabilityWeights` (10.0 / 3.0 / 1.0). The solver never hardcodes them. This is an explicit spec requirement.

### Draft version is never auto-published
The worker creates a `Draft` version. Publishing requires an explicit admin action with `schedule.publish` permission. The solver result is never directly live.

### Fairness history feeds into the next solve
`FairnessCounter` rows are read by the normalizer and sent to the solver. The solver uses them to penalise assigning burden tasks to already-burdened people. After each solve, the worker can update these counters (Phase 4 extension).

### CP-SAT objective priority order
Matches spec Section 18 exactly:
1. Hard constraints (enforced, not penalised)
2. Coverage (weight 1000 — effectively mandatory)
3. Stability today+tomorrow (weight = 10.0 × 100 = 1000)
4. Stability days 3–7 (weight = 3.0–1.0 × 100)
5. Fairness burden distribution (weight = burden_level × history_score)

## How it connects
- `TriggerSolverCommand` is called after any admin save that changes scheduling-relevant data
- `SolverWorkerService` runs alongside the API as a `BackgroundService`
- The resulting `ScheduleVersion` (draft) is what the admin reviews in Phase 4
- `AssignmentChangeSummary` feeds the diff UI in Phase 4/5

## How to run / verify

```bash
# Start all services
docker compose -f infra/compose/docker-compose.yml up -d

# Login
TOKEN=$(curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.local","password":"Demo1234!"}' | jq -r .accessToken)

SPACE="10000000-0000-0000-0000-000000000001"

# Trigger a solve
curl -X POST "http://localhost:5000/spaces/$SPACE/schedule-runs/trigger" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"triggerMode":"standard"}'
# Returns: { "runId": "..." }

# Check solver health directly
curl http://localhost:8000/health

# Test solver directly
curl -X POST http://localhost:8000/solve \
  -H "Content-Type: application/json" \
  -d @infra/scripts/test-solver-payload.json
```

## What comes next
- Phase 4: Publish and rollback endpoints; diff display API; schedule version queries
- Phase 4: Fairness counter update after each completed solve
- Phase 5: Admin UI for draft review, diff visualization, publish/rollback

## Git commit

```bash
git add -A && git commit -m "feat(phase3): scheduling domain, solver queue, CP-SAT constraints, and worker"
```
