# Step 036 — Group Alert Controller Endpoints

## Phase
Phase: Group Alerts & Phone — API Layer

## Purpose
Expose the group alert commands and query through HTTP endpoints in `GroupsController`, completing the API surface for creating, listing, and deleting group alerts.

## What was built

- `apps/api/Jobuler.Api/Controllers/GroupsController.cs` — added three new endpoints and one request record:
  - `POST spaces/{spaceId}/groups/{groupId}/alerts` → `CreateGroupAlertCommand`
  - `GET  spaces/{spaceId}/groups/{groupId}/alerts` → `GetGroupAlertsQuery`
  - `DELETE spaces/{spaceId}/groups/{groupId}/alerts/{alertId}` → `DeleteGroupAlertCommand`
  - `CreateAlertRequest(string Title, string Body, string Severity)` record

## Key decisions

- Permission checks are delegated entirely to the Application layer handlers (as per architecture rules) — the controller dispatches commands without an extra `RequirePermissionAsync` call, consistent with how `GetMessages` and `CreateMessage` work.
- `CreatedAtAction(nameof(CreateAlert), new { id })` is used for the POST response to return a proper 201 with a location hint.

## How it connects

- Depends on `CreateGroupAlertCommand`, `GetGroupAlertsQuery`, and `DeleteGroupAlertCommand` defined in `Jobuler.Application/Groups/Commands/GroupAlertCommands.cs` (step 035).
- Sits behind `[Authorize]` inherited from the controller class.

## How to run / verify

```bash
dotnet build --no-restore
# Then hit the endpoints with a valid JWT:
# POST   /spaces/{spaceId}/groups/{groupId}/alerts
# GET    /spaces/{spaceId}/groups/{groupId}/alerts
# DELETE /spaces/{spaceId}/groups/{groupId}/alerts/{alertId}
```

## What comes next

- Frontend components to display and create alerts on the group detail page.

## Git commit

```bash
git add -A && git commit -m "feat(alerts): add alert endpoints to GroupsController"
```
