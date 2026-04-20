# Forces Scheduler SaaS — KIRO-Ready Technical Specification

## 1. Product Goal

Build a secure, multilingual, multi-tenant web service for force/platoon/shift scheduling. The platform must automatically generate and continuously re-optimize schedules using a deterministic constraint solver over a 7-day planning horizon, while strongly preserving the schedule for today and tomorrow and minimizing unnecessary changes across the rest of the horizon.[cite:51][cite:88][cite:93]

The system targets military-style force scheduling first, but the domain model should remain generic enough to support other shift-based organizations later with mostly branding and terminology changes.[cite:33][cite:49]

## 2. Core Product Principles

- All users authenticate and enter the application in read-only viewer mode by default.
- Users with sufficient permission can switch into administrator mode.
- Normal users cannot edit scheduling, personnel, constraints, permissions, or publication state.
- Scheduling is fully automatic after admin changes are saved.
- The scheduler must recompute the full 7-day planning window after meaningful changes.
- The scheduler must apply time-weighted stability penalties: changes in today and tomorrow are heavily penalized, changes in mid-horizon days are moderately penalized, and changes in later days are lightly penalized.[cite:88][cite:91][cite:93]
- Emergency changes are entered as facts, constraints, or operational updates; the system then re-optimizes automatically with minimum near-term disturbance.[cite:74][cite:75]
- Published schedules are immutable snapshots; rollback restores a previous schedule by creating a new version, not by mutating history.[cite:59]
- The solver is the source of truth for scheduling decisions; AI, if added, is only an assistant layer for parsing, summarization, and explanations.[cite:33][cite:51]

## 3. Primary Use Cases

### 3.1 Viewer Use Cases

- View current published schedule.
- Search assignments by person, task, date, time, or time range.
- View today and tomorrow first, with access to the rest of the 7-day horizon.
- View personal assignments and general roster visibility allowed by permissions.
- Optionally update personal profile picture only.

### 3.2 Admin Use Cases

- Enter admin mode from the same authenticated application session.
- Create and manage spaces.
- Manage space roles, groups, permissions, and ownership transfer.
- Add and manage people, availability, qualifications, and hard individual restrictions.
- Create tasks and time slots.
- Define hard and soft constraints at space and group scopes.
- Enter emergency changes and operational updates.
- Trigger automatic schedule recalculation by saving changes.
- Review diffs, solver results, logs, conflicts, fairness metrics, and stability summaries.
- Publish a draft schedule version.
- Roll back to one of the previous published versions.

## 4. Functional Scope

### 4.1 Spaces and Multi-Tenancy

A **space** is the core tenant workspace for one operational context such as a platoon, company, site, or other shift-based team. Each space has isolated users, people records, groups, roles, tasks, schedules, permissions, logs, and versions.[cite:49][cite:59]

Support multiple spaces per SaaS environment, with strong tenant isolation enforced both in application logic and PostgreSQL Row-Level Security (RLS) policies.[cite:49][cite:59][cite:60]

### 4.2 Roles and Permissions

The system must support two separate ideas:

1. **System/space permissions** — who can view, administer, publish, manage permissions, transfer ownership, or manage sensitive restrictions.
2. **Operational roles** — dynamic roles created by the space owner/admin such as Soldier, Squad Commander, Unit Commander, Platoon Commander, Observer, Medic, War Room Operator, Kitchen Lead, and so on.

Operational roles are data, not hardcoded enums. Space owners/admins can create, rename, deactivate, and assign these roles per space.

Permission examples:

| Permission | Description |
|---|---|
| `space.view` | View published schedule and allowed roster data |
| `space.admin_mode` | Enter admin mode |
| `people.manage` | Manage people and profile-level operational data |
| `constraints.manage` | Create/edit space or group constraints |
| `restrictions.manage_sensitive` | View/edit sensitive reasons |
| `tasks.manage` | Manage task types and task slots |
| `schedule.recalculate` | Trigger recalculation |
| `schedule.publish` | Publish drafts |
| `schedule.rollback` | Restore previous version |
| `permissions.manage` | Grant/revoke permissions |
| `ownership.transfer` | Transfer ownership |
| `logs.view_sensitive` | View sensitive audit/system logs |

