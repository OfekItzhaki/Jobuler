# Requirements Document

## Introduction

This feature exposes the personal and role-based constraint capabilities that already exist in the backend domain (`ConstraintScopeType.Person` and `ConstraintScopeType.Role`) but are not yet surfaced in the UI.

**Part 1 — Personal Constraints**: A group admin can add hard or soft scheduling constraints for a specific registered member of the group (a person whose `linked_user_id` is not null and whose `invitation_status` is `"accepted"`). These constraints follow the same shape as group constraints — rule type, severity, payload, and optional effective date range.

**Part 2 — Role-Based Constraints**: A group admin can add hard or soft scheduling constraints that apply to all members who hold a given space role (e.g., "Admin" or "Member"). This allows the admin to define rules that apply uniformly to everyone in a role without repeating them per person.

**Future extension note**: The owner will later be able to define custom member roles and assign them. This feature is designed so that role constraints work against the existing `space_roles` / `person_role_assignments` tables and will naturally extend when new roles are introduced.

---

## Glossary

- **System**: The Jobuler backend (ASP.NET Core API + Application + Domain + Infrastructure layers).
- **UI**: The Jobuler Next.js frontend.
- **ConstraintRule**: The domain entity in the `constraint_rules` table. Fields: `space_id`, `scope_type` (`Person`/`Role`/`Group`/`TaskType`/`Space`), `scope_id`, `severity` (`Hard`/`Soft`/`Emergency`), `rule_type`, `rule_payload_json`, `effective_from`, `effective_until`, `is_active`.
- **ConstraintScopeType**: The enum on `ConstraintRule`. `Person` means the constraint targets one specific person. `Role` means it targets all people who hold a given space role.
- **Registered_Member**: A `Person` record whose `linked_user_id` is not null and whose `invitation_status` is `"accepted"`. Only registered members may have personal constraints.
- **Group_Admin**: A user whose `adminGroupId` in `AuthStore` equals the current `groupId` (frontend) / a user with the `constraints.manage` permission scoped to the group's space (backend).
- **IPermissionService**: The Application-layer interface used for all permission checks.
- **SpaceRole**: A dynamic operational role within a space (e.g., Soldier, Medic, Admin, Member). Stored in the `space_roles` table.
- **PersonRoleAssignment**: The join table linking a `Person` to a `SpaceRole` within a space. Stored in `person_role_assignments`.
- **GroupMembership**: The join table linking a `Person` to a `Group`. Stored in `group_memberships`.
- **ConstraintsTab**: The "אילוצים" tab on the `GroupDetailPage`. Currently shows only group-scoped constraints. This feature extends it to show and manage personal and role constraints as well.
- **Permissions.ConstraintsManage**: The `constraints.manage` permission key — required for all constraint write operations.
- **Permissions.SpaceAdminMode**: The `space.admin_mode` permission key — required to read constraints.
- **no_task_type_restriction**: The primary per-person rule type already supported by the solver. Payload: `{ "task_type_id": "..." }`.

---

## Requirements

### Requirement 1: List Personal and Role Constraints in the Constraints Tab

**User Story:** As a group admin, I want to see personal and role-based constraints alongside group constraints in the Constraints tab, so that I have a complete picture of all scheduling rules in one place.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and `adminGroupId` equals `groupId`, THE UI SHALL display all active constraints for the current space, grouped into three sections: "אילוצי קבוצה" (Group), "אילוצים אישיים" (Personal), and "אילוצי תפקיד" (Role).
2. THE UI SHALL display each personal constraint row with: the person's display name (or full name), severity badge, rule type label, formatted payload summary, and effective date range.
3. THE UI SHALL display each role constraint row with: the role name, severity badge, rule type label, formatted payload summary, and effective date range.
4. WHEN a personal constraint's `scope_id` does not match any member in the current group, THE UI SHALL still display the constraint row but show the `scope_id` as a fallback label.
5. THE System SHALL return personal constraints (`scope_type = 'Person'`) and role constraints (`scope_type = 'Role'`) from the existing `GET /spaces/{spaceId}/constraints` endpoint — no new endpoint is required.
6. THE UI SHALL resolve person names by cross-referencing the `scope_id` against the already-loaded `members` list.
7. THE UI SHALL resolve role names by fetching `GET /spaces/{spaceId}/roles` when the constraints tab is first activated.

