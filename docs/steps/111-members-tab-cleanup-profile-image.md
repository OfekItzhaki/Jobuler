# Step 111 — Members Tab Cleanup + Profile Image in Modal

## Phase
Phase 9 — Polish & Hardening

## Purpose
Two small cleanups after the Roles tab extraction:

1. The `MembersTab` still had an inline role editor (the "Role" button per member row) that was only visible to the group owner. Now that role assignment lives in the dedicated `RolesTab`, this was a duplicate UI path — confusing and inconsistent with the single-role-per-member model.

2. The member profile modal showed a blue initial avatar even when the member had a profile image set. The `profileImageUrl` field was already in `GroupMemberDto` but wasn't being used in the modal view.

## What was changed

### `apps/web/app/groups/[groupId]/tabs/MembersTab.tsx`
- Removed the inline role editor: the "Role" button, the `editingRoleFor` / `roleEditValue` / `roleSaving` / `roleErrors` state, and the `handleSaveRole` function
- Removed the `isOwner` prop usage for role editing (prop kept in interface for ownership transfer logic elsewhere, but no longer drives role UI)
- The `groupRoles` prop and `onUpdateMemberRole` prop are still accepted (used by `RolesTab` via `page.tsx`) — no interface changes needed
- Member profile modal: replaced the hardcoded blue initial avatar with a conditional — shows `<img>` when `profileImageUrl` is set, falls back to the initial avatar otherwise

## Key decisions
- Role assignment is now exclusively managed in `RolesTab` — one place, one mental model
- The `MembersTab` still shows the role badge on each member row (read-only) so admins can see assignments at a glance without switching tabs

## Git commit

```bash
git add -A && git commit -m "fix(members): remove duplicate inline role editor; show profile image in member modal"
```
