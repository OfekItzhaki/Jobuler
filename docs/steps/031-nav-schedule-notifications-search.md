# Step 031 — Nav Restructure, Date-Navigable Schedule, Notifications Page, My Missions Search

## Phase
Phase 8 — UX Improvements

## Purpose
Four focused frontend improvements to streamline navigation, make the group schedule more useful, add a notifications inbox, and make the my-missions page searchable.

## What was built

### `apps/web/components/shell/AppShell.tsx` (modified)
- Removed "סידור" and "קבוצות" section labels and the "היום" / "מחר" nav items
- Replaced with three flat nav items: "המשימות שלי", "הקבוצות שלי", "הודעות"
- Updated the logged-in user section to show an avatar circle (first letter, blue background) alongside the display name and "מחובר" subtitle

### `apps/web/app/groups/[groupId]/page.tsx` (modified)
- Added `scheduleDate` (YYYY-MM-DD, defaults to today) and `scheduleView` ("day" | "week") state variables
- Replaced the static `renderSchedulePanel()` with a date-navigable version:
  - Prev/next day buttons clamped to `[today − 2 days, today + solverHorizonDays]`
  - "היום" shortcut button
  - Day view: filtered table of assignments for the selected date
  - Week view: grouped list of assignments for each day of the selected week
  - Assignments are filtered client-side from the already-fetched `scheduleData`

### `apps/web/app/notifications/page.tsx` (created)
- New page at `/notifications` listing all space notifications
- Fetches from `GET /spaces/{spaceId}/notifications`
- Unread notifications shown first with blue accent; clicking marks as read via `PATCH .../read`
- Empty state with bell icon

### `apps/web/app/schedule/my-missions/page.tsx` (modified)
- Added `search` state
- Search input below the range selector filters assignments by `taskTypeName` or `groupName` before grouping by date

## Key decisions
- Schedule filtering is done client-side since the full schedule is already fetched; no extra API calls needed
- The notifications page reuses the existing `apiClient` and `useSpaceStore` patterns
- Nav simplification removes rarely-used "today/tomorrow" shortcuts in favour of the date-navigable group schedule

## How it connects
- The new `/notifications` route is linked from the sidebar nav item
- `scheduleDate` / `scheduleView` are local state — no URL params needed for this use case
- All changes are purely presentational; no API or domain changes required

## How to run / verify
1. `cd apps/web && npm run dev`
2. Check sidebar shows three items: המשימות שלי, הקבוצות שלי, הודעות
3. Open a group → Schedule tab → verify date nav and day/week toggle work
4. Visit `/notifications` — page loads and marks items read on click
5. Visit `/schedule/my-missions` — search box filters the list

## What comes next
- Notification badge count on the bell icon in the topbar
- Push / real-time notifications via WebSocket

## Git commit
```bash
git add -A && git commit -m "feat(ux): nav restructure, date-navigable schedule, notifications page, my-missions search"
```
