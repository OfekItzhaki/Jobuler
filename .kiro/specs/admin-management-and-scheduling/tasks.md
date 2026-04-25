# Tasks: Admin Management and Scheduling

## Task List

- [x] 1. Domain Layer — GroupTask entity
  - [x] 1.1 Create `apps/api/Jobuler.Domain/Tasks/GroupTask.cs` with `Create()`, `Update()`, and `Deactivate()` methods, implementing `AuditableEntity` and `ITenantScoped`
  - [x] 1.2 Add `AllowsDoubleShift` and `UpdatedByUserId` fields; reuse existing `TaskBurdenLevel` enum from `TaskType`

- [x] 2. Domain Layer — GroupAlert Update method
  - [x] 2.1 Add `Update(string title, string body, AlertSeverity severity)` method to `apps/api/Jobuler.Domain/Groups/GroupAlert.cs`

- [x] 3. Domain Layer — ScheduleVersion Discard
  - [x] 3.1 Add `Discarded` value to `ScheduleVersionStatus` enum in `apps/api/Jobuler.Domain/Scheduling/ScheduleVersion.cs`
  - [x] 3.2 Add `Discard()` method to `ScheduleVersion` that throws `InvalidOperationException` if status is not `Draft`

- [x] 4. Infrastructure — GroupTask EF Configuration
  - [x] 4.1 Create `apps/api/Jobuler.Infrastructure/Persistence/Configurations/GroupTaskConfiguration.cs` using Fluent API: map all columns, configure unique index on `(space_id, group_id, name)`, configure CHECK constraint on `burden_level`
  - [x] 4.2 Add `public DbSet<GroupTask> GroupTasks => Set<GroupTask>();` to `AppDbContext`

- [x] 5. DB Migration 014 — tasks table
  - [x] 5.1 Create `infra/migrations/014_group_tasks.sql` with the `tasks` table DDL: all columns, CHECK constraints (`ends_at > starts_at`, `duration_hours > 0`, `required_headcount >= 1`, `burden_level IN (...)`), UNIQUE index on `(space_id, group_id, name)`, RLS policy, and `set_updated_at()` trigger
  - [x] 5.2 Verify migration applies cleanly on top of 013 without touching `task_types` or `task_slots`

- [x] 6. Application Layer — GroupTask Commands and Queries
  - [x] 6.1 Create `apps/api/Jobuler.Application/Tasks/Commands/GroupTaskCommands.cs` with:
    - `CreateGroupTaskCommand` record + `CreateGroupTaskCommandHandler` (permission check: `tasks.manage`, insert entity, return `Guid`)
    - `UpdateGroupTaskCommand` record + `UpdateGroupTaskCommandHandler` (permission check: `tasks.manage`, load entity, call `Update()`, return 204)
    - `DeleteGroupTaskCommand` record + `DeleteGroupTaskCommandHandler` (permission check: `tasks.manage`, load entity, call `Deactivate()`, return 204)
  - [x] 6.2 Create `apps/api/Jobuler.Application/Tasks/Queries/GetGroupTasksQuery.cs` with `GetGroupTasksQuery` record + handler (permission check: `space.view`, return active tasks ordered by `starts_at` ascending, mapped to `GroupTaskDto`)
  - [x] 6.3 Create `apps/api/Jobuler.Application/Tasks/Validators/GroupTaskCommandValidator.cs` with FluentValidation rules: name 1–200 non-blank chars, `ends_at > starts_at`, `duration_hours > 0`, `required_headcount >= 1`, `burden_level` in valid set

- [x] 7. API Layer — Group-scoped TasksController endpoints
  - [x] 7.1 Add route `[Route("spaces/{spaceId:guid}/groups/{groupId:guid}/tasks")]` controller class (or add actions to existing `TasksController`) with:
    - `GET /` → `GetGroupTasksQuery`
    - `POST /` → `CreateGroupTaskCommand`, returns 201 with `{ id }`
    - `PUT /{taskId}` → `UpdateGroupTaskCommand`, returns 204
    - `DELETE /{taskId}` → `DeleteGroupTaskCommand`, returns 204
  - [x] 7.2 Add `CreateGroupTaskRequest` and `UpdateGroupTaskRequest` record types with all task fields

