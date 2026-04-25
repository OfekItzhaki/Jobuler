# Step 033 — Group Member Phone Number Display in UI

## Phase
Phase 8 — Group Alerts and Phone

## Purpose
Surface the `phoneNumber` field on group members in the frontend member list, so admins and regular members can see phone numbers alongside display names.

## What was built

- `apps/web/lib/api/groups.ts` — Added `phoneNumber: string | null` to the `GroupMemberDto` interface.
- `apps/web/app/groups/[groupId]/page.tsx` — Updated both `renderMembersReadOnly()` and `renderMembersEdit()` to render the phone number as `<span className="text-xs text-slate-400 mr-2">` when non-null. Null/undefined values are never rendered.

## Key decisions

- Used a simple `{m.phoneNumber && (...)}` guard to ensure "null" or "undefined" strings are never shown.
- Phone number appears after the display name (and after the owner badge in edit mode) to keep the visual hierarchy clean.

## How it connects

- `GroupMemberDto.phoneNumber` is populated by the API's `GetGroupMembersQuery` handler (added in step 032).
- The UI now reflects the full member data returned by `GET /spaces/{spaceId}/groups/{groupId}/members`.

## How to run / verify

1. Start the API and web app.
2. Navigate to a group detail page → Members tab.
3. Members with a phone number should show it in small gray text next to their name.
4. Members without a phone number show nothing extra.

## What comes next

- Alert/notification settings per group (remaining tasks in the group-alerts-and-phone spec).

## Git commit

```bash
git add -A && git commit -m "feat(groups): display phone number in member list UI"
```
