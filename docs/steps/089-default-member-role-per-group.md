# Step 089 — Default Member Role Per Group

## Phase
Phase 9 — Member Management Enhancements

## Purpose
Every group needs a "floor" role that:
- Has no permissions (view-only)
- Is automatically assigned to any new member, regardless of who adds them
- Cannot be deleted (only renamed) — it's the fallback role the system always knows exists
- Appears first in the role dropdown so the owner can see it clearly

Previously, non-owner admins couldn't assign roles at all, and there was no guaranteed role to fall back to. This step introduces the concept of a **default group role** — a system-created "Member" role that is auto-created with every group and auto-assigned when a non-owner adds a member.

## What was built

### Domain
- `SpaceRole.cs` — Added `IsDefault` property. `CreateForGroup` now accepts an `isDefault` parameter. `Deactivate()` throws `InvalidOperationException` if `IsDefault = true`, preventing deletion.

### Infrastructure
- `SpaceConfiguration.cs` — Added `is_default` column mapping in `SpaceRoleConfiguration`.

### Migration
- `infra/migrations/030_space_roles_is_default.sql` — Adds `is_default BOOLEAN NOT NULL DEFAULT FALSE` to `space_roles`. Adds a partial unique index ensuring at most one default role per `(space_id, group_id)`.
- `infra/migrations/031_backfill_default_member_roles.sql` — For every existing group without a default role, inserts a "Member" role with `is_default = true`. Then assigns that role to every current group member who has no group-scoped role assignment. Both steps are idempotent via `ON CONFLICT DO NOTHING`.

### Application layer
- `CreateGroupCommand.cs` — After creating the group, auto-creates a `SpaceRole` with `isDefault: true`, name "Member", `PermissionLevel.View`, and no permissions. This role is always present in every group.
- `AddPersonToGroupByIdCommand.cs` — Non-owner admins no longer need to pass a `roleId`. The handler looks up the group's default role and assigns it automatically. Owner-provided `roleId` still takes precedence.
- `AddPersonByEmailCommand.cs` — Same automatic default role assignment for non-owners.
- `AddPersonByPhoneCommand.cs` — Same.
- `GroupRoleCommands.cs` — `GroupRoleDto` now includes `IsDefault`. `DeactivateGroupRoleCommand` is blocked by the domain's `Deactivate()` guard.
- `GetGroupRolesQuery.cs` — Filters to `IsActive` only, sorts default role first (`OrderByDescending(r => r.IsDefault)`), includes `IsDefault` in the DTO.

## Key decisions

- **Auto-create on group creation, not lazily**: The default role is guaranteed to exist from the moment the group is created. No "ensure default exists" logic needed in add-member paths.
- **Domain-level guard on deletion**: `Deactivate()` throws rather than silently ignoring the request. This surfaces as a 400 via `ExceptionHandlingMiddleware`.
- **Non-owner path never passes a roleId**: The add-member commands look up the default role themselves. The caller doesn't need to know the role ID — they just add the member and the system handles it.
- **One default per group enforced at DB level**: The partial unique index `WHERE is_default = TRUE` prevents accidental duplicates even if application logic has a bug.
- **Rename allowed**: `Update()` on the domain entity doesn't check `IsDefault`, so the owner can rename "Member" to anything they want (e.g. "Volunteer", "Soldier").

## How it connects
- `GetGroupRoles` returns `isDefault: true` on the default role — the frontend uses this to pin it at the top of the dropdown and disable the delete button for it.
- `GetGroupMembers` already returns `roleId` and `roleName` per member (added in step 088), so the frontend can show the assigned role immediately.

## How to run / verify

1. Apply migrations in order:
   ```bash
   psql $DATABASE_URL -f infra/migrations/030_space_roles_is_default.sql
   psql $DATABASE_URL -f infra/migrations/031_backfill_default_member_roles.sql
   ```

2. Restart the API.

3. Verify existing groups now have a default role:
   ```
   GET /spaces/{spaceId}/groups/{groupId}/roles
   → [ { "name": "Member", "isDefault": true, "permissionLevel": "View", ... }, ... ]
   ```

4. Verify existing members have the default role assigned:
   ```
   GET /spaces/{spaceId}/groups/{groupId}/members
   → each member now has "roleId" and "roleName": "Member"
   ```

5. Create a new group — verify a "Member" role is auto-created:
   ```
   POST /spaces/{spaceId}/groups  →  201
   GET  /spaces/{spaceId}/groups/{newGroupId}/roles
   → [ { "name": "Member", "isDefault": true, ... } ]
   ```

6. Add a member as a non-owner admin (no roleId in body):
   ```
   POST /spaces/{spaceId}/groups/{groupId}/members
   Body: { "personId": "..." }
   → 200, member is assigned the "Member" role automatically
   ```

7. Try to delete the default role:
   ```
   DELETE /spaces/{spaceId}/groups/{groupId}/roles/{defaultRoleId}
   → 400 "The default member role cannot be deleted. You may rename it."
   ```

8. Rename the default role:
   ```
   PUT /spaces/{spaceId}/groups/{groupId}/roles/{defaultRoleId}
   Body: { "name": "Volunteer", "permissionLevel": "view" }
   → 204
   ```

## What comes next
- Frontend: role dropdown in "Add Member" modal — show roles from `GET /roles`, pin the default one at top, disable delete button when `isDefault: true`
- Backfill: existing groups don't have a default role yet — migration 031 handles this by inserting a "Member" role for every existing group and assigning it to all current members who have no group-scoped role.

## Git commit
```bash
git add -A && git commit -m "feat(groups): default member role per group, auto-assigned on add"
```
