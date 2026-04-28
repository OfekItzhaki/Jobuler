# Step 072 — Notification bell live refresh

## Phase
Phase 7 — UX Hardening & Quality Pass

## Purpose
Notifications were only refreshing every 30 seconds. When the solver finished (or failed due to infeasibility), the admin had to wait up to 30s to see the result in the bell. Two changes fix this:
1. Reduce the background poll interval from 30s → 5s
2. Force an immediate refetch the moment the solver run polling detects a terminal state

## What was built

### Change 1 — Faster poll interval
- `apps/web/lib/query/hooks/useNotifications.ts` — `refetchInterval` reduced from `30_000` to `5_000`
- `refetchIntervalInBackground: false` added so the tab doesn't hammer the API when not focused

### Change 2 — Immediate refetch on solver completion
- `apps/web/lib/query/hooks/useNotifications.ts` — added `useRefetchNotifications(spaceId)` hook that returns a function to invalidate the notifications query cache
- `apps/web/app/groups/[groupId]/page.tsx` — imports and calls `refetchNotifications()` the moment the solver polling detects `Completed`, `Failed`, or `TimedOut`

## Key decisions
- 5s is a reasonable balance: fast enough to feel live, cheap enough not to overload the API
- `refetchIntervalInBackground: false` prevents unnecessary requests when the user is on another tab
- The immediate invalidation on solver completion means the notification appears within ~1s of the run finishing, regardless of where the 5s timer is

## How to run / verify
1. Trigger the solver with insufficient people → run fails → notification bell badge appears within ~1s of the run status changing to Failed
2. Trigger solver successfully → "הסידור מוכן לעיון" notification appears immediately when polling detects Completed

## Git commit
```bash
git add -A && git commit -m "feat(notifications): 5s poll interval, immediate refetch on solver completion"
```
