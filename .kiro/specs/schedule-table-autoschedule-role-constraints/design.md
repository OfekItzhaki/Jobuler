# Design Document — Schedule Table, Auto-Scheduler Gap Detection, and Role Constraints

## Overview

This document covers the technical design for three tightly coupled features:

**Feature 1 — Schedule Table View**: Replace the current list-based schedule display with a proper two-dimensional table (rows = time slots, columns = task names, cells = assigned people). Applied to the group `ScheduleTab` (day and week views) and the admin schedule page. Monthly and yearly views are removed entirely.

**Feature 2 — Auto-Scheduler Gap Detection**: Enhance `AutoSchedulerService.CheckGroupAsync` to detect incomplete coverage by checking every task slot in the horizon. If any slot has no published assignment, trigger the solver once per group with the current published version as the baseline.

**Feature 3 — Personal and Role Constraints**: Add `group_id` to `space_roles` and `person_role_assignments` so roles are group-scoped. Add group-scoped role CRUD endpoints. Extend `CreateConstraintCommandHandler` to validate role and person scope IDs. Extend the `ConstraintsTab` UI into three sections (Group / Role / Personal). Extend the solver to expand role-scoped and group-scoped constraints to individual people before building the CP-SAT model.

---

## Architecture

The system follows a strict 4-layer architecture: `Api → Application → Domain ← Infrastructure`. All changes respect this layering.

