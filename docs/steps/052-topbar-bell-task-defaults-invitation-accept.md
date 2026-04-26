# Step 052 — Topbar bell cleanup, task form defaults, invitation accept page, schedule version enum fix

## Phase
Phase 7 — Hardening & UX polish

## Purpose
Four small but important fixes:
1. Remove the dead `SidebarNotificationBell` component from `AppShell.tsx` — the bell moved to the topbar and the sidebar version was never rendered.
2. Pre-fill the task creation form with today's full-day range (`00:00`–`23:59`, 24 h) so admins don't have to type dates every time.
3. Add the `/invitations/accept` page so invitation links actually work — reads the `token` query param, calls `POST /invitations/accept`, and shows appropriate Hebrew feedback.
4. Fix `ScheduleVersionConfiguration` — the `HasConversion` lambda used a switch expression which is illegal inside EF Core expression trees; replaced with a `ValueConverter<>` using ternary chains.

## What was built

| File | Change |
|------|--------|
| `apps/web/components/shell/AppShell.tsx` | Removed `SidebarNotificationBell` function (~100 lines) and unused `useRef` import |
| `apps/web/app/groups/[groupId]/page.tsx` | "הוסף משימה" button now sets `startsAt = today T00:00`, `endsAt = today T23:59`, `durationHours = 24` |
| `apps/web/app/invitations/accept/page.tsx` | New page — reads `token`, checks auth, calls API, shows success/error/loading in Hebrew |
| `apps/api/Jobuler.Infrastructure/Persistence/Configurations/SchedulingConfiguration.cs` | Replaced switch-expression `HasConversion` with `ValueConverter<ScheduleVersionStatus, string>` using ternary chains; added `using Microsoft.EntityFrameworkCore.Storage.ValueConversion` |

## Key decisions
- `ValueConverter<TModel, TProvider>` is the correct EF Core pattern when the conversion logic can't be expressed as a simple expression tree (i.e., when you need multi-branch logic).
- The invitation accept page uses `Suspense` to wrap `useSearchParams()` — required by Next.js App Router.
- Task form defaults use `new Date().toISOString().split("T")[0]` (UTC date) which is consistent with how the rest of the app formats dates.

## How to connect
- The invitation accept page is linked from invitation emails via `/invitations/accept?token=<token>`.
- The `ScheduleVersionStatus.Discarded` enum value is now correctly round-tripped through the DB (migration 016 already added the `discarded` value to the PostgreSQL enum).

## How to run / verify
1. Restart the API after the build — the new binary handles `GET/POST /spaces/{id}/groups/{id}/tasks` and `DELETE /spaces/{id}/constraints/{id}`.
2. Open a group as admin → click "הוסף משימה" → verify start/end fields default to today 00:00–23:59.
3. Visit `/invitations/accept?token=test` while logged out → should show login prompt.
4. Visit the same URL while logged in → should attempt the API call and show success or error.

## What comes next
- Wire invitation emails to include the `/invitations/accept?token=...` link.
- Add redirect-after-login support so the user lands on the accept page after logging in.

## Git commit
```bash
git add -A && git commit -m "fix: topbar bell cleanup, task form defaults, invitation accept page, schedule version discarded enum"
```
