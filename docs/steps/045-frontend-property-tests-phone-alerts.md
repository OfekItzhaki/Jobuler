# Step 045 — Frontend Property Tests: Phone Rendering, Severity Badge, Delete Buttons

## Phase
Phase: Group Alerts and Phone — Frontend Property Tests

## Purpose
Adds three frontend property tests (tasks 2.2, 7.3, 7.5) that verify UI logic for phone number rendering, severity badge colors, and delete button visibility. These tests run as plain Node.js scripts compiled from TypeScript — no test framework required.

## What was built

### Files created

- **`apps/web/__tests__/phoneNumberRendering.test.ts`** (task 2.2)
  Property 2: Phone number renders correctly for all members.
  Tests that `null` phone numbers never render as the string `"null"` or `"undefined"`, that non-null phones render as-is, and that mixed lists behave correctly across 100 iterations.

- **`apps/web/__tests__/alertSeverity.test.ts`** (task 7.3)
  Property 10: Severity badge color is correct for all severity values.
  Tests that `getSeverityBadge` returns blue for `info`, amber for `warning`, red for `critical`; that the lookup is case-insensitive; that unknown severities fall back to `info`; and that all three severities have distinct bg classes. Runs 100 iterations over all valid severities.

- **`apps/web/__tests__/alertDeleteButtons.test.ts`** (task 7.5)
  Property 11: Delete buttons appear only on own alerts (backend enforcement).
  Tests that `canDeleteAlert` returns `true` only when `alert.createdByPersonId === currentPersonId`, that non-admins never see delete buttons, and that admins see delete buttons on all alerts (with backend enforcing creator-only). Runs 100 iterations over mixed creator IDs.

- **`apps/web/tsconfig.tests.json`**
  A separate TypeScript config for compiling test files to CommonJS (`__tests__/dist/`). Includes only `__tests__/**/*.ts`, `lib/api/groups.ts`, and `lib/utils/**/*.ts` to avoid browser-only globals in `lib/api/client.ts`.

## Key decisions

- **Plain Node.js assertions** — follows the same pattern as `groupAvatar.test.ts`: `import * as assert from "assert"`, a local `test()` helper, and `process.exit(1)` on failure. No Jest, Vitest, or fast-check runtime needed.
- **Type-only imports from `lib/api/groups.ts`** — `GroupMemberDto` and `GroupAlertDto` are interfaces; importing them compiles to nothing at runtime, so no `apiClient` dependency is pulled in.
- **Separate `tsconfig.tests.json`** — the main `tsconfig.json` uses `moduleResolution: bundler` and `noEmit: true` (Next.js defaults). A separate config with `module: commonjs` and `moduleResolution: node` is needed to produce runnable `.js` files in `__tests__/dist/`.
- **Backend vs. frontend delete logic** — the test explicitly models both the UI check (`isAdmin` gate) and the backend check (`createdByPersonId === currentPersonId`), documenting that the UI shows delete for all admin-owned alerts while the backend enforces creator-only.

## How it connects

- `phoneNumberRendering.test.ts` validates the `GroupMemberDto.phoneNumber` interface added in task 2.1.
- `alertSeverity.test.ts` validates `SEVERITY_BADGE` and `getSeverityBadge` from `lib/utils/alertSeverity.ts` (task 7.2).
- `alertDeleteButtons.test.ts` validates the delete-button visibility logic from the alerts tab UI (task 7.4).

## How to run / verify

Compile first (only needed when source files change):

```bash
node apps/web/node_modules/typescript/bin/tsc -p apps/web/tsconfig.tests.json
```

Then run each test:

```bash
node apps/web/__tests__/dist/__tests__/phoneNumberRendering.test.js
node apps/web/__tests__/dist/__tests__/alertSeverity.test.js
node apps/web/__tests__/dist/__tests__/alertDeleteButtons.test.js
```

Expected output for each: all tests pass, `Results: N passed, 0 failed`.

## What comes next

- Tasks 11.2–11.9 — optional Forgot Password backend property tests
- Step 14 checkpoint — run all tests and confirm the spec is complete

## Git commit

```bash
git add -A && git commit -m "test(group-alerts): add frontend property tests for phone rendering, severity badge, delete buttons (tasks 2.2, 7.3, 7.5)"
```