```
┌─────────────────────────────────────────────────────────────────┐
│  Frontend (Next.js)                                             │
│  ScheduleTable2D  │  ConstraintsTab (3 sections)  │  RolesSection│
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTP
┌────────────────────────────▼────────────────────────────────────┐
│  Api Layer (ASP.NET Core)                                       │
│  GroupRolesController  │  ConstraintsController (enhanced)      │
└────────────────────────────┬────────────────────────────────────┘
                             │ MediatR
┌────────────────────────────▼────────────────────────────────────┐
│  Application Layer                                              │
│  CreateGroupRoleCommand  │  UpdateGroupRoleCommand              │
│  DeactivateGroupRoleCommand  │  GetGroupRolesQuery              │
│  CreateConstraintCommand (enhanced validation)                  │
│  SolverPayloadNormalizer (effective-date filtering)             │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  Domain Layer                                                   │
│  SpaceRole (+ GroupId)  │  PersonRoleAssignment (+ GroupId)     │
│  ConstraintRule (unchanged)                                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  Infrastructure Layer                                           │
│  AutoSchedulerService (gap detection)                           │
│  SolverPayloadNormalizer (effective-date filtering)             │
│  EF Configurations (new columns)                                │
└─────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────┐
│  Solver (Python / CP-SAT)                                       │
│  constraints.py: expand_group_constraints()                     │
│                  expand_role_constraints()                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Components and Interfaces

### Feature 1 — Frontend Components

#### `ScheduleTable2D` (new component)

**Path**: `apps/web/components/schedule/ScheduleTable2D.tsx`

Replaces the existing `ScheduleTable` (list-based) in both the group `ScheduleTab` and the admin schedule page.

```typescript
interface ScheduleTable2DProps {
  assignments: ScheduleAssignment[];
  currentUserName?: string;  // used to highlight the user's column
  filterDate?: string;       // ISO date string "YYYY-MM-DD"
}
```

**Rendering logic**:
1. Filter `assignments` to those overlapping `filterDate` (if provided).
2. Derive unique task names → columns (sorted alphabetically).
3. Derive unique time slots (start–end pairs) → rows (sorted by start time).
4. Build a `Map<slotKey, Map<taskName, string[]>>` where `slotKey = "${startsAt}|${endsAt}"`.
5. Render an `<table>` with `overflow-x-auto` wrapper.
6. Column header for the current user's task gets a `bg-blue-50` highlight class.
7. Cells with multiple people render names joined by `<br />`.
8. Empty cells render as `—` (em dash) in a muted colour.

The existing `ScheduleTable` component at `apps/web/components/schedule/ScheduleTable.tsx` is **kept** for the admin page's existing list-based rendering path but the admin page will be updated to use `ScheduleTable2D` instead.

#### `ScheduleTab` (modified)

**Path**: `apps/web/app/groups/[groupId]/tabs/ScheduleTab.tsx`

Changes:
- **Day view**: Replace the existing `<table>` list with `<ScheduleTable2D assignments={dayAssignments} filterDate={scheduleDate} currentUserName={currentUserName} />`.
- **Week view**: Replace the per-day card list with seven day-name tab buttons (Sun–Sat). Clicking a tab sets `selectedWeekDay` state. Render `<ScheduleTable2D>` for the selected day. Today's tab gets a `bg-blue-500 text-white` highlight. Default to today's tab on mount.
- Remove the `"month"` and `"year"` view options from the view toggle. The toggle only shows `"day"` and `"week"`.
- Accept a new `currentUserName?: string` prop passed down from `GroupDetailPage`.

#### Admin Schedule Page (modified)

**Path**: `apps/web/app/admin/schedule/page.tsx`

Changes:
- Replace `<ScheduleTable assignments={selected.assignments} />` with `<ScheduleTable2D assignments={selected.assignments} filterDate={selectedDate} />`.
- Add a date picker / day navigation control above the table so the admin can filter by day.
- All other existing functionality (version sidebar, publish/rollback/discard, diff card, CSV/PDF export, infeasibility banner, solver trigger) is preserved unchanged.

#### `ConstraintsTab` (modified)

**Path**: `apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx`

Restructured into three collapsible sections:

1. **אילוצי קבוצה (Group Constraints)** — constraints where `scopeType === "group"` and `scopeId === groupId`.
2. **אילוצי תפקיד (Role Constraints)** — constraints where `scopeType === "role"`.
3. **אילוצים אישיים (Personal Constraints)** — constraints where `scopeType === "person"`.

Each section has its own "New" button, list, and create/edit modal. The role constraint create form includes a role selector populated from `GET /spaces/{spaceId}/groups/{groupId}/roles` (active only). The personal constraint create form includes a person selector populated from the loaded `members` list filtered to `linkedUserId !== null`.

New props added to `ConstraintsTab`:
```typescript
groupRoles: GroupRoleDto[];
groupRolesLoading: boolean;
members: GroupMemberDto[];
```

#### `SettingsTab` — Roles Section (modified)

**Path**: `apps/web/app/groups/[groupId]/tabs/SettingsTab.tsx`

A new "תפקידים" section is added below the existing settings sections. It renders:
- A list of roles for the group (fetched from `GET /spaces/{spaceId}/groups/{groupId}/roles`).
- An "Add Role" inline form (name + optional description).
- Rename and deactivate buttons per role row.
- Deactivated roles are shown with a strikethrough and cannot be re-activated from the UI (admin must use the API directly if needed).

New props:
```typescript
groupRoles: GroupRoleDto[];
groupRolesLoading: boolean;
onCreateRole: (name: string, description?: string) => Promise<void>;
onUpdateRole: (roleId: string, name: string, description?: string) => Promise<void>;
onDeactivateRole: (roleId: string) => Promise<void>;
```

---

### Feature 2 — Backend: AutoSchedulerService

**Path**: `apps/api/Jobuler.Infrastructure/Scheduling/AutoSchedulerService.cs`

The `CheckGroupAsync` method is enhanced. The existing coverage check (comparing `latestAssignmentEnd` to `horizonEnd`) is replaced with a slot-level gap check:

```
For each active TaskSlot in [today, today + horizonDays):
  If no published Assignment exists for that slot → gap detected