- [x] 8. Application Layer — Constraint Update and Delete Commands
  - [x] 8.1 Create `apps/api/Jobuler.Application/Constraints/Commands/UpdateConstraintCommand.cs` with `UpdateConstraintCommand` record + handler (permission check: `constraints.manage`, load entity, call `ConstraintRule.Update()`, return 204)
  - [x] 8.2 Create `apps/api/Jobuler.Application/Constraints/Commands/DeleteConstraintCommand.cs` with `DeleteConstraintCommand` record + handler (permission check: `constraints.manage`, load entity, call `ConstraintRule.Deactivate()`, return 204)
  - [x] 8.3 Create `apps/api/Jobuler.Application/Constraints/Validators/UpdateConstraintCommandValidator.cs` with FluentValidation rules: `rulePayloadJson` is valid JSON, `effectiveUntil >= effectiveFrom` when both provided

- [x] 9. API Layer — ConstraintsController PUT and DELETE
  - [x] 9.1 Add `PUT /{constraintId}` action to `ConstraintsController` dispatching `UpdateConstraintCommand`, returns 204
  - [x] 9.2 Add `DELETE /{constraintId}` action to `ConstraintsController` dispatching `DeleteConstraintCommand`, returns 204
  - [x] 9.3 Add `UpdateConstraintRequest` record type with `RulePayloadJson`, `EffectiveFrom`, `EffectiveUntil` fields

- [x] 10. Application Layer — Admin Delete Any Group Alert (remove ownership check)
  - [x] 10.1 Remove the `callerPerson` lookup and `alert.CreatedByPersonId != callerPerson.Id` ownership check from `DeleteGroupAlertCommandHandler` in `GroupAlertCommands.cs`; keep only the `people.manage` permission check

- [x] 11. Application Layer — UpdateGroupAlertCommand
  - [x] 11.1 Add `UpdateGroupAlertCommand` record + `UpdateGroupAlertCommandHandler` to `GroupAlertCommands.cs` (permission check: `people.manage`, load alert, call `alert.Update()`, return 204)
  - [x] 11.2 Add `UpdateGroupAlertCommandValidator` with FluentValidation rules: title 1–200 non-blank chars, body 1–2000 non-blank chars, severity in `{info, warning, critical}`

- [x] 12. Application Layer — Admin Delete Any Group Message (add people.manage bypass)
  - [x] 12.1 Update `DeleteGroupMessageCommandHandler` in `GroupMessageCommands.cs` to allow deletion when the caller holds `people.manage` (check via `IPermissionService.HasPermissionAsync`), in addition to the existing author-or-owner check; inject `IPermissionService`

- [x] 13. Application Layer — UpdateGroupMessageCommand
  - [x] 13.1 Add `UpdateGroupMessageCommand` record + `UpdateGroupMessageCommandHandler` to `GroupMessageCommands.cs` (permission check: `people.manage`, load message, call `message.Update(content, message.IsPinned)`, return 204)
  - [x] 13.2 Add `UpdateGroupMessageCommandValidator` with FluentValidation rule: content 1–5000 non-blank chars

- [x] 14. Application Layer — PinGroupMessageCommand
  - [x] 14.1 Add `PinGroupMessageCommand` record + `PinGroupMessageCommandHandler` to `GroupMessageCommands.cs` (permission check: `people.manage`, load message, call `message.Update(message.Content, isPinned)`, return 204)

- [x] 15. API Layer — GroupsController alert and message admin endpoints
  - [x] 15.1 Add `PUT /spaces/{spaceId}/groups/{groupId}/alerts/{alertId}` action to `GroupsController` dispatching `UpdateGroupAlertCommand`, returns 204
  - [x] 15.2 Add `PUT /spaces/{spaceId}/groups/{groupId}/messages/{messageId}` action to `GroupsController` dispatching `UpdateGroupMessageCommand`, returns 204
  - [x] 15.3 Add `PATCH /spaces/{spaceId}/groups/{groupId}/messages/{messageId}/pin` action to `GroupsController` dispatching `PinGroupMessageCommand`, returns 204
  - [x] 15.4 Add `UpdateGroupAlertRequest`, `UpdateGroupMessageRequest`, and `PinGroupMessageRequest` record types