### 4.3 Ownership Transfer

A space must have an owner. Ownership can be delegated, transferred fully, or revoked. Every ownership change must be logged in a dedicated ownership history table with timestamps, acting users, previous owner, new owner, and reason if provided.

### 4.4 Groups and Dynamic Hierarchy

Support dynamic grouping structures inside a space. Group types are also dynamic. Example group types include squad, unit, platoon, company, or custom business equivalents.

The system must support:
- Creating group types.
- Creating groups under those types.
- Assigning people to one or more groups.
- Applying constraints to an individual, role, group, task type, or entire space.

### 4.5 People and Availability

Each person record should support:
- Full name.
- Display name / nickname.
- Profile image.
- Active/inactive state.
- Role assignments.
- Group memberships.
- Qualifications/certifications.
- Availability windows.
- Home/base state over time.
- Hard individual restrictions.
- Optional sensitive notes separated from public operational restrictions.

### 4.6 Tasks and Time Slots

Model tasks in two layers:

1. **Task Type** — semantic duty definition such as Point 1, Point 2, Kitchen, War Room, Patrol, Reserve, Gate, Duty Officer, or generic shift type.
2. **Task Slot** — scheduled occurrence with date/time, capacity, required roles/qualifications, priority, overlap behavior, and location.

Every task type must include `task_burden_level` with these values:
- Favorable
- Neutral
- Disliked
- Hated

Task types must be editable later by admins.

### 4.7 Overlap Rules

Default behavior: a person cannot be assigned to overlapping task slots.[cite:46]

Support explicit overlap rules:
- Task type may declare `allows_overlap = true`.
- Compatibility can be refined via task type pair rules if necessary, for example Task A may overlap with Task B, but not with Task C.
- If no explicit compatibility exists, overlap is forbidden.[cite:46]

### 4.8 Free-in-Base / At-Home Views

Maintain operational state lists visible mainly to admins:
- People free in base, including time windows.
- People at home, including time windows.

These lists should be permission-controlled and optionally hidden from non-admin users.

## 5. Scheduling Philosophy

The scheduler must be deterministic and constraint-based, implemented with OR-Tools CP-SAT rather than an LLM making direct scheduling decisions.[cite:33][cite:51]

### 5.1 Why CP-SAT

OR-Tools CP-SAT is suited to scheduling with binary decision variables, hard constraints, and weighted soft objectives such as fairness and stability. It supports exact feasibility rules, no-overlap conditions, capacity requirements, and optimization over competing penalties.[cite:33][cite:46][cite:51]

### 5.2 Time Horizon

- Planning horizon: 7 days.
- Near-term priority window: today + tomorrow.
- Mid horizon: days 3–4.
- Far horizon: days 5–7.

The solver must always compute a full 7-day draft after meaningful changes, but disturbance penalties must be strongest in the near-term window and weaker farther away.[cite:88][cite:91][cite:93]

### 5.3 Replan Modes

#### Standard Replan
- Triggered after typical admin changes.
- Recomputes full 7-day horizon asynchronously.
- Preserves today/tomorrow as strongly as possible.
- Preserves the rest when possible, but may reshape later days more freely if beneficial.

#### Emergency Replan
- Triggered after urgent operational changes.
- Recomputes full 7-day horizon too, but may prioritize rapid first response for next 24–48 hours and finalize the full schedule in the background.[cite:74][cite:75]
- Must return a clear change summary.

### 5.4 Stability Model

The scheduler must compare every new candidate plan against the current baseline draft or published version and assign weighted penalties for deviations. The weights must be time-sensitive:[cite:88][cite:93]

| Window | Change penalty intensity |
|---|---|
| Today + tomorrow | Very high |
| Days 3–4 | Medium |
| Days 5–7 | Low |

This ensures that the system can globally recompute the 7-day plan while protecting operational stability where it matters most.[cite:90][cite:93]

## 6. Constraint Model

### 6.1 Constraint Scopes

Constraints can target:
- Individual person.
- Dynamic role.
- Group.
- Task type.
- Entire space.

### 6.2 Constraint Severity

Recommended model:
- **Space constraints**: hard or soft.
- **Group constraints**: hard or soft.
- **Individual constraints**: hard by default.

