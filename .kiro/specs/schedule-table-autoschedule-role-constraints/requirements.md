# Requirements Document

## Introduction

This feature set covers three tightly coupled capabilities that together complete the scheduling experience in Jobuler:

**Feature 1 — Schedule Table View**: Replace the current list-based schedule display with a proper two-dimensional table layout (rows = time slots, columns = task names, cells = assigned people). This applies to the group schedule tab (day and week views), the admin schedule page, and any other place a schedule is currently rendered as a list. Monthly and yearly views are removed — they are not needed.

**Feature 2 — Auto-Scheduler Gap Detection**: Enhance the existing `AutoSchedulerService` so it checks coverage day-by-day within the solver horizon (per group). If any day within the horizon is missing coverage, the solver is triggered with the current published schedule as the baseline, so stability weights keep changes minimal.

**Feature 3 — Personal and Role Constraints**: Extend the constraint system to support three scope levels — group-level, role-level (all members holding a given `SpaceRole`), and individual-level — each with hard, soft, and emergency severities. The solver already consumes constraints from the `constraint_rules` table; this feature adds the UI and backend validation to manage all three scope levels, and introduces group-level constraints as a first-class concept in the UI.

The three features are tightly coupled: constraints feed the solver, the solver fills gaps detected by the auto-scheduler, and the resulting schedule is displayed in the new table view.

---

## Glossary

- **System**: The Jobuler backend (ASP.NET Core API + Application + Domain + Infrastructure layers).
- **UI**: The Jobuler Next.js frontend.
- **ScheduleTable**: The new two-dimensional schedule display component. Rows are time slots (start–end), columns are task names, cells contain the names of people assigned to that task at that time.
- **ScheduleTab**: The "סידור" tab on the `GroupDetailPage`. Currently shows a list view. This feature replaces it with the new `ScheduleTable` component.
- **AdminSchedulePage**: The `/admin/schedule` page. Currently uses the existing `ScheduleTable` component (list-based). This feature replaces it with the new two-dimensional table.
- **SolverHorizonDays**: The per-group setting (3–7 days) that defines how far ahead the solver should schedule.
- **AutoSchedulerService**: The existing `BackgroundService` in `Jobuler.Infrastructure.Scheduling` that periodically checks whether a published schedule covers the horizon and triggers the solver if not.
- **Gap**: A task slot within the solver horizon for which no published assignment exists. The schedule is considered incomplete if any task slot across any day in the horizon has no published assignment. The auto-scheduler triggers one solver run per group to fill all gaps at once — not one run per gap day.
- **Baseline**: The current published `ScheduleVersion` passed to the solver as `baselineVersionId`. Stability weights in the solver payload keep changes to the baseline minimal.
- **ConstraintRule**: The domain entity in `constraint_rules`. Fields: `space_id`, `scope_type` (`Person`/`Role`/`Group`/`TaskType`/`Space`), `scope_id`, `severity` (`Hard`/`Soft`/`Emergency`), `rule_type`, `rule_payload_json`, `effective_from`, `effective_until`, `is_active`.
- **ConstraintScopeType**: The enum on `ConstraintRule`. `Group` means the constraint applies to all members of a group. `Role` means it applies to all members holding a given `SpaceRole`. `Person` means it applies to one specific individual.
- **ConstraintSeverity**:
  - `Hard` — must be satisfied at all times; solver fails if it cannot satisfy a hard constraint, unless an emergency constraint overrides it.
  - `Soft` — the solver treats it as hard first; only relaxes it if it is the sole blocker preventing schedule completion.
  - `Emergency` — overrides all other constraints including hard ones; used for urgent situations.