---

### Requirement 2: Create a Personal Constraint

**User Story:** As a group admin, I want to add a hard or soft constraint for a specific registered member of the group, so that the solver respects that individual's limitations or preferences.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and `adminGroupId` equals `groupId`, THE UI SHALL display an "אילוץ אישי חדש" (New Personal Constraint) button in the Personal section.
2. WHEN the admin clicks "אילוץ אישי חדש", THE UI SHALL open a create form with the following fields: person selector (dropdown of registered group members only), rule type selector, severity selector (Hard / Soft), payload editor, effective from (optional date), effective until (optional date).
3. THE UI SHALL populate the person selector only with members whose `linkedUserId` is not null (registered members).
4. WHEN the admin submits the form, THE UI SHALL call `POST /spaces/{spaceId}/constraints` with `scopeType: "person"`, `scopeId: <personId>`, and the remaining fields.
5. THE System SHALL accept `scopeType = "person"` in `POST /spaces/{spaceId}/constraints`, requiring `[Authorize]` and the `constraints.manage` permission — this is already enforced by the existing `ConstraintsController`.
6. THE System SHALL validate that `scope_id` is a non-null, non-empty GUID when `scope_type = "person"`; IF `scope_id` is null or empty, THEN THE System SHALL return HTTP 400.
7. THE System SHALL validate that the person identified by `scope_id` belongs to the same `space_id`; IF the person does not exist in the space, THEN THE System SHALL return HTTP 404.
8. IF the create API call returns an error, THEN THE UI SHALL display the error message in Hebrew below the form.
9. WHEN the create call succeeds, THE UI SHALL close the form and re-fetch the constraints list.

---

### Requirement 3: Create a Role-Based Constraint

**User Story:** As a group admin, I want to add a hard or soft constraint that applies to all members holding a specific space role, so that I can define rules for an entire role category without repeating them per person.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and `adminGroupId` equals `groupId`, THE UI SHALL display an "אילוץ תפקיד חדש" (New Role Constraint) button in the Role section.
2. WHEN the admin clicks "אילוץ תפקיד חדש", THE UI SHALL open a create form with the following fields: role selector (dropdown of active space roles), rule type selector, severity selector (Hard / Soft), payload editor, effective from (optional date), effective until (optional date).
3. THE UI SHALL populate the role selector by fetching `GET /spaces/{spaceId}/roles` and filtering to active roles only.
4. WHEN the admin submits the form, THE UI SHALL call `POST /spaces/{spaceId}/constraints` with `scopeType: "role"`, `scopeId: <roleId>`, and the remaining fields.
5. THE System SHALL accept `scopeType = "role"` in `POST /spaces/{spaceId}/constraints`, requiring `[Authorize]` and the `constraints.manage` permission — this is already enforced by the existing `ConstraintsController`.
6. THE System SHALL validate that `scope_id` is a non-null, non-empty GUID when `scope_type = "role"`; IF `scope_id` is null or empty, THEN THE System SHALL return HTTP 400.
7. THE System SHALL validate that the role identified by `scope_id` belongs to the same `space_id` and is active; IF the role does not exist or is inactive, THEN THE System SHALL return HTTP 404.
8. IF the create API call returns an error, THEN THE UI SHALL display the error message in Hebrew below the form.
9. WHEN the create call succeeds, THE UI SHALL close the form and re-fetch the constraints list.

---

### Requirement 4: Edit a Personal or Role Constraint

**User Story:** As a group admin, I want to edit the payload, severity, and effective dates of a personal or role constraint, so that I can update rules without deleting and recreating them.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and `adminGroupId` equals `groupId`, THE UI SHALL display an edit button on each personal and role constraint row.
2. WHEN the admin clicks the edit button, THE UI SHALL open the constraint edit form pre-populated with the constraint's current severity, payload, effective from, and effective until values.
3. THE UI SHALL NOT allow changing the `scope_type`, `scope_id`, or `rule_type` during an edit — these fields are read-only in the edit form.
4. WHEN the admin submits the edit form, THE UI SHALL call `PUT /spaces/{spaceId}/constraints/{constraintId}` with the updated `severity`, `rulePayloadJson`, `effectiveFrom`, and `effectiveUntil`.
5. THE System SHALL accept updates to personal and role constraints via the existing `PUT /spaces/{spaceId}/constraints/{constraintId}` endpoint — no new endpoint is required.
6. IF the update API call returns an error, THEN THE UI SHALL display the error message in Hebrew below the form.
7. WHEN the update call succeeds, THE UI SHALL close the form and re-fetch the constraints list.

