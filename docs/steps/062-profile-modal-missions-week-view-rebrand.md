# Step 062 — Profile Edit Modal, Missions Week View & Shifter Rebrand

## Phase
Phase 7 — UX Polish & Branding

## Purpose
Three UX improvements in one step:
1. Move profile editing into a modal (consistent with the rest of the app's modal-first pattern)
2. Redesign the "My Missions" page to show week-day buttons that open a missions table modal
3. Refresh the Shifter logo and favicon SVGs

## What was built

### `apps/web/app/profile/page.tsx`
- Removed the inline edit form that replaced the hero card
- Added a `Modal` (using the existing `Modal` component) that opens when the user clicks "עריכה"
- The modal contains the same form fields (display name, phone, profile image, birthday)
- The profile view card is always visible; editing happens in the overlay

### `apps/web/app/schedule/my-missions/page.tsx`
- Added week-day buttons (ראשון–שבת) that appear when the "השבוע" range is selected
- Each button shows a green dot badge if there are missions on that day
- Today's button is highlighted with a blue border
- Clicking a day button opens a `Modal` with a table:
  - Row header: mission name (taskTypeName)
  - Side column: time range (e.g. 10:00 – 14:00)
  - Third column: group name
- The existing list view below the buttons is preserved for full context

### `apps/web/public/logo.svg`
- Refreshed to a horizontal lockup: icon mark (blue rounded square with S + clock) + "Shifter" wordmark

### `apps/web/public/favicon.svg`
- Refined S letterform proportions and clock indicator for better legibility at small sizes

## Key decisions
- Reused the existing `Modal` component — no new dependencies
- Week days are computed client-side from the current date (Sunday = index 0)
- The day modal shows an empty state message when no missions exist for that day
- Fixed pre-existing TypeScript errors in `groups/[groupId]/page.tsx`: `a.startsAt` → `a.slotStartsAt` and `a.endsAt` → `a.slotEndsAt` to match the `ScheduleAssignment` interface in `types.ts`. This unblocked the production build.

## How it connects
- `Modal` component lives at `components/Modal.tsx` and is already used across the app
- `my-missions` page fetches from `/spaces/{id}/my-assignments?range=week` — same endpoint, no API changes
- Profile page uses `getMe` / `updateMe` from `lib/api/auth` — no API changes

## How to run / verify
1. Start the dev server: `npm run dev` (from `apps/web`)
2. Navigate to `/profile` — click "עריכה" → modal should open
3. Navigate to `/schedule/my-missions` → select "השבוע" → day buttons appear → click any day → table modal opens
4. Check favicon in browser tab and `/logo.svg` in browser

## What comes next
- Manual QA pass by the user
- LTS tagging once the version is confirmed stable

## Git commit
```bash
git add -A && git commit -m "feat(ux): profile edit modal, missions week-day buttons with table modal, logo refresh"
```
