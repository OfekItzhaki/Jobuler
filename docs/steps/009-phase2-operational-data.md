# Step 009 — Phase 2: Operational Data

## Phase
Phase 2 — Operational Data

## Purpose
Implement the full operational domain: people, groups, tasks, constraints, availability, restrictions, and presence windows. This is the data layer the solver needs to compute schedules. Without this, the solver has nothing to work with.

## What was built

### Domain entities

| File | Description |
|---|---|
| `Jobuler.Domain/People/Person.cs` | Person record within a space; optional link to auth user |
| `Jobuler.Domain/People/PersonQualification.cs` | Certifications/qualifications with expiry |
| `Jobuler.Domain/People/AvailabilityWindow.cs` | Time windows when a person can be scheduled |
| `Jobuler.Domain/People/PresenceWindow.cs` | Where a person is (FreeInBase / AtHome / OnMission); OnMission is derived-only |
| `Jobuler.Domain/People/PersonRestriction.cs` | Operational restriction (visible to admins with people.manage) |
| `Jobuler.Domain/People/SensitiveRestrictionReason.cs` | Sensitive reason behind a restriction (requires restrictions.manage_sensitive) |
| `Jobuler.Domain/Groups/GroupType.cs` | Dynamic group type (squad, unit, platoon, etc.) |
| `Jobuler.Domain/Groups/Group.cs` | Group instance under a type |
| `Jobuler.Domain/Groups/GroupMembership.cs` | Person ↔ Group link |
| `Jobuler.Domain/Groups/PersonRoleAssignment.cs` | Person ↔ Role link |
| `Jobuler.Domain/Tasks/TaskBurdenLevel.cs` | Enum: Favorable / Neutral / Disliked / Hated |
| `Jobuler.Domain/Tasks/TaskType.cs` | Semantic duty definition with burden level and overlap flag |
| `Jobuler.Domain/Tasks/TaskSlot.cs` | Scheduled occurrence with time, headcount, required roles/qualifications |
| `Jobuler.Domain/Tasks/TaskTypeOverlapRule.cs` | Explicit overlap compatibility between two task types |
| `Jobuler.Domain/Constraints/ConstraintRule.cs` | Flexible constraint with scope, severity, rule_type, and JSON payload |

### Infrastructure (EF Core configurations)

| File | Description |
|---|---|
| `Configurations/PeopleConfiguration.cs` | Fluent mappings for all people-related entities |
| `Configurations/GroupsConfiguration.cs` | Fluent mappings for groups and memberships |
| `Configurations/TasksConfiguration.cs` | Fluent mappings for task types, slots, overlap rules |
| `Configurations/ConstraintConfiguration.cs` | Fluent mapping for constraint_rules with JSONB column |
| `Configurations/StringExtensions.cs` | ToSnakeCase / ToPascalCase helpers for enum DB conversions |
| `Persistence/AppDbContext.cs` | Extended with all Phase 2 DbSets |

### Application layer (CQRS)

| File | Description |
|---|---|
| `People/Commands/CreatePersonCommand.cs` | Create person |
| `People/Commands/UpdatePersonCommand.cs` | Update person profile |
| `People/Commands/AddRestrictionCommand.cs` | Add restriction + optional sensitive reason in one transaction |
| `People/Queries/GetPeopleQuery.cs` | List people + person detail with conditional sensitive data |
| `Tasks/Commands/CreateTaskTypeCommand.cs` | Create task type |
| `Tasks/Commands/CreateTaskSlotCommand.cs` | Create task slot |
| `Tasks/Queries/GetTaskTypesQuery.cs` | List task types and slots with optional time filter |
| `Constraints/Commands/CreateConstraintCommand.cs` | Create constraint rule |
| `Constraints/Queries/GetConstraintsQuery.cs` | List constraints |

### API controllers

| File | Endpoints |
|---|---|
| `PeopleController.cs` | `GET/POST /spaces/{id}/people`, `GET/PUT /spaces/{id}/people/{id}`, `POST /spaces/{id}/people/{id}/restrictions` |
| `TasksController.cs` | `GET/POST /spaces/{id}/task-types`, `GET/POST /spaces/{id}/task-slots` |
| `ConstraintsController.cs` | `GET/POST /spaces/{id}/constraints` |

## Key decisions

### Sensitive restriction separation enforced at two levels
1. DB: `sensitive_restriction_reasons` is a separate table with its own RLS policy
2. API: `PeopleController` checks `restrictions.manage_sensitive` before accepting or returning sensitive data
3. Query: `GetPersonDetailQuery` only loads sensitive reasons when `IncludeSensitive = true`

### PresenceState.OnMission is derived-only
The `PresenceWindow.CreateManual()` factory throws if you try to set `OnMission` manually. Only `CreateDerived()` can produce it. This is enforced in the domain, not just the API.

### Constraint payload is open JSON
`rule_payload_json` is a JSONB column with no fixed schema. Each `rule_type` defines its own payload shape (documented in the `ConstraintRule` XML comment). This keeps the constraint model extensible without migrations for new rule types.

### TaskSlot stores role/qualification IDs as JSON arrays
`required_role_ids_json` and `required_qualification_ids_json` are serialized `List<Guid>` stored as JSONB. The solver reads these directly from the payload. No join table needed for this relationship.

## How it connects
- People, task slots, and constraints are the primary inputs to the solver payload (Phase 3)
- `GetPersonDetailQuery` with `IncludeSensitive` is the pattern for all permission-conditional data loading
- Availability and presence windows feed into solver eligibility filtering

## How to run / verify

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.local","password":"Demo1234!"}' | jq -r .accessToken)

SPACE="10000000-0000-0000-0000-000000000001"

# List people
curl "http://localhost:5000/spaces/$SPACE/people" -H "Authorization: Bearer $TOKEN"

# Create a person
curl -X POST "http://localhost:5000/spaces/$SPACE/people" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"fullName":"Test Person","displayName":"Test"}'

# List task types
curl "http://localhost:5000/spaces/$SPACE/task-types" -H "Authorization: Bearer $TOKEN"

# Create a constraint
curl -X POST "http://localhost:5000/spaces/$SPACE/constraints" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"scopeType":"space","scopeId":null,"severity":"hard","ruleType":"min_rest_hours","rulePayloadJson":"{\"hours\":8}","effectiveFrom":null,"effectiveUntil":null}'
```

## What comes next
- Phase 3: Solver payload normalization reads from these tables to build `SolverInput`
- Phase 3: Full CP-SAT hard constraints use restriction and availability data
- Phase 4: Draft/publish workflow reads task slots and assignments