```

If any gap is found, trigger the solver once for the group (not once per gap). The `TriggerSolverCommand` already picks up the latest published version as `baselineVersionId` — no change needed there.

The existing skip guards (active run, existing draft, recent failure) are preserved and checked **before** the gap scan to avoid unnecessary DB queries.

New log message at `Information` level lists the gap slot IDs and their start times before triggering.

---

### Feature 3 — Backend: Group-Scoped Roles

#### `GroupRolesController` (new)

**Path**: `apps/api/Jobuler.Api/Controllers/GroupRolesController.cs`

```
Route: /spaces/{spaceId}/groups/{groupId}/roles
```

| Method | Path | Permission | Handler |
|--------|------|-----------|---------|
| GET | `/spaces/{spaceId}/groups/{groupId}/roles` | `SpaceView` | `GetGroupRolesQuery` |
| POST | `/spaces/{spaceId}/groups/{groupId}/roles` | `PeopleManage` | `CreateGroupRoleCommand` |
| PUT | `/spaces/{spaceId}/groups/{groupId}/roles/{roleId}` | `PeopleManage` | `UpdateGroupRoleCommand` |
| DELETE | `/spaces/{spaceId}/groups/{groupId}/roles/{roleId}` | `PeopleManage` | `DeactivateGroupRoleCommand` |

All actions call `IPermissionService.RequirePermissionAsync` before dispatching.

#### Application Commands/Queries (new)

- `CreateGroupRoleCommand(SpaceId, GroupId, Name, Description?, RequestingUserId)` → `Guid`
- `UpdateGroupRoleCommand(SpaceId, GroupId, RoleId, Name, Description?, RequestingUserId)` → `Unit`
- `DeactivateGroupRoleCommand(SpaceId, GroupId, RoleId, RequestingUserId)` → `Unit`
- `GetGroupRolesQuery(SpaceId, GroupId)` → `List<GroupRoleDto>`

`GroupRoleDto`:
```csharp
public record GroupRoleDto(Guid Id, string Name, string? Description, bool IsActive);
```

#### `CreateConstraintCommandHandler` (modified)

**Path**: `apps/api/Jobuler.Application/Constraints/Commands/CreateConstraintCommand.cs`

Validation added after permission check:

- **`ScopeType == Role`**: Verify `ScopeId` is non-null. Query `SpaceRoles` for `id = ScopeId AND space_id = SpaceId AND is_active = true`. If not found → throw `KeyNotFoundException("Role not found in this space.")`.
- **`ScopeType == Person`**: Verify `ScopeId` is non-null. Query `People` for `id = ScopeId AND space_id = SpaceId`. If not found → throw `KeyNotFoundException("Person not found in this space.")`. Then check `linked_user_id IS NOT NULL AND invitation_status = 'accepted'`. If not → throw `InvalidOperationException("Personal constraints can only be applied to registered members.")`.

The `ExceptionHandlingMiddleware` already maps `KeyNotFoundException → 404` and `InvalidOperationException → 400`. The 422 for unregistered members requires a new `UnprocessableEntityException` or mapping `InvalidOperationException` to 422 for this specific message. The cleanest approach is to introduce a `DomainValidationException` that maps to 422.

#### `SolverPayloadNormalizer` (modified)

**Path**: `apps/api/Jobuler.Infrastructure/Scheduling/SolverPayloadNormalizer.cs`

Add effective-date filtering to the constraint query:

```csharp
var horizonStartDate = horizonStart; // DateOnly
var horizonEndDate = horizonEnd;     // DateOnly

var constraints = await _db.ConstraintRules.AsNoTracking()
    .Where(c => c.SpaceId == spaceId && c.IsActive
        && (c.EffectiveUntil == null || c.EffectiveUntil >= horizonStartDate)
        && (c.EffectiveFrom == null || c.EffectiveFrom <= horizonEndDate))
    .ToListAsync(ct);
```

This replaces the current unfiltered query. No other changes to the normalizer are needed — all three scope types already flow through the same pipeline.

---

### Feature 3 — Solver: Constraint Expansion

**Path**: `apps/solver/solver/constraints.py`

Two new expansion functions are added and called from `engine.py` before the CP-SAT model is built:

#### `expand_group_constraints(hard_constraints, soft_constraints, emergency_constraints, people, group_memberships)`

For each constraint with `scope_type == "group"`:
1. Find all `person_id` values in `group_memberships` where `group_id == constraint.scope_id`.
2. For each such person, create a new constraint copy with `scope_type = "person"` and `scope_id = person_id`.
3. Remove the original group-scoped constraint from the list.
4. Add the expanded person-scoped constraints.

#### `expand_role_constraints(hard_constraints, soft_constraints, emergency_constraints, people, role_assignments)`

For each constraint with `scope_type == "role"`:
1. Find all `person_id` values in `role_assignments` where `role_id == constraint.scope_id`.
2. For each such person, create a new constraint copy with `scope_type = "person"` and `scope_id = person_id`.
3. Remove the original role-scoped constraint.
4. Add the expanded person-scoped constraints.

Both functions operate on all three severity lists (hard, soft, emergency) and return the modified lists. They are called at the top of `solve()` before any constraint functions are invoked.

The `SolverInput` model already carries `group_memberships` (via `PersonEligibilityDto.group_ids`) and `role_assignments` (via `PersonEligibilityDto.role_ids`). The expansion functions reconstruct the membership/role maps from the `people` list rather than requiring new payload fields.

---

## Data Models

### DB Changes

#### `space_roles` — add `group_id` column

```sql
ALTER TABLE space_roles
  ADD COLUMN group_id UUID REFERENCES groups(id);
