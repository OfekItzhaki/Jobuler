# Step 068 — Constraint severity editable, admin mode scoped, solver status display, per-group stats tab

## Phase
Phase 7 — UX Hardening & Quality Pass

## Purpose
Fix a set of UX and correctness issues discovered during review:
1. Constraint edit form was missing the severity selector
2. Solver status showed raw "Completed" text with no context
3. Admin mode persisted globally across navigation (leaked between groups)
4. Stats were only accessible via a global nav item; now scoped per-group as a tab

## What was built

### Fix 1 — Constraint severity in edit form
- `apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx` — added `editConstraintSeverity` prop and severity `<select>` before the payload editor in the edit form
- `apps/web/app/groups/[groupId]/page.tsx` — added `editConstraintSeverity` state, set it in `onStartEdit`, pass to ConstraintsTab, include in `handleUpdateConstraint`
- `apps/web/lib/api/constraints.ts` — added optional `severity` field to `updateConstraint` payload type
- `apps/api/Jobuler.Domain/Constraints/ConstraintRule.cs` — updated `Update()` to accept `ConstraintSeverity?`
- `apps/api/Jobuler.Application/Constraints/Commands/UpdateConstraintCommand.cs` — added `string? Severity` to command record and handler
- `apps/api/Jobuler.Api/Controllers/ConstraintsController.cs` — added `Severity` to `UpdateConstraintRequest` and passes it to command

### Fix 2 — Solver status display
- `apps/web/app/groups/[groupId]/tabs/SettingsTab.tsx` — replaced raw status text with contextual Hebrew messages (✓ completed / ✗ failed / generic)
- `apps/web/app/groups/[groupId]/page.tsx` — polling now also stops on "TimedOut" status

### Fix 3 — Admin mode scoped to group
- `apps/web/app/groups/[groupId]/page.tsx` — cleanup effect now calls `exitAdminMode()` on unmount
- `apps/web/components/shell/AppShell.tsx` — removed global admin mode indicator from topbar; removed stats nav item from sidebar (stats is now a per-group tab)

### Fix 4 — Per-group stats tab
- `apps/web/app/groups/[groupId]/types.ts` — `stats` already in `ActiveTab` and `ADMIN_ONLY_TABS`
- `apps/web/app/groups/[groupId]/page.tsx` — added `stats` to `TAB_LABELS` and `ALL_TABS`; imports and renders `StatsTab`
- `apps/web/app/groups/[groupId]/tabs/StatsTab.tsx` — new component; fetches space-level burden stats, filters to group members, shows summary cards + leaderboards + people table using shared `StatsLeaderboard` and `StatsPeopleTable` components

## Key decisions
- Severity update is optional (`string?`) so existing callers without severity still work
- Stats filtering is client-side (filter by `memberIds` set) — avoids a new API endpoint
- Admin mode cleanup on unmount prevents stale admin state when navigating between groups

## How it connects
- `ConstraintRule.Update()` now accepts an optional severity — backward compatible
- `StatsTab` reuses the same leaderboard/table components as the global stats page
- AppShell is now stateless w.r.t. admin mode — the group detail page owns that state

## How to run / verify
1. Open a group in admin mode → Constraints tab → edit a constraint → verify severity dropdown appears
2. Trigger solver → verify status shows Hebrew contextual message
3. Enter admin mode on group A, navigate to group B → admin mode should be off
4. In admin mode, open a group → Stats tab should appear and show group-scoped data

## Git commit
```bash
git add -A && git commit -m "fix(group): constraint severity editable, admin mode scoped to group, solver status display, per-group stats tab"
```
