# Step 012 — Phase 5: UX, Schedule Tables, Admin Workflow, Logs UI

## Phase
Phase 5 — UX and Localization

## Purpose
Build the user-facing schedule views (Today/Tomorrow with real data), the admin draft review and publish/rollback UI, and the system logs viewer. All views are permission-aware and RTL-compatible.

## What was built

### Frontend

| File | Description |
|---|---|
| `lib/api/schedule.ts` | API client functions: getCurrentSchedule, getVersionDetail, triggerSolve, publishVersion, rollbackVersion, getRunStatus |
| `lib/store/spaceStore.ts` | Zustand store for current space selection (persisted) |
| `components/schedule/ScheduleTable.tsx` | Filterable assignment table with person, task, time, source columns |
| `components/schedule/DiffSummaryCard.tsx` | Visual diff summary: added/removed/changed counts + stability score |
| `app/schedule/today/page.tsx` | Today's schedule — loads current published version, filters to today's date |
| `app/schedule/tomorrow/page.tsx` | Tomorrow's schedule — same pattern, filters to tomorrow |
| `app/admin/schedule/page.tsx` | Admin draft review: version list, diff card, assignment table, publish/rollback/trigger buttons |
| `app/admin/logs/page.tsx` | System logs viewer with severity filter and color-coded severity badges |
| `components/shell/AppShell.tsx` | Updated nav: admin links for Schedule and Logs appear only in admin mode |

### Backend additions

| File | Description |
|---|---|
| `Domain/Logs/SystemLog.cs` | Append-only system log entity |
| `Domain/Logs/AuditLog.cs` | Append-only audit log entity |
| `Application/Logs/Queries/GetSystemLogsQuery.cs` | Paginated log query with severity/eventType/date filters; hides sensitive logs without permission |
| `Api/Controllers/LogsController.cs` | `GET /spaces/{id}/logs` — requires admin_mode; sensitive entries require logs.view_sensitive |
| `Infrastructure/Persistence/Configurations/LogsConfiguration.cs` | EF Core mappings for system_logs and audit_logs |

## Key decisions

### Today/Tomorrow filter is client-side
The schedule API returns all assignments for the current published version. The `ScheduleTable` component filters by `filterDate` (ISO date prefix match on `slotStartsAt`). This avoids an extra API call and keeps the component reusable.

### Admin mode gates all admin UI
`app/admin/schedule/page.tsx` and `app/admin/logs/page.tsx` check `isAdminMode` from the auth store. If not in admin mode, they render a prompt instead of the content. The actual permission enforcement is on the API — the frontend check is UX only.

### Severity color coding matches spec Section 9.3
- Info → blue
- Warning → amber
- Error → red
- Critical → bold red

### Sensitive logs hidden without permission
`GetSystemLogsQuery` filters out `is_sensitive = true` entries unless `IncludeSensitive = true`. The controller checks `logs.view_sensitive` permission and passes the result into the query. Sensitive entries never reach unauthorized clients.

## How to run / verify

```bash
cd apps/web && npm install && npm run dev
# Open http://localhost:3000
# Login → redirects to /schedule/today
# Enter admin mode → Schedule and Logs links appear in nav
# /admin/schedule → version list, trigger solve, publish, rollback
# /admin/logs → system log viewer with severity filter
```

## What comes next
- Phase 6: CSV export, observability, rate limiting, security hardening

## Git commit

```bash
git add -A && git commit -m "feat(phase5): schedule tables, admin workflow UI, logs viewer"
```