```

- Nullable initially (for backward compatibility with existing space-level roles).
- New roles created via `POST /spaces/{spaceId}/groups/{groupId}/roles` always have `group_id` set.
- The unique index on `(space_id, name)` is replaced with `(space_id, group_id, name)` to allow the same role name in different groups.

#### `person_role_assignments` — add `group_id` column

```sql
ALTER TABLE person_role_assignments
  ADD COLUMN group_id UUID REFERENCES groups(id);
```

- Nullable initially.
- The unique index on `(person_id, role_id)` is extended to `(person_id, role_id, group_id)`.

#### Domain Entity Changes

**`SpaceRole`** — add `GroupId` property:

```csharp
public Guid? GroupId { get; private set; }

public static SpaceRole CreateForGroup(
    Guid spaceId, Guid groupId, string name,
    Guid createdByUserId, string? description = null) =>
    new()
    {
        SpaceId = spaceId,
        GroupId = groupId,
        Name = name.Trim(),
        Description = description?.Trim(),
        CreatedByUserId = createdByUserId
    };
```

**`PersonRoleAssignment`** — add `GroupId` property:

```csharp
public Guid? GroupId { get; private set; }
```

#### EF Configuration Changes

`SpaceRoleConfiguration`:
```csharp
builder.Property(r => r.GroupId).HasColumnName("group_id").IsRequired(false);
builder.HasIndex(r => new { r.SpaceId, r.GroupId, r.Name }).IsUnique();
```

`PersonRoleAssignmentConfiguration`:
```csharp
builder.Property(r => r.GroupId).HasColumnName("group_id").IsRequired(false);
builder.HasIndex(r => new { r.PersonId, r.RoleId, r.GroupId }).IsUnique();
```

### Migration Plan

1. **Migration `AddGroupIdToSpaceRolesAndPersonRoleAssignments`**:
   - Add `group_id UUID NULL` to `space_roles`.
   - Drop old unique index `(space_id, name)` on `space_roles`.
   - Add new unique index `(space_id, group_id, name)` on `space_roles`.
   - Add `group_id UUID NULL` to `person_role_assignments`.
   - Drop old unique index `(person_id, role_id)` on `person_role_assignments`.
   - Add new unique index `(person_id, role_id, group_id)` on `person_role_assignments`.

2. **No data migration needed** — existing roles with `group_id = NULL` continue to work as space-level roles. The new group-scoped role endpoints always set `group_id`.

3. **`GetGroupRolesQuery`** filters by `group_id = groupId` (not null). The existing `GetSpaceRolesQuery` continues to filter by `space_id` only (returns all roles including group-scoped ones — used by the admin constraints page).

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: ScheduleTable2D column and row completeness

*For any* set of schedule assignments, the rendered two-dimensional table SHALL contain exactly one column header per unique task name present in the assignments, and exactly one row header per unique (start, end) time slot pair present in the assignments.

**Validates: Requirements 1.1**

---

### Property 2: Multi-person cell grouping

*For any* set of assignments where multiple people share the same task name and the same time slot, the rendered cell for that (task, slot) combination SHALL contain all of those people's names.

**Validates: Requirements 1.2**

---

### Property 3: Current user column highlight

*For any* set of assignments and any person name designated as the current user, if that person has at least one assignment, the column header for their task SHALL carry the highlight CSS class; all other column headers SHALL NOT carry that class.

**Validates: Requirements 1.8**

---

### Property 4: Date filter correctness

*For any* set of assignments spanning multiple dates and any filter date D, the filtered assignment set SHALL contain exactly those assignments whose slot overlaps date D, and no assignments from other dates.

**Validates: Requirements 3.2**

---

### Property 5: Gap detection triggers solver exactly once per group

*For any* group with a set of active task slots in the horizon and a published schedule that leaves at least one slot uncovered, the `AutoSchedulerService` SHALL trigger the solver exactly once for that group — not once per gap slot.

**Validates: Requirements 4.1, 4.2**

---

### Property 6: Constraint scope filtering in UI

*For any* list of constraints with mixed scope types (group, role, person), the group constraints section SHALL display only constraints where `scopeType === "group"` and `scopeId === groupId`; the role section SHALL display only `scopeType === "role"` constraints; the personal section SHALL display only `scopeType === "person"` constraints. No constraint SHALL appear in more than one section.

**Validates: Requirements 5.1, 6.1, 7.1**

---

### Property 7: Role constraint scope validation

*For any* `POST /spaces/{spaceId}/constraints` request with `scope_type = "role"`, if `scope_id` is null, empty, or references a role that does not exist or is inactive in the space, the system SHALL return a non-2xx error response and SHALL NOT insert a constraint record.

**Validates: Requirements 8.1, 8.2**

---

### Property 8: Person constraint scope validation

*For any* `POST /spaces/{spaceId}/constraints` request with `scope_type = "person"`, if `scope_id` references a person who does not exist in the space or whose `linked_user_id` is null or `invitation_status` is not `"accepted"`, the system SHALL return a non-2xx error response and SHALL NOT insert a constraint record.

**Validates: Requirements 9.1, 9.2, 9.3**

---

### Property 9: Solver payload includes all three constraint scope levels

*For any* space with active constraints of scope types group, role, and person, the solver payload built by `ISolverPayloadNormalizer` SHALL include all three scope types in the appropriate constraint lists (hard, soft, or emergency), with no scope type silently dropped.

**Validates: Requirements 10.1, 10.2, 10.3**

---

### Property 10: Effective-date filtering is uniform across scope types

*For any* horizon starting on date D and any set of constraints of mixed scope types (group, role, person) with various `effective_from` / `effective_until` values, the solver payload SHALL include a constraint if and only if its effective window overlaps the horizon `[D, D + horizonDays - 1]`, regardless of scope type.

**Validates: Requirements 10.4, 13.1, 13.2, 13.3, 13.4**

---

### Property 11: Group role creation is group-scoped

*For any* `POST /spaces/{spaceId}/groups/{groupId}/roles` request with a valid name, the created `SpaceRole` SHALL have `group_id = groupId` and `is_active = true`. Fetching roles for a different group SHALL NOT return this role.

**Validates: Requirements 12.1, 12.7**

---

### Property 12: Role update round-trip

*For any* existing group role, calling `PUT /spaces/{spaceId}/groups/{groupId}/roles/{roleId}` with a new name and description, then fetching the role via `GET /spaces/{spaceId}/groups/{groupId}/roles`, SHALL return the updated name and description.

**Validates: Requirements 12.2**

---

### Property 13: Active-only role selector

*For any* list of roles for a group containing both active and inactive roles, the role selector in the role constraint create form SHALL display only the active roles.

**Validates: Requirements 12.6**

---

## Error Handling

### New Exception Type

`DomainValidationException` is introduced in `Jobuler.Application.Common`:

```csharp
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message) { }
}
```

`ExceptionHandlingMiddleware` maps `DomainValidationException → HTTP 422 Unprocessable Entity`.

This is used for the "personal constraints can only be applied to registered members" check (Requirement 9.3), which is semantically a 422 (the request is well-formed but the entity is in an invalid state for this operation).

### Constraint Validation Error Flow

```
POST /spaces/{spaceId}/constraints
  → ConstraintsController.Create
    → IPermissionService.RequirePermissionAsync (403 if denied)
    → CreateConstraintCommand dispatched
      → CreateConstraintCommandHandler
        → [scope_type = role] SpaceRole lookup → KeyNotFoundException → 404
        → [scope_type = person] Person lookup → KeyNotFoundException → 404
        → [scope_type = person] Registered check → DomainValidationException → 422
        → ConstraintRule.Create + SaveChanges → 201