This matches the requirement for minimal room for error on person-level restrictions, while preserving feasibility at broader scopes.[cite:69]

### 6.3 Hard Constraints

Examples:
- Minimum 8 hours of rest between mission end and next mission start.[cite:46]
- No overlap unless explicitly allowed.[cite:46]
- Required qualifications or operational role requirements.[cite:33]
- Minimum free people on base for a time window.[cite:33]
- Kitchen cannot be assigned twice in a row.[cite:33]
- Kitchen cannot exceed 2 assignments per rolling 7-day window.[cite:33]
- Individual no-assignment restriction for task types or time windows.
- Required slot staffing minimums.
- A person cannot be assigned twice to the same slot.

### 6.4 Soft Constraints

Examples:
- Avoid changing current planned assignments unless necessary.[cite:88][cite:93]
- Avoid assigning the same burden level in consecutive missions where feasible.[cite:81]
- Prefer fair distribution of disliked and hated tasks.[cite:81][cite:84]
- Prefer continuity within a squad or unit.
- Prefer keeping later horizon changes low when feasible.[cite:88]

### 6.5 Conflict Handling

When no feasible plan exists under all hard constraints, the system must:
- Mark the solve as infeasible.
- Explain which hard constraints collide, as much as possible.
- Show impacted time ranges, tasks, or people.
- Allow admins to adjust or remove constraints based on permission level.
- Never silently violate a hard constraint in normal mode.[cite:33][cite:69]

## 7. Fairness and Burden Distribution

The system must maintain a fairness ledger that tracks historical distribution of disliked and hated duties, kitchen frequency, total assignments, nights, weekends, and other burdensome patterns if configured.

Fairness goals:
- Avoid giving the same person repeated disliked/hated assignments when alternatives exist.[cite:81][cite:84]
- Avoid assigning the same burden level in consecutive missions where feasible.
- Balance burden counts across all eligible personnel over time.
- Preserve fairness history across versions and solver runs.

Suggested fairness counters:
- Total assignments last 7/14/30 days.
- Hated tasks last 7/14/30 days.
- Disliked + hated combined score.
- Kitchen count this week.
- Night missions this week.
- Consecutive burden repetition count.

## 8. Draft, Publish, Rollback, and Versioning

### 8.1 Versioning Model

Treat schedule versions as top-level immutable snapshots. Each version owns its assignment rows. Published versions are never edited in place.[cite:59]

Recommended schedule version metadata:
- `id`
- `space_id`
- `version_number`
- `status` (`draft`, `published`, `rolled_back`, `archived`)
- `based_on_version_id`
- `solver_run_id`
- `created_by_user_id`
- `created_at`
- `published_at`
- `rollback_source_version_id`
- `summary_json`

Assignment rows should reference the version they belong to.

### 8.2 Why Snapshot Versions

This makes rollback safer and simpler because a rollback is implemented as “create a new version based on an older version,” not “try to mutate the current rows back into an older shape.” This also simplifies diffs, auditing, and historical inspection.[cite:59]

### 8.3 Rollback Requirement

Support rollback to at least the last 7 published versions.

### 8.4 Diff Requirement

Every recalculated draft should produce a diff against the current baseline, including:
- Assignments added.
- Assignments removed.
- Assignments changed by person.
- Assignments changed by time.
- Burden distribution changes.
- Violated soft constraints count delta.
- Stability score summary.

## 9. Logging and Observability

### 9.1 System Logs

System logs must support filtering by time, severity, event type, actor, space, and correlation ID.

Required system log categories:
- Solver started/completed/failed.
- Schedule changed with summary.
- Hard constraint infeasibility.
- Emergency replan requested.
- Publish event.
- Rollback event.
- Permission grant/revoke.
- Ownership transfer.
- Sensitive restriction created/updated/deleted.
- Queue retry/failure/dead-letter event.[cite:53][cite:56]
- Security-relevant auth/access event.

### 9.2 Audit Logs

Audit logs should capture who did what, when, in which space, against which entity, with before/after snapshots where appropriate.

### 9.3 UI Severity Guidance

