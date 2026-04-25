# Step 035 — Group Alert Commands (Application Layer)

## Phase
Phase N — Group Alerts and Phone Feature

## Purpose
Implements the CQRS application layer for group alerts: create, list, and delete. These handlers wire the `GroupAlert` domain entity to the database and enforce permission/membership checks.

## What was built

- `apps/api/Jobuler.Application/Groups/Commands/GroupAlertCommands.cs`
  - `GroupAlertDto` — read model returned by the query handler
  - `CreateGroupAlertCommand` + `CreateGroupAlertCommandValidator` + `CreateGroupAlertCommandHandler` (tasks 5.1)
  - `GetGroupAlertsQuery` + `GetGroupAlertsQueryHandler` (task 5.4)
  - `DeleteGroupAlertCommand` + `DeleteGroupAlertCommandHandler` (task 5.8)

## Key decisions

- Permission guard uses `Permissions.PeopleManage` (string constant from `Jobuler.Domain.Spaces`) for both create and delete, consistent with other group management commands.
- `GetGroupAlerts` checks group membership (not a space permission) — any member can read alerts, only managers can write/delete.
- Delete is restricted to the alert's original creator; space managers who didn't create the alert cannot delete it.
- Severity is validated as a string in FluentValidation and parsed to `AlertSeverity` enum in the handler, matching the domain model.
- `CreatedByDisplayName` falls back to `FullName` when `DisplayName` is null, consistent with the pattern used elsewhere (e.g. `GetScheduleVersionsQuery`).

## How it connects

- Depends on `GroupAlert.Create(...)` in `Jobuler.Domain.Groups` (step 034).
- `AppDbContext.GroupAlerts` DbSet was added in step 034.
- Controllers (task 5.9+) will dispatch these commands/queries via MediatR.
- `IPermissionService` is resolved from DI (implemented in `Jobuler.Infrastructure`).

## How to run / verify

```bash
dotnet build apps/api/Jobuler.Application/Jobuler.Application.csproj --no-restore
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)
```

## What comes next

- Task 5.9: Add alert endpoints to `GroupsController` (POST, GET, DELETE).
- Task 5.10+: Frontend UI for displaying and creating alerts on the group detail page.

## Git commit

```bash
git add -A && git commit -m "feat(group-alerts): add CreateGroupAlert, GetGroupAlerts, DeleteGroupAlert commands"
```