- [x] 16. Application Layer — DiscardVersionCommand
  - [x] 16.1 Create `apps/api/Jobuler.Application/Scheduling/Commands/DiscardVersionCommand.cs` with `DiscardVersionCommand` record + handler (permission check: `schedule.publish`, load version, call `version.Discard()`, save, return 204)

- [x] 17. API Layer — ScheduleVersionsController DELETE (discard)
  - [x] 17.1 Add `DELETE /{versionId}` action to `ScheduleVersionsController` dispatching `DiscardVersionCommand`, returns 204

- [x] 18. Frontend — tasks API client (group-scoped)
  - [x] 18.1 Add `listGroupTasks(spaceId, groupId)` function to `apps/web/lib/api/tasks.ts` calling `GET /spaces/{spaceId}/groups/{groupId}/tasks`
  - [x] 18.2 Add `createGroupTask(spaceId, groupId, data)` function calling `POST /spaces/{spaceId}/groups/{groupId}/tasks`
  - [x] 18.3 Add `updateGroupTask(spaceId, groupId, taskId, data)` function calling `PUT /spaces/{spaceId}/groups/{groupId}/tasks/{taskId}`
  - [x] 18.4 Add `deleteGroupTask(spaceId, groupId, taskId)` function calling `DELETE /spaces/{spaceId}/groups/{groupId}/tasks/{taskId}`

- [x] 19. Frontend — constraints API client (update + delete)
  - [x] 19.1 Add `updateConstraint(spaceId, constraintId, data)` function to `apps/web/lib/api/constraints.ts` calling `PUT /spaces/{spaceId}/constraints/{constraintId}`
  - [x] 19.2 Add `deleteConstraint(spaceId, constraintId)` function calling `DELETE /spaces/{spaceId}/constraints/{constraintId}`

- [x] 20. Frontend — groups API client (alert edit, message edit/pin)
  - [x] 20.1 Add `updateGroupAlert(spaceId, groupId, alertId, data)` function to `apps/web/lib/api/groups.ts` calling `PUT /spaces/{spaceId}/groups/{groupId}/alerts/{alertId}`
  - [x] 20.2 Add `updateGroupMessage(spaceId, groupId, messageId, data)` function calling `PUT /spaces/{spaceId}/groups/{groupId}/messages/{messageId}`
  - [x] 20.3 Add `pinGroupMessage(spaceId, groupId, messageId, isPinned)` function calling `PATCH /spaces/{spaceId}/groups/{groupId}/messages/{messageId}/pin`

- [x] 21. Frontend — GroupDetailPage: משימות tab (task CRUD UI)
  - [x] 21.1 Replace the existing task-types + task-slots sub-tabs in the "משימות" tab with a single unified task list that calls `listGroupTasks`
  - [x] 21.2 Display each task row with: name, time window (starts_at–ends_at in Hebrew locale), duration_hours, required_headcount, and a burden level badge (favorable = green, neutral = grey, disliked = orange, hated = red)
  - [x] 21.3 When `adminGroupId === groupId`, show a "הוסף משימה" button that opens a create form with all task fields (name text input, starts_at/ends_at datetime pickers, duration_hours number input, required_headcount number input, burden_level dropdown in Hebrew, allows_double_shift checkbox, allows_overlap checkbox)
  - [x] 21.4 When `adminGroupId === groupId`, show edit and delete buttons on each task row; edit opens the form pre-populated; delete shows a Hebrew confirmation dialog before calling `deleteGroupTask`
  - [x] 21.5 On create/edit form submit, call `createGroupTask` or `updateGroupTask`, re-fetch the task list on success, display Hebrew error message on failure

