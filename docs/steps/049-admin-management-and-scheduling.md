# Step 049 — Admin Management and Scheduling (v2.0)

## Phase
Phase 9 — Admin Management and Scheduling

## Purpose

Extends Jobuler from LTS v1.0 with full admin CRUD capabilities and a scheduling activation UI. Admins can now manage the complete lifecycle of tasks, constraints, alerts, and messages, and trigger the solver to generate draft schedules for review.

## What was built

### Domain Layer
- **`GroupTask.cs`** — New flat task entity replacing the two-level TaskType+TaskSlot model. Fields: name, starts_at, ends_at, duration_hours, required_headcount, burden_level, allows_double_shift (same person twice in a row), allows_overlap (person can be on two tasks simultaneously), is_active.
- **`GroupAlert.cs`** — Added `Update()` method for admin editing.
- **`ScheduleVersion.cs`** — Added `Discarded` status and `Discard()` method with draft-only guard.

### Infrastructure
- **`TasksConfiguration.cs`** — Added `GroupTaskConfiguration` EF Fluent API mapping for the `tasks` table.
- **`AppDbContext.cs`** — Added `GroupTasks` DbSet.

### Database
- **`infra/migrations/014_group_tasks.sql`** — Creates `tasks` table with CHECK constraints (ends_at > starts_at, duration_hours > 0, required_headcount >= 1, burden_level IN valid set), UNIQUE index on (space_id, group_id, name), updated_at trigger. Legacy task_types and task_slots tables untouched.

### Application Layer (new commands/queries)
- **`GroupTaskCommands.cs`** — CreateGroupTaskCommand, UpdateGroupTaskCommand, DeleteGroupTaskCommand with FluentValidation
- **`GetGroupTasksQuery.cs`** — Returns active tasks ordered by starts_at ascending
- **`UpdateConstraintCommand.cs`** — Updates constraint payload and effective dates
- **`DeleteConstraintCommand.cs`** — Soft-deletes constraint (is_active = false)
- **`GroupAlertCommands.cs`** — Removed ownership check from delete (any people.manage holder can delete any alert); added UpdateGroupAlertCommand
- **`GroupMessageCommands.cs`** — Rewrote with people.manage bypass for delete; added UpdateGroupMessageCommand and PinGroupMessageCommand
- **`DiscardVersionCommand.cs`** — Sets draft schedule version to Discarded status

### API Layer (new/modified endpoints)
- `GET/POST/PUT/DELETE /spaces/{spaceId}/groups/{groupId}/tasks` — Group-scoped task CRUD
- `PUT/DELETE /spaces/{spaceId}/constraints/{constraintId}` — Constraint edit/delete
- `PUT /spaces/{spaceId}/groups/{groupId}/alerts/{alertId}` — Alert edit
- `PUT /spaces/{spaceId}/groups/{groupId}/messages/{messageId}` — Message edit
- `PATCH /spaces/{spaceId}/groups/{groupId}/messages/{messageId}/pin` — Pin/unpin message
- `DELETE /spaces/{spaceId}/schedule-versions/{versionId}` — Discard draft version

### Frontend
- **`apps/web/lib/api/tasks.ts`** — Added group-scoped task CRUD functions
- **`apps/web/lib/api/constraints.ts`** — Added updateConstraint, deleteConstraint
- **`apps/web/lib/api/groups.ts`** — Added updateGroupAlert, updateGroupMessage, deleteGroupMessage, pinGroupMessage
- **`apps/web/app/groups/[groupId]/page.tsx`** — Full rewrite with:
  - **משימות tab**: Unified task list with create/edit/delete (replaces old task-types + task-slots)
  - **אילוצים tab**: Edit + delete buttons with inline edit form
  - **התראות tab**: Edit + delete on ALL alerts (not just own)
  - **הודעות tab**: Edit, delete, pin/unpin on ALL messages; pinned messages highlighted in amber
  - **הגדרות tab**: "הפעל סידור" section with solver trigger, 3s polling, status messages
  - **סידור tab**: Draft version banner with "טיוטה" badge, publish and discard buttons

### Tests (286 total, all passing)
- **`GroupTaskTests.cs`** — Domain entity unit tests (Create, Deactivate, Discard, Update)
- **`AdminManagementHandlerTests.cs`** — Application handler unit tests (28.1–28.5)
- **`GroupTaskPropertyTests.cs`** — Properties 1–5 (task CRUD round-trips, ordering, validation)
- **`ConstraintPropertyTests.cs`** — Properties 6–9 (constraint update/delete round-trips, validation)
- **`AlertMessageAdminPropertyTests.cs`** — Properties 10–14 (cross-user delete, pin/unpin, update round-trips)
- **`ScheduleVersionDiscardPropertyTests.cs`** — Property 15 (discard sets Discarded status)
- **`AdminManagementIntegrationTests.cs`** — Integration tests 33.1–33.5

## Key decisions

- **Flat task model**: Replaced TaskType+TaskSlot with a single `GroupTask` entity scoped to a group. Simpler, more flexible, no need for a "type" abstraction.
- **allows_double_shift vs allows_overlap**: Distinct concepts — double shift = same person doing the same task twice in a row; overlap = person can be assigned to another task at the same time (e.g. "כוננות חירום" + "סיור").
- **Admin delete any alert/message**: Removed ownership check — any `people.manage` holder can moderate any content.
- **Inline edit forms**: Consistent with existing page pattern; no modal dialogs needed.
- **Solver polling**: 3-second interval, stops on Completed/Failed/TimedOut/404.

## How to connect

- Backend compiles clean (0 CS errors; file-lock warnings only when API is running)
- Migration 014 applied to local PostgreSQL
- Frontend TypeScript compiles clean

## How to run / verify

```bash
# Apply migration (if not already done)
$env:PGPASSWORD = "Akame157157"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d jobuler -f "C:\Users\User\Desktop\Jobduler\infra\migrations\014_group_tasks.sql"

# Run tests
dotnet test apps/api/Jobuler.Tests/Jobuler.Tests.csproj -v minimal

# Restart API (required after code changes)
dotnet run --project apps/api/Jobuler.Api/Jobuler.Api.csproj

# Frontend
cd apps/web && npm run dev
```

## What comes next

- End-to-end testing of the solver trigger flow (requires solver service running)
- Notifications for published schedules
- Member availability windows UI

## Git commit

```bash
git add -A && git commit -m "feat(v2.0): admin management and scheduling — tasks, constraints, alerts, messages CRUD + solver trigger UI"
```