- Info: normal solve and publication events.
- Warning: soft-constraint pressure, unusual fairness outcomes, suspicious but non-failing conditions.
- Error: solver failure, failed background job, unexpected technical issue.
- Critical/Red: infeasible hard constraints, security issue, forced override mode, or emergency condition requiring attention.

## 10. AI Assistant Role

AI is optional for MVP and must not be the scheduling authority.[cite:33][cite:51]

Recommended AI use cases later:
- Parse natural-language admin input into structured rules, for example “Ofek cannot do kitchen for 10 days.”
- Summarize schedule diffs in plain language.
- Explain likely reasons for infeasible schedules or trade-offs.
- Help generate multilingual admin summaries.

Suggested flow:
1. Admin enters free text.
2. AI converts it into structured candidate data.
3. Admin reviews and confirms.
4. System stores the structured rule.
5. Solver reruns automatically.

## 11. Security Requirements

### 11.1 Multi-Tenant Isolation

Use PostgreSQL with tenant identifiers on all tenant-owned rows and enforce isolation using Row-Level Security policies.[cite:49][cite:59][cite:60]

RLS reminder: Row-Level Security is a PostgreSQL feature that restricts which rows can be read or written according to policies, adding a database-enforced security boundary beyond application code.[cite:59]

### 11.2 Transport Security

Use TLS for all network transport, including HTTPS for frontend/API traffic and secure DB/service connections.

### 11.3 Authorization

- Default all users to least privilege.
- Viewer mode by default.
- Admin mode explicitly permission-gated.
- Sensitive notes and reasons should require stronger permission than general schedule editing.
- Publish, rollback, ownership transfer, and permission edits should require elevated privileges.

### 11.4 Sensitive Data Handling

Separate operational restriction from sensitive reason where possible.

Example:
- Operational restriction visible to normal admins: “Cannot do kitchen until 2026-05-10.”
- Sensitive reason visible only to privileged admins: “Hand infection.”

### 11.5 Required Security Controls

- Password or external auth security best practices.
- Session expiration and rotation.
- Audit trail for sensitive actions.
- Rate limiting on auth and critical endpoints.
- CSRF protection where applicable.
- Input validation on all APIs.
- Encryption at rest where available in managed infrastructure.

## 12. Recommended Tech Stack

### 12.1 Frontend

**Next.js + TypeScript**

Why:
- Strong fit for data-heavy web apps.
- Excellent component ecosystem.
- Good support for authentication flows.
- Good multilingual support and RTL capabilities.
- Strong developer productivity.

UI suggestion:
- Tailwind CSS + component system such as shadcn/ui.
- Data table library for advanced filtering and searching.
- Calendar/timeline UI for schedule visualization.

### 12.2 Backend API

**ASP.NET Core Web API**

Why:
- Strong typing and architecture for complex business rules.
- Excellent auth, middleware, validation, and background-service support.
- Good fit with existing .NET experience.
- Clean service boundaries for orchestration.

Alternative:
- NestJS if an all-TypeScript backend is strongly preferred.

### 12.3 Solver Service

**Python + OR-Tools CP-SAT**

Why:
- Best ecosystem fit for optimization modeling here.[cite:33][cite:51]
- Easier solver experimentation and iteration.
- Natural separation between orchestration API and scheduling engine.

### 12.4 Database

**PostgreSQL**

Why:
- Strong relational consistency.
- Mature indexing and time-based querying.
- JSON support for flexible rule metadata.
- RLS support for SaaS multi-tenancy.[cite:49][cite:59]

Reminder: PostgreSQL uses the PostgreSQL SQL dialect, which is standards-based SQL with PostgreSQL-specific extensions.[cite:59]

### 12.5 Queue and Background Processing

**Redis + background jobs**

If backend-centric in .NET:
- Redis + Hangfire or a queue abstraction + worker service.

If mixed Node orchestration is introduced:
- Redis + BullMQ patterns for retries, backoff, idempotency, and dead-letter handling are strong references.[cite:53][cite:56]

### 12.6 Cache

Use Redis for:
- Hot schedule view cache.
- Permission snapshot cache.
- Draft diff cache.
- Rate limiting.
- Solver deduplication/lock support.

### 12.7 Object Storage

Use S3-compatible storage for exports such as CSV/PDF and potentially archived artifacts.