- [x] 22. Frontend — GroupDetailPage: אילוצים tab (constraint edit/delete UI)
  - [x] 22.1 When `adminGroupId === groupId`, show edit and delete buttons on each constraint row in the "אילוצים" tab
  - [x] 22.2 Edit button opens a constraint form pre-populated with `rulePayloadJson`, `effectiveFrom`, `effectiveUntil`; on submit calls `updateConstraint`, re-fetches on success, shows Hebrew error on failure
  - [x] 22.3 Delete button shows a Hebrew confirmation dialog; on confirm calls `deleteConstraint`, removes constraint from list on success, shows Hebrew error on failure

- [x] 23. Frontend — GroupDetailPage: התראות tab (alert edit/delete UI)
  - [x] 23.1 When `adminGroupId === groupId`, show edit and delete buttons on ALL alerts (not just own) in the "התראות" tab
  - [x] 23.2 Edit button opens an alert form pre-populated with `title`, `body`, `severity`; on submit calls `updateGroupAlert`, re-fetches on success, shows Hebrew error on failure
  - [x] 23.3 Delete button calls `deleteGroupAlert` directly (no ownership check on frontend); removes alert from list on success, shows Hebrew error on failure

- [x] 24. Frontend — GroupDetailPage: הודעות tab (message edit/delete/pin UI)
  - [x] 24.1 When `adminGroupId === groupId`, show edit, delete, and pin/unpin buttons on ALL messages in the "הודעות" tab
  - [x] 24.2 Edit button opens a message form pre-populated with `content`; on submit calls `updateGroupMessage`, re-fetches on success, shows Hebrew error on failure
  - [x] 24.3 Delete button calls `deleteGroupMessage`; removes message from list on success, shows Hebrew error on failure
  - [x] 24.4 Pin button calls `pinGroupMessage(spaceId, groupId, messageId, true)` for unpinned messages; unpin button calls `pinGroupMessage(..., false)` for pinned messages; update message's pinned state in list on success
  - [x] 24.5 Visually distinguish pinned messages (e.g., pin icon or highlighted border)

- [x] 25. Frontend — GroupDetailPage: הגדרות tab (solver trigger UI)
  - [x] 25.1 When `adminGroupId === groupId`, render a "הפעל סידור" section in the "הגדרות" tab with a "הפעל סידור" button
  - [x] 25.2 On button click, call `POST /spaces/{spaceId}/schedule-runs/trigger` with `{ triggerMode: "standard" }`, store the returned `runId`, transition to polling state
  - [x] 25.3 In polling state, show a spinner and the message "הסידור מחושב..." and disable the "הפעל סידור" button; poll `GET /spaces/{spaceId}/schedule-runs/{runId}` every 3 seconds
  - [x] 25.4 When poll returns `status === "Completed"`, stop polling, show success message "הסידור הושלם! הטיוטה מוכנה לעיון.", and trigger a re-fetch of the schedule data in the "סידור" tab
  - [x] 25.5 When poll returns `status === "Failed"` or `"TimedOut"`, stop polling and display a Hebrew error message describing the failure
  - [x] 25.6 When poll returns HTTP 404, stop polling and display "לא נמצא מידע על ריצת הסידור."
  - [x] 25.7 On trigger API error, display the Hebrew error message and return to idle state

- [x] 26. Frontend — GroupDetailPage: סידור tab (draft display + publish/discard UI)
  - [x] 26.1 After solver completes (triggered from הגדרות tab), re-fetch schedule versions and display the draft version with a "טיוטה" badge or banner
  - [x] 26.2 When a draft version exists and `adminGroupId === groupId`, show "פרסם סידור" and "בטל טיוטה" buttons
  - [x] 26.3 "פרסם סידור" button calls `POST /spaces/{spaceId}/schedule-versions/{versionId}/publish`; on success re-fetch schedule, remove "טיוטה" badge, remove both buttons; disable button while request is in flight
  - [x] 26.4 "בטל טיוטה" button shows a Hebrew confirmation dialog; on confirm calls `DELETE /spaces/{spaceId}/schedule-versions/{versionId}`; on success re-fetch schedule, remove both buttons, show previously published schedule or empty state
  - [x] 26.5 When no draft version exists, do not render "פרסם סידור" or "בטל טיוטה" buttons
  - [x] 26.6 Display Hebrew error messages for publish and discard API failures

