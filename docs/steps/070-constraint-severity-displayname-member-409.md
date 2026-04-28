# Step 070 — Constraint severity crash, display name refresh, member 409 fix

## Phase
Phase 7 — UX Hardening & Quality Pass

## Purpose
Fix three bugs found during manual testing after step 069:
1. Constraints tab crashed with `c.severity?.toLowerCase is not a function` — severity was a number
2. "Connected as" only refreshed when Zustand was empty — needed to refresh on every mount
3. Adding a member by name returned 409 Conflict with a generic error message

## What was built

### Fix 1 — Constraint severity crash
- `apps/api/Jobuler.Api/Program.cs` — added `JsonStringEnumConverter` globally so all C# enums serialize as strings (`"Hard"`, `"Soft"`) instead of integers (`0`, `1`)
- `apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx` — added defensive normalisation: if `severity` is a number (legacy data), map `0 → "hard"`, `1 → "soft"`; if string, `.toLowerCase()`
- `apps/web/app/groups/[groupId]/page.tsx` — same normalisation applied when setting `editConstraintSeverity` in `onStartEdit`

### Fix 2 — "Connected as" always fresh
- `apps/web/components/shell/AppShell.tsx` — `getMe()` now fires unconditionally on mount (empty dep array) instead of only when `storedDisplayName` is falsy; falls back to Zustand value on error

### Fix 3 — 409 on add member
- `apps/web/app/groups/[groupId]/page.tsx` — `handleAddMember` now catches the error response status; on 409 shows "כבר קיים אדם בשם זה במרחב. שנה את השם או השתמש בשם אחר." instead of a generic error

## Key decisions
- `JsonStringEnumConverter` is applied globally — affects all enums in all responses (ConstraintSeverity, ConstraintScopeType, ScheduleVersionStatus, etc.). This is a breaking change for any client that expected integer enums, but the only client is our own frontend which already expected strings.
- The frontend still handles the numeric case defensively so existing cached/stale data doesn't crash the tab.
- `getMe()` on every AppShell mount is cheap (one GET /auth/me) and ensures the name is always current after profile edits or login.

## How it connects
- `Program.cs` change propagates to all API responses — no individual controller changes needed
- The defensive number-handling in `ConstraintsTab` can be removed once all clients have migrated to the new API

## How to run / verify
1. Restart the API — constraints tab should no longer crash
2. Existing Hard constraints should show "קשה" badge (red), Soft should show "רך" (blue)
3. Log in → sidebar shows correct name immediately on every page
4. Add member with a name that already exists → shows Hebrew conflict message

## Git commit
```bash
git add -A && git commit -m "fix(group): constraint severity crash, display name always fresh, member 409 message"
```
