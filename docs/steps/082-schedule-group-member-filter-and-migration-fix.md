# Step 082 — Schedule Group Member Filter and Migration Fix

## Phase
Phase 8 — Bug fixes following step 081

## Purpose
Two bugs surfaced after deploying step 081:
1. The schedule tab on the group detail page was showing assignments for people from other groups (the schedule is space-level, not group-level).
2. The `GET /spaces/{spaceId}/groups/{groupId}/roles` endpoint returned 500 because migration 027 had not been applied to the local database.

## What was built

### Migration fix
- Applied `infra/migrations/027_group_scoped_roles.sql` to the local PostgreSQL database
- Added `group_id` column to `space_roles` and `person_role_assignments`
- Registered migration in `schema_migrations` tracking table

### Frontend — schedule group member filter
- `apps/web/app/groups/[groupId]/tabs/ScheduleTab.tsx`
  - Added `memberNames?: Set<string>` prop
  - `filtered` now excludes assignments whose `personName` is not in `memberNames` (when provided)
  - This ensures the group schedule tab only shows assignments for members of that specific group
- `apps/web/app/groups/[groupId]/page.tsx`
  - Members are now loaded eagerly when either the `schedule` or `members` tab is active (previously only on `members` tab)
  - Passes `memberNames={new Set(members.map(m => m.displayName ?? m.fullName))}` to `ScheduleTab`

## Key decisions
- Filter is applied on the frontend rather than adding a `groupId` filter to the backend schedule query — the schedule is intentionally space-level (the solver schedules across the whole space), so filtering at display time is the right layer
- Members are loaded on schedule tab activation to ensure the filter set is available immediately

## How it connects
- `ScheduleTab` now correctly scopes the displayed schedule to the group's own members
- The `memberNames` set is built from `displayName ?? fullName` to match how `personName` is resolved in the backend's `AssignmentDto`

## How to run / verify
1. Open a group detail page → Schedule tab
2. Verify only members of that group appear in the schedule table
3. Open Settings tab → verify roles load without 500 error

## Git commit
```bash
git add -A && git commit -m "fix(schedule): filter schedule to group members, apply migration 027"
```
