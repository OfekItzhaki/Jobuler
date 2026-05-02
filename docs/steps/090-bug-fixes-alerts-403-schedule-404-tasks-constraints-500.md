# Step 090 — Bug Fixes: Alerts 403, Schedule 404, Tasks/Constraints 500

## Phase
Phase 9 — Bug Fixes

## Purpose
Four issues reported from the running app:

1. `GET /alerts → 403` — Space admins who aren't group members couldn't view group alerts
2. `GET /schedule-versions/current → 404` — Console noise (not a real crash; already handled)
3. `POST /tasks → 500` — Missing migrations not yet applied
4. `POST /constraints → 500` — Missing migration 029 (constraint enum columns still native PG types)

## Root Causes

### Alerts 403
`GetGroupAlertsQuery` checked only group membership. A space admin (with `space.admin_mode` permission) who isn't a member of a specific group was blocked from viewing that group's alerts. The fix adds a fallback: if the user isn't a member, check if they have `space.admin_mode` permission.

### Schedule 404
The API correctly returns 404 when no schedule has been published. The frontend already handles this with `.catch(() => null)` — `scheduleData` is set to `[]` and the schedule tab shows an empty state. The 404 in the browser console is just network noise, not a user-facing error. No code change needed.

### Tasks/Constraints 500
Both are caused by pending migrations not yet applied to the local DB:
- Migration 029: converts `constraint_rules.scope_type` and `severity` from native PG enum types to TEXT
- Migration 030: adds `is_default` to `space_roles`
- Migration 031: backfills default member roles for existing groups

## What was built

### Backend
- `GroupAlertCommands.cs` — `GetGroupAlertsQueryHandler` now allows access if the user is a group member **OR** has `space.admin_mode` permission. The `SpacePermissionGrants` table is queried as a fallback.

## How to run / verify

1. Apply all pending migrations:
   ```bash
   psql $DATABASE_URL -f infra/migrations/029_constraint_enums_to_text.sql
   psql $DATABASE_URL -f infra/migrations/030_space_roles_is_default.sql
   psql $DATABASE_URL -f infra/migrations/031_backfill_default_member_roles.sql
   ```

2. Restart the API.

3. Verify alerts work for space admin:
   ```
   GET /spaces/{spaceId}/groups/{groupId}/alerts  (as admin user not in the group)
   → 200 []  (was 403)
   ```

4. Verify constraints work:
   ```
   GET  /spaces/{spaceId}/constraints  → 200  (was 500)
   POST /spaces/{spaceId}/constraints  → 201  (was 500)
   ```

5. Verify tasks work:
   ```
   POST /spaces/{spaceId}/groups/{groupId}/tasks  → 201  (was 500)
   ```

6. Verify schedule empty state:
   ```
   GET /spaces/{spaceId}/schedule-versions/current  → 404 (correct, no schedule published)
   Frontend shows empty schedule table, no error message shown to user
   ```

## What comes next
- Apply the migrations to any staging/production environment
- The "error message in English" on constraints will disappear once migration 029 is applied — the 500 fallback message won't trigger anymore

## Git commit
```bash
git add -A && git commit -m "fix(alerts): allow space admins to view group alerts without membership"
```