```

### AutoScheduler Error Handling

The existing per-group try/catch in `CheckAndTriggerAsync` is preserved. Gap detection errors (e.g. DB timeout) are caught and logged at `Error` level without stopping the check for other groups.

### Solver Expansion Errors

If `expand_role_constraints` or `expand_group_constraints` encounters a constraint referencing a role/group ID with no members, it logs a warning and produces zero expanded constraints (the original constraint is still removed). This is a graceful degradation — an empty role is not an error.

---

## Testing Strategy

### Unit Tests

**Frontend** (Vitest + React Testing Library):

- `ScheduleTable2D`: render with various assignment sets, verify column headers, row headers, cell contents, highlight class, empty-state message.
- `ConstraintsTab`: render with mixed-scope constraint lists, verify each section shows only its scope type.
- `SettingsTab` roles section: render with active/inactive roles, verify inactive roles are not shown in the role selector.

**Backend** (xUnit):

- `CreateConstraintCommandHandler`: test role validation (missing role → 404, inactive role → 404, valid role → 201), person validation (missing person → 404, unregistered → 422, registered → 201).
- `SolverPayloadNormalizer`: test effective-date filtering with constraints before, during, and after the horizon.
- `AutoSchedulerService.CheckGroupAsync`: test gap detection with mock DB — all slots covered (no trigger), one slot uncovered (trigger once), all slots uncovered (trigger once).

**Solver** (pytest):

- `expand_role_constraints`: test with role constraints referencing roles with 0, 1, and N members.
- `expand_group_constraints`: test with group constraints referencing groups with 0, 1, and N members.
- End-to-end: build a `SolverInput` with role and group constraints, call `solve()`, verify the constraints are respected in the output assignments.

### Property-Based Tests

**Property-based testing library**: `fast-check` (TypeScript/frontend), `Hypothesis` (Python/solver), `FsCheck` (C#/backend).

Each property test runs a minimum of **100 iterations**.

Tag format: `// Feature: schedule-table-autoschedule-role-constraints, Property N: <property_text>`