- **SpaceRole**: A dynamic operational role within a **group** (e.g., Soldier, Medic, Commander). Stored in `space_roles` with a `group_id` column so each group has its own independent role set. Created and managed by the group owner/admin. Roles are not shared across groups.
- **PersonRoleAssignment**: The join table linking a `Person` to a `SpaceRole` within a group. Stored in `person_role_assignments` with a `group_id` column.
- **Group_Admin**: A user who holds the `constraints.manage` permission for the space (backend) / whose `adminGroupId` equals the current `groupId` (frontend).
- **Registered_Member**: A `Person` whose `linked_user_id` is not null and whose `invitation_status` is `"accepted"`.
- **IPermissionService**: The Application-layer interface used for all permission checks.
- **ISolverPayloadNormalizer**: The interface implemented by `SolverPayloadNormalizer` that builds the `SolverInputDto` sent to the solver.
- **Permissions.ConstraintsManage**: The `constraints.manage` permission key — required for all constraint write operations.
- **Permissions.SchedulePublish**: The `schedule.publish` permission key — required to publish, discard, or re-trigger the solver. Any member holding this permission is considered a schedule admin for the group. `schedule.publish` and `schedule.recalculate` are treated as equivalent for notification and trigger purposes.
- **no_task_type_restriction**: Constraint rule type that prevents a person or role from being assigned to a specific task type. Payload: `{ "task_type_id": "..." }`.
- **min_rest_hours**: Constraint rule type that enforces a minimum rest gap between consecutive assignments for a person. Payload: `{ "hours": N }`.
- **no_consecutive_burden**: Constraint rule type that prevents the same burden-level task from being assigned on consecutive days. Payload: `{ "burden_level": "disliked" | "hated" }`.

---

## Requirements

### Requirement 1: Schedule Table View — Day View

**User Story:** As a group member, I want to see the daily schedule as a two-dimensional table with task names as column headers and time slots as row headers, so that I can quickly understand who is doing what at each time.

#### Acceptance Criteria

1. WHEN the "סידור" tab is active and the day view is selected, THE ScheduleTab SHALL render a two-dimensional table where each column header is a unique task name and each row header is a time slot expressed as "HH:MM – HH:MM".
2. WHEN multiple people are assigned to the same task slot, THE ScheduleTable SHALL display all their names in the same cell, separated by line breaks or comma-separated.
3. WHEN a task slot has no assigned person, THE ScheduleTable SHALL render the cell as empty (not an error state).
4. WHEN the schedule data is loading, THE ScheduleTab SHALL display a loading spinner in place of the table.
5. IF the schedule data fetch returns an error, THEN THE ScheduleTab SHALL display a Hebrew error message in place of the table.
6. WHEN there are no assignments for the selected day, THE ScheduleTab SHALL display a Hebrew empty-state message in place of the table.
7. THE ScheduleTable SHALL be horizontally scrollable when the number of task columns exceeds the viewport width.
8. THE ScheduleTable SHALL highlight the column of the task that the currently logged-in user is assigned to, if any.

---

### Requirement 2: Schedule Table View — Week View

**User Story:** As a group member, I want to navigate the weekly schedule using day-name tabs (Sunday through Saturday), so that I can quickly jump to any day's table without scrolling through a list.

#### Acceptance Criteria

1. WHEN the week view is selected in the ScheduleTab, THE UI SHALL display seven day-name buttons (Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday) as tabs above the table.
2. WHEN a day-name tab is clicked, THE ScheduleTab SHALL display the two-dimensional ScheduleTable for that specific day.
3. THE UI SHALL highlight the tab corresponding to today's date.
4. WHEN a day within the week has no assignments, THE ScheduleTab SHALL display a Hebrew empty-state message for that day's table.
5. THE UI SHALL remove the monthly and yearly schedule view options — they SHALL NOT appear anywhere in the application.
6. WHEN the week view is first loaded, THE ScheduleTab SHALL default to showing today's day tab.

---

### Requirement 3: Admin Schedule Page — Table View

**User Story:** As a space admin, I want the admin schedule page to display assignments in the same two-dimensional table format, so that I have a consistent view across the application.

#### Acceptance Criteria

1. WHEN a schedule version is selected on the admin schedule page, THE AdminSchedulePage SHALL render the assignments using the new two-dimensional ScheduleTable component.
2. THE ScheduleTable on the admin page SHALL support filtering by date so that the admin can view one day at a time.
3. THE AdminSchedulePage SHALL retain all existing functionality: version list sidebar, publish/rollback/discard actions, diff summary card, CSV/PDF export, infeasibility banner, and solver trigger buttons.
4. THE UI SHALL remove any list-based schedule rendering from the admin schedule page — the two-dimensional table is the only display format.