- [x] 27. Unit tests — Domain entities
  - [x] 27.1 Test `GroupTask.Create()` produces correct field values for valid inputs
  - [x] 27.2 Test `GroupTask.Deactivate()` sets `IsActive = false` and updates `UpdatedByUserId`
  - [x] 27.3 Test `ScheduleVersion.Discard()` succeeds when status is `Draft`
  - [x] 27.4 Test `ScheduleVersion.Discard()` throws `InvalidOperationException` when status is not `Draft`
  - [x] 27.5 Test `GroupAlert.Update()` trims whitespace from title and body

- [x] 28. Unit tests — Application layer handlers
  - [x] 28.1 Test `DeleteGroupAlertCommandHandler` succeeds for any `people.manage` holder regardless of who created the alert
  - [x] 28.2 Test `DeleteGroupMessageCommandHandler` succeeds when caller holds `people.manage` even if not the author
  - [x] 28.3 Test `PinGroupMessageCommandHandler` sets `IsPinned = true` and `IsPinned = false` correctly
  - [x] 28.4 Test `UpdateConstraintCommandValidator` rejects invalid JSON and rejects `effectiveUntil < effectiveFrom`
  - [x] 28.5 Test `CreateGroupTaskCommandValidator` rejects: empty name, whitespace-only name, name > 200 chars, `ends_at <= starts_at`, `duration_hours <= 0`, `required_headcount < 1`, invalid `burden_level`

- [x] 29. Property-based tests — Task CRUD properties
  - [x] 29.1 Property 1: Generate random valid task inputs → create → list → verify fields match (100+ iterations)
  - [x] 29.2 Property 2: Generate random datetime pairs where `ends_at ≤ starts_at` → create/update → verify 400 (100+ iterations)
  - [x] 29.3 Property 3: Generate random strings not in valid burden_level set → create/update → verify 400 (100+ iterations)
  - [x] 29.4 Property 4: Create task → delete → list → verify absent (100+ iterations)
  - [x] 29.5 Property 5: Create N tasks with random `starts_at` → list → verify ascending order (100+ iterations)

- [x] 30. Property-based tests — Constraint properties
  - [x] 30.1 Property 6: Create constraint → generate random valid update → PUT → GET → verify fields match (100+ iterations)
  - [x] 30.2 Property 7: Generate random non-JSON strings → PUT constraint → verify 400 (100+ iterations)
  - [x] 30.3 Property 8: Generate random date pairs where `until < from` → PUT constraint → verify 400 (100+ iterations)
  - [x] 30.4 Property 9: Create constraint → delete → list → verify absent (100+ iterations)

- [x] 31. Property-based tests — Alert and message admin properties
  - [x] 31.1 Property 10: Create alert as user A → delete as user B (both `people.manage`) → verify 204 and alert gone (100+ iterations)
  - [x] 31.2 Property 11: Create message as user A → delete as user B (`people.manage`) → verify 204 and message gone (100+ iterations)
  - [x] 31.3 Property 12: Create message → pin → unpin → verify `isPinned = false` (100+ iterations)
  - [x] 31.4 Property 13: Create alert → generate random valid update → PUT → GET → verify fields match (100+ iterations)
  - [x] 31.5 Property 14: Create message → generate random valid content → PUT → GET → verify content matches (100+ iterations)

- [x] 32. Property-based tests — Schedule version discard property
  - [x] 32.1 Property 15: Create draft version → DELETE → verify status = Discarded, not in draft list (100+ iterations)

- [x] 33. Integration tests
  - [x] 33.1 Verify migration 014 applies cleanly on top of migration 013
  - [x] 33.2 Verify unique constraint on `(space_id, group_id, name)` is enforced at DB level
  - [x] 33.3 Verify `burden_level` CHECK constraint rejects invalid values at DB level
  - [x] 33.4 Verify `ScheduleRunsController.Trigger` → `GetRun` returns a valid run record with correct `spaceId`
  - [x] 33.5 Verify `ScheduleVersionsController.Publish` archives the previous published version
