# Step 044 — Group Alert Property Tests

## Phase
Phase: Group Alerts and Phone — Backend Property Tests

## Purpose
Adds parameterized property tests for the group alerts feature and phone number DTO fidelity. These tests verify that the application layer behaves correctly across a wide range of inputs without relying on FsCheck — instead using xUnit `[Theory]` + `[InlineData]` and loops to cover the same property space.

## What was built

### Files created

- **`apps/api/Jobuler.Tests/Application/GroupAlertPropertyTests.cs`**
  Covers tasks 1.2, 5.2, 5.3, 5.5, 5.6, 5.7, 5.9, 5.10 from the spec:

  | Property | Task | What is tested |
  |----------|------|----------------|
  | 1 — Phone number DTO fidelity | 1.2 | `GroupMemberDto.PhoneNumber` matches the seeded `people.phone_number` value, including `null` |
  | 3 — Alert creation round-trip | 5.2 | `CreateGroupAlertCommand` → `GetGroupAlertsQuery` returns matching title/body/severity |
  | 4 — Rejects invalid inputs | 5.3 | Validator rejects blank/whitespace title or body, titles > 200 chars, bodies > 2000 chars; handler rejects invalid severity strings |
  | 5 — Ordered newest-first | 5.5 | `GetGroupAlertsQuery` returns alerts in descending `CreatedAt` order |
  | 6 — Tenant isolation | 5.6 | Alerts in space A never appear in space B queries, even when `groupId` is shared |
  | 7 — Non-members cannot read | 5.7 | `GetGroupAlertsQuery` throws `UnauthorizedAccessException` for users not in the group |
  | 8 — Delete removes own alerts | 5.9 | `DeleteGroupAlertCommand` removes the target alert and leaves others intact |
  | 9 — Cross-owner deletion rejected | 5.10 | `DeleteGroupAlertCommand` throws `UnauthorizedAccessException` when caller ≠ creator |

## Key decisions

- **No FsCheck** — parameterized `[Theory]` + `[InlineData]` and small loops cover the same property space with deterministic, readable test cases.
- **InMemory EF** — each test gets a fresh `Guid.NewGuid()` database name so tests are fully isolated.
- **NSubstitute for `IPermissionService`** — `AllowAllPermissions()` helper stubs `RequirePermissionAsync` to return `Task.CompletedTask`, keeping tests focused on business logic rather than permission wiring.
- **Handlers called directly** — tests bypass the MediatR pipeline and call handlers directly. Validator tests call `CreateGroupAlertCommandValidator.Validate()` directly, matching how `ValidationBehavior` would invoke it.
- **`SeedPersonAndMembership` helper** — seeds a `Person` linked to a `userId` and a `GroupMembership` in one call, keeping test setup concise.
- **Property 5 timestamp override** — uses EF entry property access to set deterministic `CreatedAt` values so ordering tests are not subject to clock resolution.

## How it connects

- Tests exercise `GetGroupMembersQueryHandler` (task 1.1), `CreateGroupAlertCommandHandler`, `GetGroupAlertsQueryHandler`, and `DeleteGroupAlertCommandHandler` (tasks 5.1, 5.4, 5.8).
- Validates `GroupAlert.Create` domain factory and `AlertSeverity` enum.
- Validates `CreateGroupAlertCommandValidator` FluentValidation rules.
- Uses `AppDbContext` with InMemory provider — same pattern as `CreateSpaceCommandTests.cs`.

## How to run / verify

```bash
dotnet test apps/api/Jobuler.Tests/Jobuler.Tests.csproj --no-build -v n
```

Expected: all tests pass (92 total as of this step, 0 failures).

To run only the new tests:

```bash
dotnet test apps/api/Jobuler.Tests/Jobuler.Tests.csproj --no-build --filter "FullyQualifiedName~GroupAlertPropertyTests"
```

## What comes next

- Task 2.2 — frontend property test for phone number rendering (fast-check)
- Task 7.3 — frontend property test for severity badge color
- Task 7.5 — frontend property test for delete buttons on own alerts only
- Tasks 11.2–11.9 — optional Forgot Password property tests

## Git commit

```bash
git add -A && git commit -m "test(group-alerts): add property tests for alerts and phone DTO (tasks 1.2, 5.2-5.10)"
```