| Property | Test Location | Generator |
|----------|--------------|-----------|
| P1: Column/row completeness | `ScheduleTable2D.test.tsx` | Random assignment arrays with 1–20 tasks, 1–10 slots |
| P2: Multi-person cell grouping | `ScheduleTable2D.test.tsx` | Random assignments with 2–5 people per slot |
| P3: Current user highlight | `ScheduleTable2D.test.tsx` | Random assignments + random current user name |
| P4: Date filter correctness | `ScheduleTable2D.test.tsx` | Random assignments across 1–14 dates, random filter date |
| P5: Gap detection triggers once | `AutoSchedulerServiceTests.cs` | Random task slot sets with random coverage gaps |
| P6: Constraint scope filtering | `ConstraintsTab.test.tsx` | Random constraint lists with mixed scope types |
| P7: Role constraint validation | `CreateConstraintCommandHandlerTests.cs` | Random role IDs (existing/missing/inactive) |
| P8: Person constraint validation | `CreateConstraintCommandHandlerTests.cs` | Random person states (missing/unregistered/registered) |
| P9: Payload includes all scope types | `SolverPayloadNormalizerTests.cs` | Random constraint sets with all three scope types |
| P10: Effective-date filtering uniform | `SolverPayloadNormalizerTests.cs` | Random date ranges relative to horizon |
| P11: Group role creation scoped | `GroupRolesTests.cs` | Random role names, two groups |
| P12: Role update round-trip | `GroupRolesTests.cs` | Random names/descriptions |
| P13: Active-only role selector | `ConstraintsTab.test.tsx` | Random role lists with mixed active/inactive |

### Integration Tests

- Admin schedule page: verify all existing functionality (version list, publish, rollback, discard, CSV/PDF export, solver trigger) still works after the `ScheduleTable2D` swap.
- `AutoSchedulerService`: end-to-end test with a real (test) DB verifying the solver is triggered when gaps exist and not triggered when coverage is complete.
- Solver: end-to-end test with a payload containing role and group constraints, verify the output assignments respect those constraints.
