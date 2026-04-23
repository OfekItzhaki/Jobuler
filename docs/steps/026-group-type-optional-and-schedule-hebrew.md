# Step 026 — Group Type Optional + Schedule Page Hebrew

## Phase
Phase 5 — UX Hardening

## Purpose
Two fixes that were blocking correct operation:
1. `GetGroupsQuery` used an inner join on `GroupTypeId`, which threw when groups had no type (null FK). Groups are now type-free by design.
2. Admin schedule page still had English text and was missing the emergency replan button in the JSX.

## What was built

### `apps/api/Jobuler.Application/Groups/Queries/GetGroupsQuery.cs`
- `GroupDto` record: `GroupTypeId` changed from `Guid` → `Guid?`, `GroupTypeName` from `string` → `string?`
- `GetGroupsQueryHandler`: replaced inner join with a two-step query — load all groups first, then fetch type names only for groups that have a `GroupTypeId`. This is a safe left-join equivalent in EF Core.

### `apps/web/app/admin/schedule/page.tsx`
- All English UI strings translated to Hebrew (header, version list, buttons, empty state, loading state)
- Added "סידור חירום" (emergency replan) red button next to the regular solver button — calls `handleEmergencyTrigger` which was already implemented but never rendered

## Key decisions
- Two-query approach for groups avoids EF Core's inability to do a clean left join on a nullable FK without raw SQL. Performance is fine since type IDs are fetched in a single `WHERE IN` query.
- Emergency button is visually distinct (red) to signal its severity.

## How it connects
- Groups page already sends `groupTypeId: null` — the backend now handles that correctly end-to-end.
- Schedule page is now fully Hebrew, consistent with the rest of the admin UI.

## How to run / verify
1. Start API: `cd apps\api && dotnet run --project Jobuler.Api`
2. Start frontend: `cd apps\web && npm run dev`
3. Go to Admin → Groups → create a group (no type field) → it should appear in the list
4. Go to Admin → Schedule → verify Hebrew labels and both solver buttons appear

## What comes next
- Verify solver `triggerSolve` accepts an `"emergency"` mode parameter on the backend
- Consider adding a confirmation dialog before emergency replan

## Git commit
```bash
git add -A && git commit -m "fix(groups): left-join for nullable GroupTypeId; translate schedule page to Hebrew" && git push
```
