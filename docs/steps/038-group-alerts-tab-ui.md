# Step 038 — Group Alerts Tab UI

## Phase
Phase 7 — Group Alerts & Phone Feature

## Purpose
Adds the "התראות" (Alerts) tab to the group detail page, allowing all group members to view alerts and admins to create/delete them.

## What was built

### Modified
- `apps/web/app/groups/[groupId]/page.tsx` — Added the full alerts tab UI:
  - Imported `getGroupAlerts`, `createGroupAlert`, `deleteGroupAlert`, `GroupAlertDto` from `@/lib/api/groups`
  - Imported `getSeverityBadge` from `@/lib/utils/alertSeverity`
  - Extended `ActiveTab` type to include `"alerts"`
  - Added 9 new state variables for alerts (list, loading, error, form fields, submit/delete errors)
  - Added `useEffect` to fetch alerts when the alerts tab becomes active
  - Added `fetchAlerts()` function
  - Added `handleCreateAlert()` and `handleDeleteAlert()` functions
  - Added `{ value: "alerts", label: "התראות" }` to `baseTabs` (visible to all members, not just admins)
  - Added `case "alerts": return renderAlertsPanel()` to `renderTabPanel` switch
  - Added `renderAlertsPanel()` function with create form (admin only) and alerts list with severity badges

## Key decisions
- Alerts tab is in `baseTabs` so all members (not just admins) can see alerts
- The create form and delete button are gated behind `isAdmin` on the frontend; the backend enforces creator-only delete
- Severity badge styling reuses `getSeverityBadge` from the shared utility

## How it connects
- Calls the API functions from step 037 (`getGroupAlerts`, `createGroupAlert`, `deleteGroupAlert`)
- Uses `getSeverityBadge` from `alertSeverity.ts` (step 037) for consistent color coding
- Integrates into the existing tab system in the group detail page (step 028/030)

## How to run / verify
1. Navigate to a group detail page
2. The "התראות" tab should appear for all users
3. As admin: create an alert with title, body, and severity — it should appear in the list
4. As admin: delete an alert — it should be removed from the list
5. As non-admin: the create form and delete buttons should not be visible

## What comes next
- Task 7.5: Step documentation (this file)

## Git commit
```bash
git add -A && git commit -m "feat(group-alerts): add התראות tab UI to group detail page"
```