---

### Requirement 4: Auto-Scheduler Gap Detection

**User Story:** As a group owner, I want the schedule to always be fully covered for every day within the horizon I configured, with no holes at any point during the day, so that continuous coverage is guaranteed without manual intervention.

#### Acceptance Criteria

1. WHEN the `AutoSchedulerService` runs its periodic check for a group, THE System SHALL evaluate every task slot across all calendar days from today through `today + SolverHorizonDays - 1` (inclusive) — where `SolverHorizonDays` is the per-group setting configured by the group owner — to determine whether the schedule is fully covered.
2. WHEN any task slot within the horizon has no published assignment, THE System SHALL consider the schedule incomplete and trigger the solver once for that group — covering the full horizon in a single run, not one run per gap day.
3. THE System SHALL treat the schedule as incomplete if there is any gap at any point during the day across any of the horizon days — not just at the day boundary. Every task slot that should exist must have a published assignment.
4. WHEN the solver is triggered by gap detection, THE System SHALL pass the current published `ScheduleVersion.Id` as `baselineVersionId` in the `TriggerSolverCommand`, so that stability weights keep already-covered slots unchanged and only fill the missing ones.
5. WHEN no published schedule exists for the group, THE System SHALL trigger the solver with `baselineVersionId = null` (existing behaviour is preserved).
6. WHILE an active solver run (status `Queued` or `Running`) exists for the space, THE System SHALL NOT trigger another run — the existing skip logic is preserved.
7. WHILE an unreviewed draft version exists for the space, THE System SHALL NOT trigger another run — the existing skip logic is preserved.
8. WHEN a solver run has failed within the last 2 hours for the space, THE System SHALL NOT trigger another run — the existing back-off logic is preserved.
9. THE System SHALL log the specific gap slots detected (day and task slot) at `Information` level before triggering the solver.

---

### Requirement 5: Group-Level Constraints — UI

**User Story:** As a group admin, I want to view and manage constraints that apply to all members of the group, so that I can define group-wide scheduling rules in one place.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and the user is a group admin, THE UI SHALL display a "אילוצי קבוצה" (Group Constraints) section listing all active constraints with `scope_type = 'Group'` and `scope_id = groupId`.
2. THE UI SHALL display each group constraint row with: severity badge, rule type label, formatted payload summary, and effective date range.
3. WHEN the admin clicks "אילוץ קבוצה חדש" (New Group Constraint), THE UI SHALL open a create form with: rule type selector, severity selector (Hard / Soft / Emergency), payload editor, effective from (optional date), effective until (optional date).
4. WHEN the admin submits the group constraint form, THE UI SHALL call `POST /spaces/{spaceId}/constraints` with `scopeType: "group"`, `scopeId: <groupId>`, and the remaining fields.
5. WHEN the admin clicks the edit button on a group constraint row, THE UI SHALL open the edit form pre-populated with the constraint's current values.
6. WHEN the admin clicks the delete button on a group constraint row, THE UI SHALL display a Hebrew confirmation dialog before calling `DELETE /spaces/{spaceId}/constraints/{constraintId}`.
7. IF any constraint API call returns an error, THEN THE UI SHALL display the error message in Hebrew below the relevant form.
8. WHEN a create, update, or delete call succeeds, THE UI SHALL re-fetch the constraints list without a full page reload.

---

### Requirement 6: Role-Level Constraints — UI

