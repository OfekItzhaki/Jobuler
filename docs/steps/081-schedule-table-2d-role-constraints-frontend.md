# Step 081 — Schedule Table 2D, Role Constraints Frontend, and Requirements Updates

## Phase
Phase 8 — Schedule Table, Auto-Scheduler Gap Detection, and Role Constraints (Tasks 10–18)

## Purpose
Continues the schedule-table-autoschedule-role-constraints spec from task 10 onward. Delivers the 2D schedule table component, updates the group and admin schedule views, adds the group roles API client, restructures the constraints tab into three scoped sections, and adds the roles management section to settings. Also updates requirements to reflect user feedback: full-day gap coverage, schedule.publish = recalculate permission, and two new features (manual override assignments, live person status panel).

## What was built

### Requirements updates
- `.kiro/specs/schedule-table-autoschedule-role-constraints/requirements.md`
  - Req 4: Clarified that the scheduler must cover every task slot throughout the day (no intra-day holes), for all days in the group-owner-configured horizon
  - Req 11: Renamed to "Solver Failure Notifications and Re-trigger Permission" — added AC 6: `schedule.publish` permission also grants solver re-trigger (no separate `schedule.recalculate` needed)
  - Req 14 (new): Manual Override Assignments — click a cell to override an assignment, creates a draft, locks overridden slots from solver reassignment, audit logged
  - Req 15 (new): Live Person Status Panel — real-time panel showing who is on mission/at home/free, derived from presence windows + published assignments, 30s polling

### Tasks updates
- `.kiro/specs/schedule-table-autoschedule-role-constraints/tasks.md`
  - Task 10 marked complete (already implemented in prior session)
  - Tasks 27–34 added for manual override and live status panel features

### Solver (task 10 — already done, verified)
- `apps/solver/solver/constraints.py`: `expand_role_constraints` and `expand_group_constraints` already implemented
- `apps/solver/solver/engine.py`: Both expansion functions called at top of `solve()` before CP-SAT model is built
- All 41 solver tests pass

### Frontend — ScheduleTable2D (task 12)
- `apps/web/components/schedule/ScheduleTable2D.tsx` (new)
  - Accepts `assignments`, `currentUserName`, `filterDate`, `onCellClick`
  - Derives unique task names (columns, sorted alphabetically) and time slots (rows, sorted by start time)
  - Builds `slotKey → taskName → [personName]` cell map
  - Highlights current user's task column in blue
  - Empty cells render `—` in muted colour
  - Hebrew empty-state when no assignments match the filter
  - Horizontally scrollable, sticky row-header column
  - `onCellClick` prop enables future manual override integration (task 30)
  - Uses `TableAssignment` type (subset of both `ScheduleAssignment` and `AssignmentDto`) for compatibility

### Frontend — ScheduleTab (task 13)
- `apps/web/app/groups/[groupId]/tabs/ScheduleTab.tsx`
  - Day view: replaced list table with `<ScheduleTable2D filterDate={scheduleDate} />`
  - Week view: replaced per-day card list with 7 day-name tab buttons (Sun–Sat); selected day renders `<ScheduleTable2D>`; today's tab highlighted in blue
  - Removed month/year view options (only day and week remain)
  - Added `currentUserName?: string` prop, passed down to `ScheduleTable2D`
- `apps/web/app/groups/[groupId]/page.tsx`
  - Destructures `displayName` from `useAuthStore`
  - Passes `currentUserName={displayName ?? undefined}` to `ScheduleTab`

### Frontend — Admin schedule page (task 14)
- `apps/web/app/admin/schedule/page.tsx`
  - Replaced `<ScheduleTable>` with `<ScheduleTable2D filterDate={selectedDate} />`
  - Added `selectedDate` state (defaults to today)
  - Added prev/next day navigation buttons + "היום" button above the table
  - All existing functionality preserved: version sidebar, publish/rollback, diff card, CSV/PDF export, infeasibility banner, solver trigger buttons

