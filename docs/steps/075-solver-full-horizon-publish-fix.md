# Step 075 — Solver Full Horizon + Publish Fix + Daily Time Window

## Phase
Phase 4 — Scheduling Engine

## Purpose
Two bugs and one missing feature were addressed together:

1. **Publish bug** — After publishing a schedule, members saw no assignments in "My Missions". The `GetMyAssignmentsQuery` only joined against the legacy `task_slots` table. Solver-generated assignments use derived shift GUIDs (not real `task_slots` rows), so the join returned nothing for every solver-produced assignment.

2. **3-day cap bug** — The solver only generated shifts for 3 days even when tasks were configured as 24/7. A `SchedulingWindowDays = 3` constant in `SolverPayloadNormalizer` capped shift generation to 3 days and capped the safety limit at 48 shifts per task — far too low for a 7-day 24/7 task with short shifts.

3. **Daily time window feature** — Tasks had no way to restrict shifts to a specific time-of-day window (e.g. 08:00–22:00). Added `DailyStartTime` / `DailyEndTime` fields so tasks can be either 24/7 (null) or restricted to a daily window.

## What was built

### Backend — API

- **`apps/api/Jobuler.Domain/Tasks/GroupTask.cs`**
  - Added `DailyStartTime` and `DailyEndTime` (`TimeOnly?`) properties.
  - Updated `Create` and `Update` factory methods to accept the new optional parameters.

- **`apps/api/Jobuler.Infrastructure/Persistence/Configurations/TasksConfiguration.cs`**
  - Mapped `daily_start_time` and `daily_end_time` columns with `TimeOnly ↔ TimeSpan` conversion (EF Core's native `TimeOnly` support via `TimeSpan`).

- **`apps/api/Jobuler.Application/Tasks/Commands/GroupTaskCommands.cs`**
  - Added `DailyStartTime` / `DailyEndTime` to `GroupTaskDto`, `CreateGroupTaskCommand`, and `UpdateGroupTaskCommand`.
  - Updated `CreateGroupTaskCommandHandler` and `UpdateGroupTaskCommandHandler` to pass the new fields through.

- **`apps/api/Jobuler.Application/Tasks/Queries/GetGroupTasksQuery.cs`**
  - Updated DTO projection to include `DailyStartTime` and `DailyEndTime` as `"HH:mm"` strings (null when not set).

- **`apps/api/Jobuler.Api/Controllers/TasksController.cs`**
  - Added `DailyStartTime` / `DailyEndTime` to `CreateGroupTaskRequest` and `UpdateGroupTaskRequest`.
  - Added `ParseTime(string?)` helper to convert `"HH:mm"` → `TimeOnly?`.
  - Updated `CreateGroupTask` and `UpdateGroupTask` actions to pass parsed values to commands.

- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverPayloadNormalizer.cs`**
  - Removed `SchedulingWindowDays = 3` constant — shift generation now uses the full solver horizon (up to 7 days).
  - Raised the per-task shift cap from 48 to 336 (7 days × 48 half-hour slots).
  - Added daily time window filtering: when `DailyStartTime`/`DailyEndTime` are set, shifts outside the window are skipped and the loop advances to the next day's window start.

- **`apps/api/Jobuler.Application/Scheduling/Queries/GetMyAssignmentsQuery.cs`**
  - **Root cause fix**: replaced the `TaskSlots` JOIN (which missed solver-generated assignments) with a two-pass lookup:
    1. Legacy `task_slots` join for any assignments that reference real slot rows.
    2. GroupTask shift-GUID reverse lookup (same algorithm as `GetScheduleVersionDetailQuery`) for solver-generated assignments.
  - Date range filter now applied in-memory after resolving slot metadata, so both legacy and solver assignments are filtered correctly.
  - Group name now resolved from the task's own `GroupId` when available, falling back to the person's first group.

### Database

- **`infra/migrations/024_tasks_daily_time_window.sql`**
  - Adds `daily_start_time TIME` and `daily_end_time TIME` columns to the `tasks` table (nullable).
  - Adds a CHECK constraint: both must be set together, and end must be after start.

### Frontend

- **`apps/web/lib/api/tasks.ts`**
  - Added `dailyStartTime` / `dailyEndTime` to `GroupTaskDto` and `GroupTaskPayload`.

- **`apps/web/app/groups/[groupId]/tabs/TasksTab.tsx`**
  - Added `dailyStartTime` / `dailyEndTime` to `TaskForm` interface.
  - Added a "חלון שעות יומי" (Daily time window) section to the task create/edit modal with two `<input type="time">` fields. Empty = 24/7.

- **`apps/web/app/groups/[groupId]/page.tsx`**
  - Added `dailyStartTime: ""` / `dailyEndTime: ""` to `DEFAULT_TASK_FORM`.
  - Updated `handleTaskSubmit` to include `dailyStartTime` / `dailyEndTime` in the payload (null when empty).
  - Updated `onEditTask` to populate the new fields from the existing task DTO.

## Key decisions

- **TimeOnly stored as TimeSpan in EF Core** — EF Core 6+ maps `TimeOnly` to `TimeSpan` for PostgreSQL `TIME` columns. The conversion is explicit in the configuration to avoid surprises.
- **Shift cap raised to 336** — 7 days × 24 hours × 2 shifts/hour = 336 max. This covers the worst case (30-min shifts, 24/7, 7 days) without unbounded growth.
- **Daily window skip logic** — When a shift falls outside the daily window, the loop advances to the next day's window start rather than just incrementing by one shift. This avoids O(n) iteration through off-window time.
- **In-memory date filter in GetMyAssignmentsQuery** — The shift-GUID reverse lookup requires loading all group tasks first, so the date filter is applied after resolving metadata rather than in the DB query. For typical spaces (< 1000 assignments per version) this is fine.

## How it connects

- Solver payload: `SolverPayloadNormalizer` → `TriggerSolverCommand` → Python solver → `ScheduleVersion` (draft)
- Publish: `PublishVersionCommand` → sets version status to Published
- Member view: `GET /spaces/{id}/my-assignments` → `GetMyAssignmentsQuery` → resolves shift GUIDs → returns assignments

## How to run / verify

1. Run the migration: `psql -U jobuler -d jobuler -f infra/migrations/024_tasks_daily_time_window.sql`
2. Create a task with no daily window (24/7). Trigger the solver. Verify the draft shows 7 days of shifts.
3. Create a task with daily window 08:00–22:00. Trigger the solver. Verify no shifts appear outside that window.
4. Publish the draft. Log in as a member. Go to "My Missions" — assignments should now appear.

## What comes next

- Manual override assignments (drag-and-drop or inline assignment)
- Live person status panel (who's on mission right now)

## Git commit

```bash
git add -A && git commit -m "fix(scheduling): full 7-day horizon, publish visible to members, daily time window for tasks"
```
