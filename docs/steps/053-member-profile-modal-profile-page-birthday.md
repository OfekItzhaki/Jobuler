# Step 053 — Member Profile Modal, Profile Page, Birthday Fields, Member Add Fixes

## Phase
Phase 6 — UX Enhancements

## Purpose
Completes the frontend implementation for several backend features that were already deployed:
- `GroupMemberDto` now includes `invitationStatus` and `profileImageUrl` from the API
- `POST /spaces/{spaceId}/groups/{groupId}/members` endpoint (add by personId)
- `PUT /spaces/{spaceId}/people/{personId}/info` endpoint (edit person details)
- `GET /auth/me` and `PUT /auth/me` endpoints
- `RegisterCommand` now accepts `profileImageUrl` and `birthday`

## What was built

### `apps/web/lib/api/groups.ts`
- Updated `GroupMemberDto` to include `invitationStatus: string` and `profileImageUrl: string | null`
- Added `addGroupMemberById(spaceId, groupId, personId)` — calls `POST .../members` with `{ personId }`
- Added `updatePersonInfo(spaceId, personId, payload)` — calls `PUT .../people/{personId}/info`

### `apps/web/lib/api/auth.ts`
- Updated `register()` to accept optional `profileImageUrl` and `birthday` parameters
- Added `MeDto` interface
- Added `getMe()` — calls `GET /auth/me`
- Added `updateMe(payload)` — calls `PUT /auth/me`

### `apps/web/app/groups/[groupId]/page.tsx`
- **Task 2**: Fixed `handleCreatePerson` — now calls `POST .../members` with `{ personId }` cleanly, removed broken fallback
- **Task 2**: Added guard in `handleAddMember` — shows "יש להזין אימייל או מספר טלפון" if input is empty
- **Task 3**: "הזמן" button now only renders when `m.invitationStatus !== "accepted"`
- **Task 4**: Member profile modal — clicking any member's avatar or name opens a modal with:
  - Large avatar (80px, blue gradient or profile image)
  - Full name, display name, phone with icon, status badge, owner badge
  - Admin edit mode: form with fullName, displayName, phoneNumber, profileImageUrl, birthday fields
  - Save calls `PUT /spaces/{spaceId}/people/{personId}/info`, then re-fetches members
  - Backdrop click closes modal; X button in top-right corner
- Member rows made clickable (cursor pointer) in both read-only and edit views
- Avatar now shows profile image if available, otherwise gradient with initial

### `apps/web/app/register/page.tsx`
- Added `birthday` (date input) and `profileImageUrl` (URL input) optional fields after phone number
- Updated `register()` call to pass these new fields

### `apps/web/app/profile/page.tsx` (new file)
- Premium profile page for the logged-in user
- Calls `GET /auth/me` on load
- Hero section: avatar (96px gradient or profile image), display name, email, "עריכה" button
- Info cards grid (2-column): "פרטי קשר" (phone + email) and "פרטים אישיים" (birthday + member since)
- Edit mode: inline form with displayName, phoneNumber, profileImageUrl, birthday; saves via `PUT /auth/me`
- RTL layout, Hebrew labels, consistent card styling

### `apps/web/components/shell/AppShell.tsx`
- Added "הפרופיל שלי" NavItem pointing to `/profile` after "הקבוצות שלי"

### `apps/web/__tests__/phoneNumberRendering.test.ts`
- Updated test fixtures to include the new `invitationStatus` and `profileImageUrl` fields on `GroupMemberDto`

## Key decisions
- Member modal uses inline `style` props (consistent with the profile page) rather than Tailwind, since it's a floating overlay outside the normal flow
- Edit mode in the modal only appears for admins editing non-owner members — owners are protected
- `handleCreatePerson` no longer has a broken fallback; it fails cleanly if the API call fails

## How it connects
- The modal reads `isAdmin` from the existing admin mode state
- `updatePersonInfo` is a new export from `groups.ts` that maps to the new backend endpoint
- The profile page is a standalone route wrapped in `AppShell`

## How to run / verify
1. Start the dev server: `npm run dev` in `apps/web`
2. Navigate to a group → Members tab → click any member name/avatar → modal opens
3. Enter admin mode → click a non-owner member → "ערוך" button appears in modal
4. Register page at `/register` now shows birthday and profile picture URL fields
5. Profile page at `/profile` shows user info and allows editing

## Git commit

```bash
git add -A && git commit -m "feat(frontend): member profile modal, profile page, birthday fields, fix member add errors, hide invite for confirmed users"
```