### Frontend — Group roles API client (task 15)
- `apps/web/lib/api/groups.ts`
  - Added `GroupRoleDto` interface: `{ id, name, description, isActive }`
  - Added `getGroupRoles(spaceId, groupId)`, `createGroupRole(...)`, `updateGroupRole(...)`, `deactivateGroupRole(...)`
  - Routes: `GET/POST/PUT/DELETE /spaces/{spaceId}/groups/{groupId}/roles`

### Frontend — SettingsTab roles section (task 16)
- `apps/web/app/groups/[groupId]/tabs/SettingsTab.tsx`
  - Added "תפקידים" section with inline role list and add-role form
  - Active roles show rename and deactivate buttons; deactivated roles shown with strikethrough
  - New props: `groupRoles`, `groupRolesLoading`, `onCreateRole`, `onUpdateRole`, `onDeactivateRole`
- `apps/web/app/groups/[groupId]/page.tsx`
  - Added `groupRoles` and `groupRolesLoading` state
  - Added `handleCreateRole`, `handleUpdateRole`, `handleDeactivateRole` handlers
  - Loads group roles when settings tab opens (alongside deleted groups)
  - Wired all new props to `SettingsTab`

### Frontend — ConstraintsTab three-section restructure (task 17)
- `apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx`
  - Restructured into three collapsible sections: אילוצי קבוצה / אילוצי תפקיד / אילוצים אישיים
  - Each section has its own inline create form (`SectionCreateForm`) with scope-appropriate selector
  - Role constraint form: dropdown of active group roles only
  - Personal constraint form: dropdown of registered members only (`invitationStatus === "accepted"`)
  - `ConstraintRow` component shows role name / person name badge on each row
  - New props: `groupId`, `groupRoles`, `groupRolesLoading`, `members`, `onCreateWithScope`
  - Legacy modal create path preserved for backward compatibility
- `apps/web/app/groups/[groupId]/page.tsx`
  - Passes `groupId`, `groupRoles`, `groupRolesLoading`, `members` to `ConstraintsTab`
  - Adds `onCreateWithScope` handler that calls `createConstraint` with the correct `scopeType`/`scopeId`

## Key decisions
- `ScheduleTable2D` uses a `TableAssignment` type (structural subset) so it works with both `ScheduleAssignment` (group page) and `AssignmentDto` (admin page) without casting
- Week view `selectedWeekDay` state is lifted to the top level of `ScheduleTab` to avoid React hooks-in-IIFE violations
- `onCreateWithScope` is optional on `ConstraintsTab` — when not provided, the legacy modal is used, keeping backward compatibility
- Group roles are loaded lazily (only when settings tab opens) to avoid unnecessary API calls

## How it connects
- `ScheduleTable2D` is the single source of truth for schedule rendering across the app
- `onCellClick` prop on `ScheduleTable2D` is the hook point for task 30 (manual override modal)
- `GroupRoleDto` from `groups.ts` is shared between `SettingsTab` (management) and `ConstraintsTab` (role selector)
- `onCreateWithScope` in `ConstraintsTab` feeds into the existing `createConstraint` API call in `page.tsx`

## How to run / verify
```bash
# TypeScript check
cd apps/web && npx tsc --noEmit

# Solver tests
cd apps/solver && python -m pytest tests/ -v

# Frontend unit tests (compiled)
cd apps/web && npx --yes vitest run --reporter=verbose 2>&1 | grep -E "PASS|FAIL|passed|failed"
```

## What comes next
- Task 19–25: Optional property-based and unit tests for backend and frontend
- Task 26: Final checkpoint
- Task 27–30: Manual override assignments (backend domain, solver lock, API endpoint, frontend modal)
- Task 31–33: Live person status panel (backend query, API endpoint, frontend panel)
- Task 34: Final checkpoint after new features

## Git commit
```bash
git add -A && git commit -m "feat(schedule): 2D table, role constraints UI, settings roles section"
```