---

### Requirement 5: Delete a Personal or Role Constraint

**User Story:** As a group admin, I want to delete a personal or role constraint, so that I can remove rules that are no longer applicable.

#### Acceptance Criteria

1. WHEN the "אילוצים" tab is active and `adminGroupId` equals `groupId`, THE UI SHALL display a delete button on each personal and role constraint row.
2. WHEN the admin clicks the delete button, THE UI SHALL display a confirmation dialog in Hebrew before proceeding.
3. WHEN the admin confirms deletion, THE UI SHALL call `DELETE /spaces/{spaceId}/constraints/{constraintId}`.
4. THE System SHALL soft-delete personal and role constraints via the existing `DELETE /spaces/{spaceId}/constraints/{constraintId}` endpoint — no new endpoint is required.
5. WHEN the delete call succeeds, THE UI SHALL remove the constraint from the list without a full page reload.
6. IF the delete API call returns an error, THEN THE UI SHALL display the error message in Hebrew.

---

### Requirement 6: Validate Scope ID on Constraint Creation

**User Story:** As a group admin, I want the system to reject constraint creation if the target person or role does not exist in the space, so that orphaned constraints are never created.

#### Acceptance Criteria

1. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "person"`, THE System SHALL verify that a `Person` record with `id = scope_id` and `space_id = spaceId` exists in the database.
2. IF the person does not exist in the space, THEN THE System SHALL return HTTP 404 with the message "Person not found in this space."
3. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "role"`, THE System SHALL verify that a `SpaceRole` record with `id = scope_id`, `space_id = spaceId`, and `is_active = true` exists in the database.
4. IF the role does not exist or is inactive in the space, THEN THE System SHALL return HTTP 404 with the message "Role not found in this space."
5. THE System SHALL perform these existence checks in the `CreateConstraintCommandHandler` in the Application layer, after the permission check and before inserting the entity.
6. WHEN `scope_type` is `"group"`, `"tasktype"`, or `"space"`, THE System SHALL NOT apply the person/role existence checks — existing behavior is unchanged.

---

### Requirement 7: Solver Applies Personal and Role Constraints

**User Story:** As a group admin, I want personal and role constraints to be respected by the solver when generating a schedule, so that individual limitations and role-wide rules are honoured.

#### Acceptance Criteria

1. WHEN the solver payload is built, THE System SHALL include all active personal constraints (`scope_type = 'Person'`) in the appropriate hard, soft, or emergency constraint list, with `scope_type = "person"` and `scope_id` set to the person's ID.
2. WHEN the solver payload is built, THE System SHALL include all active role constraints (`scope_type = 'Role'`) in the appropriate hard, soft, or emergency constraint list, with `scope_type = "role"` and `scope_id` set to the role's ID.
3. THE System SHALL filter personal and role constraints by `effective_from` and `effective_until` relative to the solver horizon start date — constraints outside the effective window SHALL be excluded from the payload.
4. WHILE a personal constraint with `rule_type = "no_task_type_restriction"` is active for a person, THE Solver SHALL not assign that person to the restricted task type during the effective period.
5. THE System SHALL pass personal and role constraints through the existing `ISolverPayloadNormalizer` pipeline — no separate normalizer is required.

---

### Requirement 8: Registered-Member Guard on Personal Constraint Creation

**User Story:** As a group admin, I want the system to prevent me from creating a personal constraint for an unregistered member, so that constraints are only applied to people who have confirmed their participation.

#### Acceptance Criteria

1. THE UI SHALL exclude unregistered members (those with `linkedUserId = null`) from the person selector in the personal constraint create form.
2. WHEN `POST /spaces/{spaceId}/constraints` is received with `scope_type = "person"` and the target person's `linked_user_id` is null, THE System SHALL return HTTP 422 with the message "Personal constraints can only be applied to registered members."
3. THE System SHALL perform this check in `CreateConstraintCommandHandler` after the person existence check (Requirement 6) and before inserting the entity.
4. IF the person's `invitation_status` is `"pending"`, THE System SHALL treat the person as unregistered and return HTTP 422.

