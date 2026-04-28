# Step 071 — Member name display, re-add after remove, deleted groups global, settings tab last

## Phase
Phase 7 — UX Hardening & Quality Pass

## Purpose
Fix four UX issues found during manual testing:
1. Member list should always show `fullName` as the primary name
2. Removing a member then re-adding them failed with 409 (person still exists in space)
3. Deleted groups were buried inside a specific group's Settings tab — moved to global groups page
4. Settings tab should always be last in the tab order

## What was built

### Fix 1 — Member name always shows fullName
- `apps/web/app/groups/[groupId]/tabs/MembersTab.tsx` — changed list item to show `m.fullName` as primary; `displayName` shown as secondary only when it differs from `fullName`; avatar initial uses `fullName`

### Fix 2 — Re-add after remove
- `apps/web/app/groups/[groupId]/page.tsx` — `handleAddMember` now catches 409 from `createPerson`, searches for the existing person by name using `searchPeople`, and calls `addGroupMemberById` with the found ID
- This handles the case where a person was removed from the group but still exists in the space
- `searchPeople` imported from `@/lib/api/people`

### Fix 3 — Deleted groups on global groups page
- `apps/web/app/groups/page.tsx` — added collapsible "קבוצות מחוקות" section at the bottom; lazy-loads on expand; restore button refreshes both the deleted list and the active groups grid
- `apps/web/app/groups/[groupId]/tabs/SettingsTab.tsx` — removed `deletedGroups`, `deletedGroupsLoading`, `onRestoreGroup` props and the entire "קבוצות מחוקות" section
- `apps/web/app/groups/[groupId]/page.tsx` — removed the corresponding state, effect, and props passed to SettingsTab

### Fix 4 — Settings tab last
- `apps/web/app/groups/[groupId]/page.tsx` — `ALL_TABS` reordered: `settings` moved after `stats` so it's always the last tab

## Key decisions
- `searchPeople` requires ≥2 chars — the name entered by the admin is always at least 2 chars (enforced by the required field)
- Deleted groups section is collapsible (hidden by default) to keep the groups page clean
- `fullName` is the canonical identity; `displayName` is a nickname shown only when different

## How to run / verify
1. Members tab → remove a member → "+ הוסף חבר" → enter same name → member re-added successfully
2. Member list shows full name, not display name
3. Groups page → scroll to bottom → "קבוצות מחוקות" toggle → shows deleted groups with restore
4. Group detail → tabs: Settings is now the last tab

## Git commit
```bash
git add -A && git commit -m "fix(groups): member fullName display, re-add after remove, deleted groups global page, settings tab last"
```
