# Step 047 — Group Detail Page: Constraint Form & Tasks Panel Fixes

## Phase
Phase 5 — UX Hardening

## Purpose
Fix two sets of issues in the group detail page (`apps/web/app/groups/[groupId]/page.tsx`):

1. **Constraint form** — the payload JSON field had no validation, the scope dropdown showed raw English enum values, and the payload input inherited RTL direction making JSON look malformed.
2. **Tasks panel** — the sub-tab UI (סוגי משימות / חלונות זמן) added unnecessary navigation friction. The new design merges both views into a single scrollable panel with section headers.

## What was built

### `apps/web/app/groups/[groupId]/page.tsx`
- **`handleCreateConstraint`**: Added `JSON.parse` validation before the API call. If the payload is not valid JSON, sets `constraintError` with a Hebrew message and returns early — no API call is made.
- **Constraint scope dropdown**: Replaced raw English enum values (`space`, `group`, etc.) with Hebrew labels: `מרחב (Space)`, `קבוצה (Group)`, `אדם (Person)`, `תפקיד (Role)`, `סוג משימה (TaskType)`.
- **Constraint payload input**: Added `dir="ltr"` so the monospace JSON text renders left-to-right even inside an RTL page.
- **`tasksSubTab` state removed**: Deleted the `useState<"types" | "slots">` state variable and all references to it.
- **`renderTasksPanel()` redesigned**: Removed sub-tab buttons. Now shows:
  - Two inline-toggle buttons side by side (admin only): `+ סוג משימה` and `+ משימה חדשה`
  - Task type create form (toggleable, no sub-tab guard)
  - Task slot create form (toggleable, no sub-tab guard)
  - Section header `סוגי משימות` + table (name, burden, overlap)
  - Section header `משימות מתוזמנות` + table (task type, start, end, headcount, status)
  - Both tables always visible — no conditional rendering on sub-tab

## Key decisions
- Kept all existing form state variables (`showTaskTypeForm`, `showSlotForm`, etc.) — only the sub-tab switching logic was removed.
- The task type create form no longer shows the priority field (it was a secondary detail not needed in the simplified view; the API default is used).
- JSON validation uses a simple `try { JSON.parse(...) } catch` — no external library needed.

## How it connects
- The constraint form feeds `POST /spaces/{spaceId}/constraints` via `createConstraint()` in `@/lib/api/constraints`.
- The tasks panel feeds `POST /spaces/{spaceId}/task-types` and `POST /spaces/{spaceId}/task-slots` via `@/lib/api/tasks`.
- No backend changes required — all fixes are purely frontend.

## How to run / verify
1. Start the web dev server: `cd apps/web && npm run dev`
2. Log in as an admin, navigate to a group detail page, enter admin mode.
3. **Constraint form**: Open the constraints tab → click `+ אילוץ` → enter invalid JSON in the payload field → click שמור → verify the error message `Payload חייב להיות JSON תקין` appears and no API call is made.
4. **Scope dropdown**: Verify the dropdown shows Hebrew labels (מרחב, קבוצה, etc.).
5. **Payload direction**: Verify the JSON input renders left-to-right.
6. **Tasks panel**: Open the tasks tab → verify both `+ סוג משימה` and `+ משימה חדשה` buttons appear side by side → verify both tables (סוגי משימות, משימות מתוזמנות) are visible without any sub-tab switching.

## What comes next
- Add inline editing / deletion for task types and constraints.
- Add pagination or filtering to the tasks panel when slot counts grow large.

## Git commit
```bash
git add -A && git commit -m "fix(group-detail): constraint JSON validation, RTL payload fix, tasks panel sub-tab removal"
```