### 12.8 Search

Start with PostgreSQL full-text and trigram indexing for person and task search. Avoid early adoption of Elasticsearch unless scale clearly demands it.

### 12.9 Observability

Include:
- Structured centralized logs.
- Error monitoring.
- Metrics and alerts.
- Queue depth and solver runtime metrics.
- Publish failure alarms.

## 13. Architecture Overview

### 13.1 High-Level Components

- Web frontend.
- Auth subsystem.
- API/orchestration backend.
- PostgreSQL database.
- Redis cache/queue.
- Solver worker service.
- Notification/export services.
- Logging/monitoring stack.

### 13.2 Recommended Runtime Flow

1. User logs in.
2. User enters viewer mode.
3. If authorized, user can switch to admin mode.
4. Admin edits data in draft context.
5. Admin clicks Done/Save.
6. API validates and stores changes.
7. API enqueues solver job.
8. Solver worker loads normalized planning data.
9. OR-Tools computes a new 7-day draft using hard constraints plus weighted objectives.[cite:33][cite:51]
10. Backend stores solver run, generated draft version, metrics, diff, and logs.
11. Admin reviews the draft.
12. Admin publishes or rolls back.
13. Viewer users continue to see only the latest published version.

### 13.3 Service Responsibilities

#### Frontend
- Authentication UI.
- Viewer/admin mode shell.
- Tables, filters, search, timeline/calendar views.
- Draft review, diff presentation, logs.
- Localization and RTL behavior.

#### API Backend
- Auth integration.
- Permission enforcement.
- CRUD for spaces, people, groups, tasks, constraints, versions.
- Draft workflow orchestration.
- Solver job scheduling.
- Publishing and rollback.
- Export requests.
- Audit and system log generation.

#### Solver Worker
- Normalize solver input.
- Construct CP-SAT model.
- Solve with timeout and fallback strategy.
- Produce assignments, violation summaries, fairness metrics, and explanation-friendly output.

## 14. Data Model

Minimum required entities:

### Core Identity and Access
- `users`
- `spaces`
- `space_memberships`
- `space_permission_grants`
- `space_roles` (dynamic operational roles)
- `ownership_transfer_history`

### Operational Structure
- `group_types`
- `groups`
- `group_memberships`
- `people`
- `person_role_assignments`
- `person_qualifications`
- `availability_windows`
- `presence_windows` (free/base/home state)
- `person_restrictions`
- `sensitive_restriction_reasons`

### Scheduling Domain
- `task_types`
- `task_type_overlap_rules`
- `task_slots`
- `constraint_rules`
- `schedule_runs`
- `schedule_versions`
- `assignments`
- `assignment_change_summaries`
- `fairness_counters`

### Logging and Exports
- `audit_logs`
- `system_logs`
- `exports`

## 15. Suggested Table Shape Notes

### `constraint_rules`
Suggested columns:
- `id`
- `space_id`
- `scope_type` (`person`, `role`, `group`, `task_type`, `space`)
- `scope_id`
- `severity` (`hard`, `soft`)
- `rule_type`
- `rule_payload_json`
- `is_active`
- `effective_from`
- `effective_until`
- `created_by_user_id`
- `updated_by_user_id`
- `created_at`
- `updated_at`

### `task_types`
Suggested columns:
- `id`
- `space_id`
- `name`
- `description`
- `task_burden_level`
- `default_priority`
- `allows_overlap`
- `is_active`
- `created_at`
- `updated_at`

### `task_slots`
Suggested columns:
- `id`
- `space_id`
- `task_type_id`
- `starts_at`
- `ends_at`
- `required_headcount`
- `priority`
- `required_role_ids_json`
- `required_qualification_ids_json`
- `status`
- `created_at`
- `updated_at`

### `schedule_runs`
Suggested columns:
- `id`
- `space_id`
- `trigger_type` (`standard`, `emergency`, `manual`, `rollback`)
- `baseline_version_id`
- `requested_by_user_id`
- `status`
- `solver_input_hash`
- `started_at`
- `finished_at`
- `duration_ms`
- `result_summary_json`
- `error_summary`