**User Story:** As a group admin, I want to add constraints that apply to all members holding a specific SpaceRole, so that I can define role-wide rules without repeating them per person.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and the user is a group admin, THE UI SHALL display a "אילוצי תפקיד" (Role Constraints) section listing all active constraints with `scope_type = 'Role'`.
2. THE UI SHALL display each role constraint row with: the role name (resolved from `GET /spaces/{spaceId}/roles`), severity badge, rule type label, formatted payload summary, and effective date range.
3. WHEN the admin clicks "אילוץ תפקיד חדש" (New Role Constraint), THE UI SHALL open a create form with: role selector (dropdown of active space roles), rule type selector, severity selector (Hard / Soft / Emergency), payload editor, effective from (optional date), effective until (optional date).
4. THE UI SHALL populate the role selector by fetching `GET /spaces/{spaceId}/roles` and filtering to active roles only.
5. WHEN the admin submits the role constraint form, THE UI SHALL call `POST /spaces/{spaceId}/constraints` with `scopeType: "role"`, `scopeId: <roleId>`, and the remaining fields.
6. WHEN the admin clicks the edit button on a role constraint row, THE UI SHALL open the edit form pre-populated with the constraint's current values.
7. WHEN the admin clicks the delete button on a role constraint row, THE UI SHALL display a Hebrew confirmation dialog before calling `DELETE /spaces/{spaceId}/constraints/{constraintId}`.
8. IF any constraint API call returns an error, THEN THE UI SHALL display the error message in Hebrew below the relevant form.
9. WHEN a create, update, or delete call succeeds, THE UI SHALL re-fetch the constraints list without a full page reload.

---

### Requirement 7: Individual-Level Constraints — UI

**User Story:** As a group admin, I want to add constraints for a specific registered member, so that the solver respects that individual's limitations or preferences.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and the user is a group admin, THE UI SHALL display a "אילוצים אישיים" (Personal Constraints) section listing all active constraints with `scope_type = 'Person'`.
2. THE UI SHALL display each personal constraint row with: the person's display name (resolved from the loaded members list), severity badge, rule type label, formatted payload summary, and effective date range.
3. WHEN the admin clicks "אילוץ אישי חדש" (New Personal Constraint), THE UI SHALL open a create form with: person selector (dropdown of registered group members only — those with `linkedUserId` not null), rule type selector, severity selector (Hard / Soft / Emergency), payload editor, effective from (optional date), effective until (optional date).
4. WHEN the admin submits the personal constraint form, THE UI SHALL call `POST /spaces/{spaceId}/constraints` with `scopeType: "person"`, `scopeId: <personId>`, and the remaining fields.
5. WHEN the admin clicks the edit button on a personal constraint row, THE UI SHALL open the edit form pre-populated with the constraint's current values.
6. WHEN the admin clicks the delete button on a personal constraint row, THE UI SHALL display a Hebrew confirmation dialog before calling `DELETE /spaces/{spaceId}/constraints/{constraintId}`.
7. IF any constraint API call returns an error, THEN THE UI SHALL display the error message in Hebrew below the relevant form.
8. WHEN a create, update, or delete call succeeds, THE UI SHALL re-fetch the constraints list without a full page reload.

---

### Requirement 8: Role-Level Constraint — Backend Validation

**User Story:** As a group admin, I want the system to reject role constraint creation if the target role does not exist or is inactive in the space, so that orphaned constraints are never created.

#### Acceptance Criteria

1. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "role"`, THE System SHALL validate that `scope_id` is a non-null, non-empty GUID; IF `scope_id` is null or empty, THEN THE System SHALL return HTTP 400.
2. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "role"`, THE System SHALL verify that a `SpaceRole` record with `id = scope_id`, `space_id = spaceId`, and `is_active = true` exists; IF the role does not exist or is inactive, THEN THE System SHALL return HTTP 404 with the message "Role not found in this space."
3. THE System SHALL perform this check in `CreateConstraintCommandHandler` after the permission check and before inserting the entity.

---

### Requirement 9: Individual-Level Constraint — Backend Validation

**User Story:** As a group admin, I want the system to reject personal constraint creation if the target person does not exist in the space or is unregistered, so that constraints are only applied to valid, confirmed members.

#### Acceptance Criteria

1. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "person"`, THE System SHALL validate that `scope_id` is a non-null, non-empty GUID; IF `scope_id` is null or empty, THEN THE System SHALL return HTTP 400.
2. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "person"`, THE System SHALL verify that a `Person` record with `id = scope_id` and `space_id = spaceId` exists; IF the person does not exist, THEN THE System SHALL return HTTP 404 with the message "Person not found in this space."
3. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "person"` and the target person's `linked_user_id` is null or `invitation_status` is not `"accepted"`, THE System SHALL return HTTP 422 with the message "Personal constraints can only be applied to registered members."
4. THE System SHALL perform these checks in `CreateConstraintCommandHandler` in the order: permission check → person existence → registered-member guard → insert.

---

### Requirement 10: Solver Applies All Three Constraint Scope Levels

**User Story:** As a group admin, I want group-level, role-level, and individual-level constraints to all be respected by the solver when generating a schedule, so that every rule is honoured regardless of its scope.

#### Acceptance Criteria

1. WHEN the solver payload is built, THE ISolverPayloadNormalizer SHALL include all active group constraints (`scope_type = 'Group'`) in the appropriate hard, soft, or emergency constraint list, with `scope_type = "group"` and `scope_id` set to the group's ID.
2. WHEN the solver payload is built, THE ISolverPayloadNormalizer SHALL include all active role constraints (`scope_type = 'Role'`) in the appropriate hard, soft, or emergency constraint list, with `scope_type = "role"` and `scope_id` set to the role's ID.
3. WHEN the solver payload is built, THE ISolverPayloadNormalizer SHALL include all active personal constraints (`scope_type = 'Person'`) in the appropriate hard, soft, or emergency constraint list, with `scope_type = "person"` and `scope_id` set to the person's ID.
4. THE ISolverPayloadNormalizer SHALL filter all constraints by `effective_from` and `effective_until` relative to the solver horizon start date — constraints outside the effective window SHALL be excluded from the payload.
5. THE ISolverPayloadNormalizer SHALL pass all three scope levels through the existing constraint pipeline — no separate normalizer is required.

---

### Requirement 11: Solver Failure Notifications and Re-trigger Permission

**User Story:** As a group admin, I want to be notified when the solver cannot create a schedule, and I want anyone who can publish a schedule to also be able to re-run the solver, so that I can take corrective action without needing a separate permission.

#### Acceptance Criteria

1. WHEN a solver run completes with `feasible = false`, THE System SHALL send a notification to the user who triggered the run (identified by `ScheduleRun.RequestedByUserId`), if that user is not null.
2. WHEN a solver run completes with `feasible = false` and `RequestedByUserId` is null (auto-triggered run), THE System SHALL send a notification to all members of the space who hold the `schedule.publish` permission.
3. THE notification message SHALL include the group name and a summary of why the schedule could not be created (from `SummaryJson.conflict_details` if available).
4. WHEN a solver run completes successfully (`feasible = true`), THE System SHALL send a notification only to the user who triggered the run, if that user is not null — no broadcast to all admins.
5. THE System SHALL use the existing `INotificationService` for all notification delivery — no new notification infrastructure is required.
6. ANY user holding the `schedule.publish` permission SHALL also be permitted to manually re-trigger the solver (recalculate/re-run) — no separate `schedule.recalculate` permission is required. The two actions are treated as equivalent.

---

### Requirement 12: SpaceRole Management — CRUD

**User Story:** As a group owner or admin, I want to create, rename, and deactivate SpaceRoles, so that I can define the operational roles relevant to my group.

#### Acceptance Criteria

1. WHEN an admin calls `POST /spaces/{spaceId}/groups/{groupId}/roles` with a name and optional description, THE System SHALL create a new active `SpaceRole` scoped to that group and return its ID; the caller MUST hold the `people.manage` permission.
2. WHEN an admin calls `PUT /spaces/{spaceId}/groups/{groupId}/roles/{roleId}` with an updated name or description, THE System SHALL update the role's name and description; the caller MUST hold the `people.manage` permission.
3. WHEN an admin calls `DELETE /spaces/{spaceId}/groups/{groupId}/roles/{roleId}`, THE System SHALL deactivate the role (`is_active = false`) rather than hard-deleting it; the caller MUST hold the `people.manage` permission.
4. IF a role is deactivated and active role constraints reference it, THE System SHALL NOT automatically deactivate those constraints — they remain active until explicitly deleted by an admin.
5. THE UI SHALL display a "תפקידים" (Roles) management section in the group settings tab, allowing admins to add, rename, and deactivate roles for that group only.
6. WHEN a role is deactivated, THE UI SHALL remove it from the role selector in the role constraint create form.
7. Roles from one group SHALL NOT be visible or selectable in another group's constraint forms.

---

### Requirement 13: Constraint Effective-Date Filtering in Solver

**User Story:** As a group admin, I want constraints with effective date ranges to be automatically excluded from solver runs that fall outside those ranges, so that time-limited rules are applied only when relevant.

#### Acceptance Criteria

1. WHEN the solver payload is built for a horizon starting on date D, THE ISolverPayloadNormalizer SHALL exclude any constraint where `effective_until < D`.
2. WHEN the solver payload is built for a horizon starting on date D, THE ISolverPayloadNormalizer SHALL exclude any constraint where `effective_from > D + SolverHorizonDays - 1` (the constraint has not yet started within the horizon).
3. WHEN a constraint has `effective_from = null` and `effective_until = null`, THE ISolverPayloadNormalizer SHALL include it in every solver payload — it is always active.
4. THE ISolverPayloadNormalizer SHALL apply this filtering uniformly to group, role, and personal constraints.

---

### Requirement 14: Manual Override Assignments

**User Story:** As a group admin, I want to manually override a specific assignment in a published or draft schedule, so that I can correct the solver's output without re-running the full solver.

#### Acceptance Criteria

1. WHEN an admin opens the schedule table on the admin schedule page, THE UI SHALL allow clicking on any cell to open an override modal for that slot.
2. THE override modal SHALL display the current assignee(s) for the slot and a person selector showing all eligible group members.
3. WHEN the admin selects a different person and confirms, THE System SHALL create a new draft version (if one does not already exist) with the override applied — the original published version is never mutated.
4. WHEN the admin removes an assignee from a slot, THE System SHALL record the slot as explicitly unassigned in the draft version.
5. THE System SHALL require the `schedule.publish` permission to perform manual overrides.
6. WHEN a manual override is saved, THE UI SHALL reflect the change immediately in the schedule table without a full page reload.
7. THE System SHALL record manual overrides in the audit log with: actor, slot ID, previous assignee(s), new assignee(s), timestamp.
8. WHEN a draft with manual overrides is published, THE System SHALL treat the overridden assignments as fixed — the solver SHALL NOT reassign those slots if re-triggered.

---

### Requirement 15: Live Person Status Panel

**User Story:** As a group admin or member, I want to see a real-time panel showing who is currently on mission, where they are, and who is at home, so that I have situational awareness of the group at any moment.

#### Acceptance Criteria

1. WHEN a group member opens the group detail page, THE UI SHALL display a "סטטוס נוכחי" (Current Status) tab or panel showing the live status of all group members.
2. THE panel SHALL display each member with their current presence state: `on_mission` (with task name and location if available), `at_home`, `blocked` (unavailable), or `free_in_base` (available).
3. THE panel SHALL update in near-real-time — polling every 30 seconds or using a WebSocket/SSE connection if available.
4. WHEN a member's status is `on_mission`, THE UI SHALL display the task name and the slot's end time so observers know when they will be free.
5. THE panel SHALL be read-only for regular members; only admins can change a member's presence state.
6. THE System SHALL derive the current status from the combination of: active `presence_windows` records (manual overrides) and the currently published schedule (assignment-based status).
7. WHEN a published assignment is active right now (current time falls within the slot's `starts_at`–`ends_at`), THE System SHALL treat the assigned person as `on_mission` for that slot, unless a manual `presence_window` override exists.
8. THE System SHALL expose a `GET /spaces/{spaceId}/groups/{groupId}/live-status` endpoint returning the current status for all group members.
9. THE UI SHALL display the panel in Hebrew, with status labels and last-updated timestamp.
