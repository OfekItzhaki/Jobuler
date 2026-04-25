# Step 037 — Group Alert Frontend API and Severity Badge Utility

## Phase
Phase 7 — Group Alerts Frontend

## Purpose
Expose the group alerts API surface to the Next.js frontend and provide a reusable severity badge utility. These are the foundational pieces required before the alerts tab UI (task 7.4) can be built.

## What was built

### Modified
- `apps/web/lib/api/groups.ts`
  - Added `GroupAlertDto` interface with fields: `id`, `title`, `body`, `severity` (union type), `createdAt`, `createdByPersonId`, `createdByDisplayName`
  - Added `getGroupAlerts(spaceId, groupId)` — GET `/spaces/{spaceId}/groups/{groupId}/alerts`
  - Added `createGroupAlert(spaceId, groupId, payload)` — POST to the same route
  - Added `deleteGroupAlert(spaceId, groupId, alertId)` — DELETE `/spaces/{spaceId}/groups/{groupId}/alerts/{alertId}`

### Created
- `apps/web/lib/utils/alertSeverity.ts`
  - `SEVERITY_BADGE` map: `info` → blue, `warning` → amber, `critical` → red, each with `bg`, `text`, `border`, and Hebrew `label` fields
  - `getSeverityBadge(severity)` helper that falls back to `info` for unknown values

## Key decisions
- `severity` is typed as `"info" | "warning" | "critical"` in `GroupAlertDto` to match the backend enum and enable exhaustive checks in the UI.
- `getSeverityBadge` lowercases the input and falls back to `info` so the UI never crashes on unexpected API values.
- `border` was added to `SEVERITY_BADGE` (beyond the design doc's minimal spec) to support bordered badge variants in the alerts tab card UI.

## How it connects
- `GroupAlertDto` and the three API functions are consumed by the alerts tab in `app/groups/[groupId]/page.tsx` (task 7.4).
- `SEVERITY_BADGE` / `getSeverityBadge` are used by the alert card renderer to apply Tailwind classes per severity.
- The API functions follow the same `apiClient` pattern as all other functions in `lib/api/groups.ts`.

## How to run / verify
1. TypeScript check: `npx tsc --noEmit` in `apps/web` — should produce no errors.
2. Import `getGroupAlerts` in any component and verify autocomplete shows the correct return type.
3. Import `getSeverityBadge("warning")` and verify it returns `{ bg: "bg-amber-50", text: "text-amber-700", border: "border-amber-200", label: "אזהרה" }`.

## What comes next
- Task 7.3: Property test for severity badge color correctness (fast-check).
- Task 7.4: Add the "התראות" tab to `app/groups/[groupId]/page.tsx` using these API functions and the severity badge utility.

## Git commit
```bash
git add -A && git commit -m "feat(alerts): add GroupAlertDto, alert API functions, and severity badge utility"
```
