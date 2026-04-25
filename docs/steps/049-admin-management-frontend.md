# Step 049 — Admin Management Frontend (Tasks 21–26)

## Phase
Phase 8 — Admin Management and Scheduling

## Purpose
Complete the frontend implementation for the admin-management-and-scheduling spec. The backend was already implemented (GroupTask domain entity, EF config, migration, application commands/queries, API controllers, and frontend API clients). This step adds the UI layer in `GroupDetailPage` for all six admin-facing features.

## What was built

### Files modified

**`apps/web/app/groups/[groupId]/page.tsx`**
- Added `React` import (needed for `React.Fragment` in constraints table)
- **Task 21 — משימות tab**: Already fully implemented in prior work. Unified group task list using `listGroupTasks`, burden level badges, admin create/edit/delete form.
- **Task 22 — אילוצים tab**: Added edit and delete buttons to each constraint row (admin only). Edit opens an inline row form pre-populated with `rulePayloadJson`, `effectiveFrom`, `effectiveUntil`. Delete shows Hebrew confirm dialog and calls `deleteConstraint`. Added 5th column header for admin actions. Fixed `taskTypes` reference → `groupTasks`.
- **Task 23 — התראות tab**: Added edit button alongside existing delete button (admin only, on ALL alerts). Edit opens an inline form within the alert card pre-populated with `title`, `body`, `severity`. Calls `updateGroupAlert` on submit.
- **Task 24 — הודעות tab**: Added admin action buttons (pin/unpin, edit, delete) on ALL messages. Edit opens inline textarea. Delete shows Hebrew confirm dialog. Pin/unpin toggles `isPinned` state. Pinned messages get `bg-amber-50 border-amber-200 shadow-sm` styling + 📌 badge.
- **Task 25 — הגדרות tab**: Added "הפעל סידור" section (admin only) between solver horizon and deleted groups. Shows spinner + "הסידור מחושב..." while polling, success message on Completed, error messages on Failed/TimedOut/404.
- **Task 26 — סידור tab**: Added draft version banner at top of schedule panel. Shows "טיוטה" badge, "פרסם סידור" and "בטל טיוטה" buttons (admin only). Publish calls `POST /schedule-versions/{id}/publish`. Discard shows Hebrew confirm then calls `DELETE /schedule-versions/{id}`.

**`infra/migrations/014_group_tasks.sql`** (already existed, applied in this step)
- Migration applied successfully: `CREATE TABLE tasks`, indexes, trigger, RLS policy.

## Key decisions

- Used `React.Fragment` for the constraints table to allow inline edit rows without breaking table structure.
- Inline edit forms (within the card/row) rather than modal dialogs — consistent with existing pattern in the file.
- Solver trigger section placed between "solver horizon" and "deleted groups" in settings — logical grouping.
- Draft version banner placed at the very top of the schedule panel so it's immediately visible.
- `taskTypes` reference in constraint form replaced with `groupTasks` since the old task-types model is superseded by the new flat group tasks model.

## How it connects

- Calls `updateConstraint` / `deleteConstraint` from `apps/web/lib/api/constraints.ts`
- Calls `updateGroupAlert` from `apps/web/lib/api/groups.ts`
- Calls `updateGroupMessage` / `deleteGroupMessage` / `pinGroupMessage` from `apps/web/lib/api/groups.ts`
- Solver trigger calls `POST /spaces/{spaceId}/schedule-runs/trigger` and polls `GET /spaces/{spaceId}/schedule-runs/{runId}`
- Draft publish calls `POST /spaces/{spaceId}/schedule-versions/{versionId}/publish`
- Draft discard calls `DELETE /spaces/{spaceId}/schedule-versions/{versionId}`

## How to run / verify

1. Start the API: `dotnet run --project apps/api/Jobuler.Api`
2. Start the frontend: `npm run dev` in `apps/web`
3. Navigate to a group, enter admin mode
4. Test each tab: משימות (create/edit/delete tasks), אילוצים (edit/delete constraints), התראות (edit/delete any alert), הודעות (edit/delete/pin any message), הגדרות (trigger solver), סידור (publish/discard draft)

## What comes next

- Unit tests for domain entities (Task 27)
- Unit tests for application handlers (Task 28)
- Property-based tests (Tasks 29–32)
- Integration tests (Task 33)

## Git commit

```bash
git add -A && git commit -m "feat(frontend): admin management UI — tasks 21-26 (constraints edit/delete, alerts edit, messages edit/pin/delete, solver trigger, draft publish/discard)"
```
