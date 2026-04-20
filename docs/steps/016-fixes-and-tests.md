# Step 016 — Fixes, Tests, and Missing UI

## Phase
Post-MVP Hardening

## Purpose
Fix the gaps identified after the initial build: access_token cookie sync, space selector UI, people management UI, .NET test project, Python solver tests, and CI pipeline corrections.

## What was built / fixed

### Auth cookie fix
- `authStore.ts` — now sets `access_token` cookie (max-age 900s = 15 min) alongside localStorage on login, logout, and token refresh. The Next.js middleware route guard reads this cookie.
- `client.ts` — Axios interceptor now also updates the cookie when rotating the access token.

### Space selector
- `app/spaces/page.tsx` — Lists all spaces the user belongs to. Auto-selects if only one space. Create new space form included.
- `lib/api/spaces.ts` — `getMySpaces()` and `createSpace()` API calls.
- `app/page.tsx` — Root now redirects to `/spaces` instead of `/schedule/today`.
- `middleware.ts` — `/spaces` added to public paths (no auth redirect loop).
- `AppShell.tsx` — Shows current space name as a link back to `/spaces`.

### People management UI
- `lib/api/people.ts` — Full people API client: list, detail, create, update, add restriction.
- `app/admin/people/page.tsx` — People list with create form. Admin mode required.
- `app/admin/people/[personId]/page.tsx` — Person detail: roles, groups, qualifications, restrictions with add form.
- `AppShell.tsx` — People link added to admin nav.

### .NET test project
- `Jobuler.Tests/Jobuler.Tests.csproj` — xUnit + FluentAssertions + NSubstitute + EF InMemory.
- `Domain/ScheduleVersionTests.cs` — Tests publish rules, rollback creation, status transitions.
- `Domain/PersonRestrictionTests.cs` — Tests restriction creation.
- `Domain/PresenceWindowTests.cs` — Tests OnMission derived-only enforcement.
- `Application/CreateSpaceCommandTests.cs` — Tests space creation with owner permissions using EF InMemory.
- Added to `Jobuler.sln`.

### Python solver tests
- `tests/test_engine.py` — 10 tests covering: basic feasibility, headcount, no-overlap, stability baseline preservation, metrics output.
- `requirements.txt` — Added pytest and ruff.

### CI pipeline
- Removed `continue-on-error` from .NET test step (tests now exist).
- Added `pytest tests/ -v` to solver CI job.

## What this fixes

| Issue | Fix |
|---|---|
| Login → redirect broken | access_token cookie now set on login |
| No space selector | `/spaces` page with auto-select |
| No people management UI | Full CRUD pages under `/admin/people` |
| No tests | .NET domain + application tests, Python solver tests |
| CI tests skipped | `continue-on-error` removed |

## Git commit

```bash
git add -A && git commit -m "fix: auth cookie, space selector, people UI, tests, CI corrections"
```
