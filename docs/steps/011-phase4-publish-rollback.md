# Step 011 — Phase 4: Publish, Rollback, and Fairness Counters

## Phase
Phase 4 — Workflow

## Purpose
Complete the draft → publish → rollback lifecycle. After this step, admins can review a solver-generated draft, publish it to make it live for viewers, roll back to any of the last 7 published versions, and poll solver run status. Fairness counters are also updated automatically after each successful solve.

## What was built

### Application layer

| File | Description |
|---|---|
| `Scheduling/Commands/PublishVersionCommand.cs` | Publishes a draft; archives the current published version atomically |
| `Scheduling/Commands/RollbackVersionCommand.cs` | Creates a new draft by copying assignments from a target published version; never mutates history |
| `Scheduling/Commands/UpdateFairnessCountersCommand.cs` | Recomputes rolling fairness counters for all active people after a solve |
| `Scheduling/Queries/GetScheduleVersionsQuery.cs` | List versions, get version detail with assignments + diff, get current published |
| `Scheduling/Queries/GetScheduleRunQuery.cs` | Poll solver run status by RunId |

### API controllers

| File | Endpoints |
|---|---|
| `ScheduleVersionsController.cs` | `GET /schedule-versions`, `GET /schedule-versions/current`, `GET /schedule-versions/{id}`, `POST /{id}/publish`, `POST /{id}/rollback` |
| `ScheduleRunsController.cs` | Added `GET /schedule-runs/{runId}` for status polling |

### Infrastructure

- `SolverWorkerService.cs` — now calls `UpdateFairnessCountersCommand` after each successful solve

## Key decisions

### Publish archives the previous published version
`PublishVersionCommand` sets all currently-published versions to `Archived` before publishing the new one. Only one version is ever `Published` at a time. This is atomic within a single `SaveChangesAsync`.

### Rollback never mutates history
`RollbackVersionCommand` creates a brand new `Draft` version with `RollbackSourceVersionId` pointing to the target. The target version's status changes to `RolledBack` (status only — its assignments are untouched). This matches the spec's immutability requirement exactly.

### Fairness counters update after every solve
`UpdateFairnessCountersCommand` reads all published assignments within the last 30 days and recomputes per-person counters. Uses upsert pattern (insert if not exists for today, update if exists). These counters feed into the next solver run.

## How to run / verify

```bash
TOKEN=...  SPACE="10000000-0000-0000-0000-000000000001"

# Trigger solve
RUN=$(curl -s -X POST "http://localhost:5000/spaces/$SPACE/schedule-runs/trigger" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"triggerMode":"standard"}' | jq -r .runId)

# Poll run status
curl "http://localhost:5000/spaces/$SPACE/schedule-runs/$RUN" -H "Authorization: Bearer $TOKEN"

# List versions
curl "http://localhost:5000/spaces/$SPACE/schedule-versions" -H "Authorization: Bearer $TOKEN"

# Publish latest draft (replace VERSION_ID)
curl -X POST "http://localhost:5000/spaces/$SPACE/schedule-versions/$VERSION_ID/publish" \
  -H "Authorization: Bearer $TOKEN"

# Rollback to a published version
curl -X POST "http://localhost:5000/spaces/$SPACE/schedule-versions/$VERSION_ID/rollback" \
  -H "Authorization: Bearer $TOKEN"
```

## What comes next
- Phase 5: Frontend schedule tables, admin draft review UI, logs UI (built in same commit)

## Git commit

```bash
git add -A && git commit -m "feat(phase4): publish/rollback workflow and fairness counter updates"
```
