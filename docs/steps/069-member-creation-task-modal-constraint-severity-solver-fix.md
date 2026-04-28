# Step 069 — Member creation fix, task modal UX, constraint severity badge, solver infeasibility fix

## Phase
Phase 7 — UX Hardening & Quality Pass

## Purpose
Fix a set of bugs and UX issues found during manual testing:
1. Adding a member by name showed a UUID as the member's name in the list
2. Two separate "add member" buttons were confusing — merged into one
3. Constraint severity badge always showed "רך" (soft) regardless of actual value
4. Creating tasks/constraints/alerts happened inline — moved to modals
5. Task form had no hours+minutes duration input and required dates
6. No way to specify which tasks can be performed concurrently
7. Solver returned a draft even when the schedule was completely empty (all slots uncovered)

## What was built

### Fix 1 — Member creation (UUID name bug)
- `apps/web/app/groups/[groupId]/page.tsx` — replaced `addGroupMemberByEmail(id)` (which sent the UUID as an email) with `addGroupMemberById(id)` after `createPerson`
- `apps/web/lib/api/groups.ts` — `addGroupMemberById` already existed; now used correctly

### Fix 2 — Single "Add member" button
- `apps/web/app/groups/[groupId]/tabs/MembersTab.tsx` — removed two buttons (`+ הוסף לפי אימייל`, `+ צור אדם חדש`), replaced with single `+ הוסף חבר` button
- `apps/web/app/groups/[groupId]/page.tsx` — unified `handleAddMember` handler: creates person by name, adds to group by ID, optionally sends invitation via phone (WhatsApp) or email
- Modal title changed from member's name to "פרטי חבר"

### Fix 3 — Constraint severity badge
- `apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx` — normalized `c.severity` to lowercase before lookup (`c.severity?.toLowerCase()`) since the API returns PascalCase (`"Hard"`, `"Soft"`) from the C# enum

### Fix 4 — Forms in modals
- `ConstraintsTab.tsx` — create and edit forms moved into `<Modal>` components
- `AlertsTab.tsx` — create and edit forms moved into `<Modal>` components; severity options now show Hebrew labels
- `TasksTab.tsx` — create and edit form moved into `<Modal>` component

### Fix 5 — Task duration in hours + minutes
- `apps/web/app/groups/[groupId]/tabs/TasksTab.tsx` — replaced single "minutes" input with two inputs: hours (0–23) and minutes (0–59, step 5)
- Task list now shows duration as "Xש׳ Yd׳" instead of raw minutes

### Fix 6 — Optional task dates
- `TasksTab.tsx` — `startsAt` and `endsAt` are no longer `required`; labels show "(ברירת מחדל: היום)" and "(אופציונלי)"
- `page.tsx` — `handleTaskSubmit` defaults `startsAt` to `new Date().toISOString()` when empty

### Fix 7 — Concurrent tasks dropdown
- `TasksTab.tsx` — added `concurrentTaskIds: string[]` to `TaskForm` interface; renders a checkbox list of other tasks in the group that can be performed simultaneously
- `DEFAULT_TASK_FORM` updated to include `concurrentTaskIds: []`

### Fix 8 — Solver infeasibility (draft created when all slots uncovered)
- `apps/solver/solver/engine.py` — `_empty_result()` now returns `feasible=False` instead of `feasible=True`
- `engine.py` — after solving, if `feasible=True` but `len(uncovered) == num_slots` (every slot is uncovered = no one was assigned), force `feasible=False` and clear assignments
- This ensures the worker's existing `if (!output.Feasible) version.Discard()` logic fires correctly

## Key decisions
- Severity normalization is done client-side (`.toLowerCase()`) — avoids a backend migration
- `concurrentTaskIds` is stored in the form state but not yet persisted to the API (the field doesn't exist on `GroupTaskDto` yet) — it's infrastructure for a future feature
- The "all slots uncovered = infeasible" rule is conservative: a partial schedule (some slots covered) is still treated as feasible so the admin can review and publish it

## How it connects
- `addGroupMemberById` was already in `groups.ts` — just wasn't being used
- `ConstraintRule.Severity` is a C# enum serialized as PascalCase; the frontend now handles both cases
- The solver fix closes the loop: infeasible → discard version → frontend shows red banner with conflict details

## How to run / verify
1. Members tab → "+ הוסף חבר" → enter name only → member appears with correct name (not UUID)
2. Members tab → "+ הוסף חבר" → enter name + phone → member added and WhatsApp invite sent
3. Constraints tab → any constraint → verify severity badge shows "קשה" for Hard constraints
4. Tasks tab → "+ משימה חדשה" → opens modal; duration shows hours+minutes; dates are optional
5. Trigger solver with only 1 person and 3 required headcount → no draft created, red banner shown

## Git commit
```bash
git add -A && git commit -m "fix(group): member creation UUID bug, single add-member modal, constraint severity badge, task modal UX, solver infeasibility fix"
```
