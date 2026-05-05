# Step 091 — Run Pending Migrations & Member Role Editing

## Phase
Phase 9 — Member Management Enhancements

## Purpose
Two things in this step:

1. **Apply pending migrations** (029–031) to the local database so constraints, tasks, and the default member role all work correctly.
2. **Make member roles editable** in the Members tab — the group owner can now assign or change any member's role directly from the member list, without going to Settings.

## Migrations applied

| Migration | What it does |
|---|---|
| 029 | Converts `constraint_rules.scope_type` and `severity` from native PG enum types to TEXT — fixes the 500 on `GET/POST /constraints` |
| 030 | Adds `is_default BOOLEAN` to `space_roles` + partial unique index (one default per group) |
| 031 | Backfills a "Member" default role for every existing group; assigns it to all current members who had no role |

Output confirmed: 2 groups got default roles, 4 members got role assignments, 1 schema_migrations row inserted.

## What was built

### API layer
- `GroupAlertCommands.cs` — `GetGroupAlertsQueryHandler` now allows space admins (`space.admin_mode` permission) to view group alerts even if they're not a group member. Previously only members could view alerts.

### Frontend — `lib/api/groups.ts`
- `GroupMemberDto` — added `roleId: string | null` and `roleName: string | null` fields (already returned by the backend since step 088)
- `GroupRoleDto` — added `isDefault: boolean` field (returned by backend since step 089)
- `updateMemberRole(spaceId, groupId, personId, roleId)` — new API function calling `PATCH /members/{personId}/role`

### Frontend — `MembersTab.tsx`
- Added `isOwner` and `groupRoles` props
- Each member row now shows a role badge (e.g. "Member", "מפקד") next to their name; owner gets an "בעלים" badge
- Group owner sees a "תפקיד" button per non-owner member that expands an inline role editor
- Inline editor: dropdown of all active group roles (default role shown with "(ברירת מחדל)" suffix) + "ללא תפקיד" option + save/cancel
- Role badge updates immediately on save (optimistic local state update)
- Remove confirmation is now a two-step inline confirm (was immediate) — prevents accidental removals
- Role name also shown in the member profile modal

### Frontend — `page.tsx`
- Added `handleUpdateMemberRole` handler — calls API, then updates local `members` state so the badge refreshes without a full reload
- Added `useEffect` to load `groupRoles` when the members tab opens (needed for the role dropdown)
- `isOwner` computed as: current user's `userId` matches the `linkedUserId` of the member whose `personId` equals `group.ownerPersonId`
- Passes `isOwner`, `groupRoles`, `onUpdateMemberRole` to `MembersTab`

### Tests
- `__tests__/phoneNumberRendering.test.ts` — updated all `GroupMemberDto` fixtures to include `roleId: null, roleName: null`

## Key decisions
- **Owner-only role editing**: non-owner admins can add members but can't change roles — consistent with the backend enforcement
- **Inline editor, not modal**: keeps the interaction lightweight; the dropdown is small enough to fit inline
- **Optimistic update**: role badge updates immediately without waiting for a GET /members reload — better UX, and the backend is the source of truth on next load
- **Default role shown in dropdown**: `isDefault` flag lets the frontend label it clearly so the owner knows which one is the fallback

## How to verify
1. Log in as `admin@demo.local`
2. Open any group → Members tab
3. Enter admin mode
4. Each member should show a "Member" role badge (from the backfill)
5. Click "תפקיד" on any non-owner member → dropdown appears with available roles
6. Select a role and save → badge updates immediately
7. Refresh the page → role persists

## Git commit
```bash
git add -A && git commit -m "feat(members): editable member roles in members tab; fix(alerts): space admin can view group alerts; fix(db): apply migrations 029-031"
```