### `schedule_versions`
Suggested columns:
- `id`
- `space_id`
- `version_number`
- `status`
- `baseline_version_id`
- `source_run_id`
- `created_by_user_id`
- `published_by_user_id`
- `rollback_source_version_id`
- `created_at`
- `published_at`
- `summary_json`

### `assignments`
Suggested columns:
- `id`
- `schedule_version_id`
- `space_id`
- `task_slot_id`
- `person_id`
- `assignment_source` (`solver`, `override`)
- `change_reason_summary`
- `created_at`

## 16. API Design Guidelines

Suggested API groups:
- `/auth`
- `/spaces`
- `/spaces/{spaceId}/members`
- `/spaces/{spaceId}/permissions`
- `/spaces/{spaceId}/roles`
- `/spaces/{spaceId}/groups`
- `/spaces/{spaceId}/people`
- `/spaces/{spaceId}/tasks`
- `/spaces/{spaceId}/task-slots`
- `/spaces/{spaceId}/constraints`
- `/spaces/{spaceId}/schedule-runs`
- `/spaces/{spaceId}/schedule-versions`
- `/spaces/{spaceId}/assignments`
- `/spaces/{spaceId}/logs`
- `/spaces/{spaceId}/exports`

Key endpoint behaviors:
- Save draft changes and trigger solve.
- Get current published version.
- Get latest draft version.
- Get diff between versions.
- Publish version.
- Roll back to version.
- Search assignments by name/task/time range.

## 17. Solver Contract

### 17.1 Input Payload

The orchestration backend should send a normalized payload to the solver with:
- Planning horizon dates.
- Current baseline version assignments.
- People list and eligibility data.
- Group memberships and operational roles.
- Qualifications.
- Availability windows.
- Presence/base/home state windows.
- Task slots.
- Task burden levels.
- Hard constraints.
- Soft constraints with weights.
- Stability weights by time bucket.
- Fairness counters/history.
- Trigger mode (`standard` or `emergency`).

### 17.2 Output Payload

Solver output should include:
- Feasible/infeasible status.
- Assignment set for full 7-day horizon.
- Uncovered slots if any policy allows that representation.
- Hard conflict summary if infeasible.
- Soft penalty totals by category.
- Stability metrics by horizon bucket.
- Fairness metrics.
- Human-readable explanation fragments for UI summaries.

## 18. Optimization Objective Order

Recommended lexicographic or heavily weighted priority order:

1. Satisfy all hard constraints.[cite:51]
2. Maximize required staffing coverage.[cite:79]
3. Minimize changes in today + tomorrow versus baseline.[cite:88][cite:93]
4. Minimize changes in days 3–7 versus baseline.[cite:91][cite:93]
5. Minimize weighted soft-constraint violations.[cite:81][cite:84]
6. Improve fairness of burden distribution over time.[cite:81]

Important note: items 3 and 4 are intentionally separated to reflect the stronger importance of near-term stability.

## 19. Manual Override Philosophy

Do not make manual assignment editing the default workflow. Prefer admin-entered emergency facts or changes followed by automatic re-optimization.

If a true override mode is added:
- It must be explicit, dangerous-looking, and audited.
- Override-created assignments should be visually distinct.
- The system should attempt to preserve them in subsequent reruns only if policy allows.
- Use this sparingly; it is not the normal operational path.

## 20. Internationalization

Required initial languages:
- Hebrew (highest priority).
- English.
- Russian.

Requirements:
- UI string externalization from day one.
- RTL support for Hebrew.
- Date/time localization.
- Safe fallback to English keys if needed.
- Domain terminology dictionary so task/role labels are not hardcoded in only one language.

## 21. Export Requirements

MVP export targets:
- CSV export of schedules.
- Basic printable schedule view or PDF generation.

Exports should be permission-controlled and version-linked.

## 22. Non-Functional Requirements

- Secure by default.
- Multi-tenant isolation.
- Deterministic scheduling.
- Background solve support.
- Idempotent job handling.[cite:53][cite:56]
- Robust logging and observability.
- Searchable and readable schedule views.
- Extensible domain model for non-military use later.

## 23. Suggested Monorepo Structure

