# Step 088 — Constraint Enum Fix & Member Role Assignment at Add Time

## Phase
Phase 9 — Bug Fixes & Member Management Enhancements

## Purpose
Two separate issues addressed in this step:

1. **Bug fix**: `GET /constraints` and `POST /constraints` were returning 500 errors because `constraint_rules.scope_type` and `constraint_rules.severity` were still stored as native PostgreSQL enum types. EF Core's `ValueConverter` (which serializes to/from lowercase strings) is incompatible with native PG enums — Npgsql maps them differently at runtime. All other enum columns in the schema were converted to TEXT in migrations 019/021, but these two were missed.

2. **Feature**: When adding a member to a group, the group owner can now optionally assign a group role at add time. Non-owner admins can only add members with no role; the group owner can assign or change the role later via a new PATCH endpoint.

## What was built

### Migration
- `infra/migrations/029_constraint_enums_to_text.sql` — Converts `constraint_rules.scope_type` and `constraint_rules.severity` from native PG enum types to TEXT columns with CHECK constraints. Idempotent (wrapped in DO blocks). Drops the now-unused `constraint_scope_type` and `constraint_severity` PG enum types.

### Application layer
- `AddPersonToGroupByIdCommand.cs` — Added optional `RoleId` parameter. Validates that only the group owner can assign a role at add time. Validates the role belongs to the target group. Creates a `PersonRoleAssignment` if a role is provided.
- `AddPersonByEmailCommand.cs` — Same role assignment logic added.
- `AddPersonByPhoneCommand.cs` — Same role assignment logic added.
- `GroupRoleCommands.cs` — Added `UpdateMemberRoleCommand` / `UpdateMemberRoleCommandHandler`. Group owner only. Replaces all existing group-scoped role assignments for the member with the new one. Pass `RoleId = null` to remove the role.
- `GetGroupsQuery.cs` — `GroupMemberDto` now includes `RoleId` and `RoleName`. `GetGroupMembersQueryHandler` loads group-scoped `PersonRoleAssignments` and joins role names in a single query pass.

### API layer
- `GroupsController.cs` — Updated `AddMemberById`, `AddMemberByEmail`, `AddMemberByPhone` to accept optional `roleId` in the request body. Added `PATCH /spaces/{spaceId}/groups/{groupId}/members/{personId}/role` endpoint backed by `UpdateMemberRoleCommand`. Added `UpdateMemberRoleRequest` record.

## Key decisions

- **Role assignment is owner-only at add time**: Non-owner admins (who have `PeopleManage` permission) can add members but cannot assign roles. This matches the stated requirement and avoids privilege escalation.
- **One role per member per group**: `UpdateMemberRoleCommand` removes all existing group-scoped assignments before adding the new one. This keeps the model simple — a member has at most one role in a group.
- **Remove is permanent**: `DELETE /members/{personId}` already hard-deletes the `GroupMembership` row. The confirmation prompt is a frontend concern; the API is correct as-is.
- **404 on `/schedule-versions/current`**: Expected when no schedule has been published. Frontend should handle this gracefully with an empty state.
- **409 on `POST /people`**: Expected when the person already exists in the space. Frontend should catch and display a friendly message.

## How it connects
- `PersonRoleAssignment` already existed in the domain and DB schema (migration 027). This step wires it into the add-member flow.
- `GetGroupMembersQuery` now returns `roleId` and `roleName` so the frontend can display the member's current role without a separate request.
- The constraint fix unblocks the entire constraints UI which was broken for all spaces.

## How to run / verify

1. Apply the migration:
   ```bash
   psql $DATABASE_URL -f infra/migrations/029_constraint_enums_to_text.sql
   ```

2. Restart the API server.

3. Verify constraints endpoint works:
   ```
   GET /spaces/{spaceId}/constraints  → 200 (was 500)
   POST /spaces/{spaceId}/constraints → 201 (was 500)
   ```

4. Verify member role assignment:
   ```
   POST /spaces/{spaceId}/groups/{groupId}/members
   Body: { "personId": "...", "roleId": "..." }
   → 200 if caller is group owner with a valid group role
   → 400 if caller is not the group owner and roleId is provided
   ```

5. Verify role update:
   ```
   PATCH /spaces/{spaceId}/groups/{groupId}/members/{personId}/role
   Body: { "roleId": "..." }   → 204 (assigns role)
   Body: { "roleId": null }    → 204 (removes role)
   → 403 if caller is not the group owner
   ```

6. Verify GET members returns roleId/roleName:
   ```
   GET /spaces/{spaceId}/groups/{groupId}/members
   → each member now has "roleId" and "roleName" fields
   ```

## What comes next
- Frontend: add role selector to the "Add Member" modal (show roles only when the current user is the group owner)
- Frontend: add role editor to the member detail view (group owner only)
- Frontend: handle 404 on `/schedule-versions/current` gracefully with an empty state
- Frontend: handle 409 on `POST /people` with a "person already exists" message

## Git commit
```bash
git add -A && git commit -m "fix(constraints): convert scope_type/severity enums to text; feat(groups): role assignment at member add time"
```
