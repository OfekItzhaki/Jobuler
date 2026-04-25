# Step 050 — Admin Management and Scheduling: Test Suite

## Phase
Phase 8 — Test Coverage (admin-management-and-scheduling spec, Tasks 27–33)

## Purpose
Write the full test suite for the admin management and scheduling feature. All backend code was already written and compiling. This step adds unit tests, property-based tests, and integration tests to verify correctness of the new handlers, validators, and domain logic.

Also fixes the existing `GroupAlertPropertyTests.cs` — Property9 previously tested that cross-owner deletion was rejected, but the ownership check was removed in Task 10. The test now verifies the opposite: any `people.manage` holder can delete any alert.

## What was built

### Modified
- `apps/api/Jobuler.Tests/Application/GroupAlertPropertyTests.cs`
  - Renamed `Property9_DeleteGroupAlertCommand_RejectsCrossOwnerDeletion` → `Property9_DeleteGroupAlertCommand_AllowsAnyAdminToDelete`
  - Inverted the assertion: now verifies deletion succeeds (no exception) and alert is gone

### Created
- `apps/api/Jobuler.Tests/Application/AdminManagementHandlerTests.cs` (Task 28)
  - 28.1: `DeleteGroupAlertCommandHandler` — any `people.manage` holder can delete any alert
  - 28.2: `DeleteGroupMessageCommandHandler` — admin bypass works for non-authors
  - 28.3: `PinGroupMessageCommandHandler` — pin/unpin round-trip
  - 28.4: `UpdateConstraintCommandValidator` — rejects invalid JSON and `effectiveUntil < effectiveFrom`
  - 28.5: `CreateGroupTaskCommandValidator` — rejects all invalid inputs (empty name, whitespace, >200 chars, bad dates, bad duration, bad headcount, bad burden level)

- `apps/api/Jobuler.Tests/Application/GroupTaskPropertyTests.cs` (Task 29)
  - Property 1: Valid task inputs → create → list → fields match (10 InlineData cases)
  - Property 2: `ends_at ≤ starts_at` → validator rejects (5 cases)
  - Property 3: Invalid `burden_level` → validator rejects (5+ cases)
  - Property 4: Create → delete → list → absent (5 cases)
  - Property 5: Create N tasks in reverse order → list → ascending `starts_at` (4 cases: 2, 3, 5, 10 tasks)

- `apps/api/Jobuler.Tests/Application/ConstraintPropertyTests.cs` (Task 30)
  - Property 6: Create constraint → update → verify fields match (5 cases)
  - Property 7: Non-JSON strings → validator rejects (9 cases; `"null"` excluded — it's valid JSON)
  - Property 8: `effectiveUntil < effectiveFrom` → validator rejects (5 cases)
  - Property 9: Create → delete → list → absent (5 cases)

- `apps/api/Jobuler.Tests/Application/AlertMessageAdminPropertyTests.cs` (Task 31)
  - Property 10: Create alert as user A → delete as user B → 204 and gone (5 cases)
  - Property 11: Create message as user A → delete as user B → 204 and gone (5 cases)
  - Property 12: Create message → pin → unpin → `isPinned = false` (5 cases)
  - Property 13: Create alert → update → fetch → fields match (5 cases)
  - Property 14: Create message → update content → fetch → content matches (5 cases)

- `apps/api/Jobuler.Tests/Application/ScheduleVersionDiscardPropertyTests.cs` (Task 32)
  - Property 15: Create draft → discard → status = Discarded (5 cases)
  - Property 15: Create draft → discard → not in draft list (5 cases)
  - Property 15: Discard Published version → throws `InvalidOperationException` (5 cases)

- `apps/api/Jobuler.Tests/Integration/AdminManagementIntegrationTests.cs` (Task 33)
  - 33.1: `GroupTasks` DbSet exists and can be queried via in-memory EF
  - 33.2: Unique constraint behavior documented (EF in-memory allows duplicates; real constraint is DB-level)
  - 33.3: Valid burden levels accepted by validator; invalid ones rejected
  - 33.4: `TriggerSolverCommand` creates a `ScheduleRun` record with correct fields and enqueues a job
  - 33.5: `PublishVersionCommand` archives the previous published version before publishing the new one

## Key decisions

- **`"null"` is valid JSON**: `JsonDocument.Parse("null")` succeeds. The Property 7 test list excludes `"null"` and uses `"just text"` instead. This is correct behavior — the validator only rejects non-parseable JSON.
- **In-memory DB and unique constraints**: EF Core's in-memory provider does not enforce unique indexes. Test 33.2 documents this explicitly rather than asserting a throw, since the real constraint is enforced at the PostgreSQL level.
- **Group seeding for task tests**: `CreateGroupTaskCommandHandler` checks that the group exists in the space. Tests seed a `Group` entity with a known `groupId` via reflection to bypass the auto-generated ID.
- **`AllowAllPermissions` helper**: Both `RequirePermissionAsync` and `HasPermissionAsync` are stubbed to allow all calls, matching the pattern in `GroupAlertPropertyTests.cs`.

## How it connects
- Tests cover Tasks 1–17 of the spec (domain, application, and API layers)
- `GroupAlertPropertyTests.cs` fix aligns with Task 10 (ownership check removal)
- Integration tests verify the EF model configuration (Task 4) and command handlers (Tasks 6, 16)

## How to run / verify

```bash
dotnet test apps/api/Jobuler.Tests/Jobuler.Tests.csproj -v minimal
```

Expected: **286 tests, 0 failures**.

## What comes next
- Frontend E2E tests for the new admin UI tabs (tasks, constraints, alerts, messages, schedule)
- Real PostgreSQL integration tests for unique constraint and CHECK constraint enforcement

## Git commit

```bash
git add -A && git commit -m "test(admin-management): add unit, property, and integration tests (tasks 28-33)"
```