```text
forces-scheduler/
  apps/
    web/                  # Next.js frontend
    api/                  # ASP.NET Core API
    solver/               # Python OR-Tools service
  packages/
    contracts/            # Shared DTOs / schemas / API contracts
    ui/                   # Shared UI components if needed
    i18n/                 # Translation dictionaries
  infra/
    docker/
    compose/
    scripts/
    migrations/
  docs/
    architecture/
    api/
    product/
  .github/
  README.md
```

## 24. Delivery Phases

### Phase 1 — Foundation
- Monorepo setup.
- Auth integration.
- Space model.
- Memberships and permissions.
- PostgreSQL schema.
- RLS strategy skeleton.

### Phase 2 — Operational Data
- People.
- Roles.
- Groups.
- Tasks.
- Availability.
- Restrictions.

### Phase 3 — Scheduling Core
- Solver payload normalization.
- OR-Tools prototype.
- Hard constraints.
- Stability objective.
- Fairness counters.

### Phase 4 — Workflow
- Draft save.
- Solver queue.
- Draft version creation.
- Diff display.
- Publish and rollback.

### Phase 5 — UX and Localization
- Viewer/admin mode shell.
- Searchable schedule tables.
- Timeline/calendar view.
- Hebrew/English/Russian support.
- Logs UI.

### Phase 6 — Exports and Hardening
- CSV/PDF export.
- Observability.
- Alerts.
- Security improvements.
- Performance tuning.

### Phase 7 — Optional AI Layer
- Rule parsing assistant.
- Summary generation.
- Conflict explanation assistant.

## 25. Explicit Decisions Already Made

These decisions are already established and should not be revisited without intent:

- Solver-first architecture, not AI-first.[cite:33][cite:51]
- Full 7-day recomputation after meaningful changes.[cite:88][cite:93]
- Today + tomorrow are the most stability-sensitive window.[cite:91][cite:93]
- Later days may change more, but unnecessary changes should still be avoided.[cite:88]
- Normal users are read-only.
- Admin changes happen in draft and are then solved automatically.
- Dynamic operational roles exist per space.
- Space/group constraints may be hard or soft; individual constraints are hard by default.[cite:69]
- Rollback to previous versions is required.
- Hebrew/English/Russian support is required.
- Logs, auditability, and security are first-class requirements.

## 26. Open Product Decisions to Confirm Early

These still need explicit confirmation during implementation planning:
- Whether auth is built in-house or integrated from an existing internal auth platform.
- Whether the first MVP includes a calendar/timeline view or only advanced tables.
- Whether emergency mode can temporarily relax selected soft constraints automatically.
- Whether solver timeouts should return a best-known feasible result or fail closed.
- Whether PDF generation is required in the first milestone or CSV is enough.
- Whether presence states (`free_in_base`, `at_home`, `on_mission`) are derived or manually editable.

## 27. KIRO Execution Instructions

Use this project specification as the source of truth. When implementing:

1. Start by producing the database schema, entity diagram, and API contracts.
2. Implement multi-tenant security boundaries early, not later.[cite:49][cite:59]
3. Build the scheduling domain and versioning model before polishing UI.
4. Implement the solver with a small but real scenario first.
5. Add weighted stability objectives early so the architecture does not assume full schedule churn.[cite:88][cite:93]
6. Keep viewer/admin mode permissions explicit in both frontend and backend.
7. Prefer deterministic behavior, logging, and auditability over “smart” heuristics that are hard to reason about.
8. Treat AI features as optional extensions, not prerequisites.

## 28. Companion Prompt for KIRO

Use the following implementation brief alongside this spec:

> Build a production-minded MVP for a secure multi-tenant scheduling SaaS. Use Next.js for the frontend, ASP.NET Core for the API, PostgreSQL for the database, Redis for queue/cache, and Python + OR-Tools CP-SAT for the solver. The system must support viewer mode and permission-gated admin mode, dynamic operational roles, groups, hard/soft constraints, immutable schedule versions, rollback, logs, and multilingual support (Hebrew first, then English and Russian). The solver must always recompute the full 7-day horizon after meaningful changes, but minimize disturbance with time-weighted stability penalties: the strongest protection is for today and tomorrow, weaker but still present protection applies to later days. Normal users are read-only. Admin changes save drafts and trigger asynchronous re-optimization. Published schedules are immutable snapshots.

