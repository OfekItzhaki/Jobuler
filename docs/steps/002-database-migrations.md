# Step 002 — Database Migrations

## Phase
Phase 1 — Foundation

## Purpose
Define the complete PostgreSQL schema for the entire domain — identity, operational data, tasks, scheduling, logs — with multi-tenant Row-Level Security (RLS) enforced at the database layer from day one.

## What was built

| File | Description |
|---|---|
| `infra/migrations/000_extensions.sql` | Enables `uuid-ossp`, `pg_trgm`, `btree_gin` extensions |
| `infra/migrations/001_core_identity.sql` | `users`, `refresh_tokens`, `spaces`, `space_memberships`, `space_permission_grants`, `space_roles`, `ownership_transfer_history` + RLS policies |
| `infra/migrations/002_operational_data.sql` | `group_types`, `groups`, `group_memberships`, `people`, `person_role_assignments`, `person_qualifications`, `availability_windows`, `presence_windows`, `person_restrictions`, `sensitive_restriction_reasons` + RLS |
| `infra/migrations/003_tasks_and_constraints.sql` | `task_types`, `task_type_overlap_rules`, `task_slots`, `constraint_rules` + RLS |
| `infra/migrations/004_scheduling.sql` | `schedule_runs`, `schedule_versions`, `assignments`, `assignment_change_summaries`, `fairness_counters` + RLS |
| `infra/migrations/005_logs_and_exports.sql` | `audit_logs`, `system_logs`, `exports` + RLS |

## Key decisions

### Multi-tenancy via RLS
Every tenant-scoped table has RLS enabled. Policies check `current_setting('app.current_space_id', TRUE)::UUID`. The application sets this session variable in a middleware before any query runs. This means even if application-layer permission checks have a bug, the database will not return another tenant's rows.

### Presence state hybrid model
`presence_windows` has an `is_derived` boolean. `on_mission` state is auto-derived from assignments (set by the solver worker). `at_home` and `free_in_base` are manually set by admins. This matches real operational practice.

### Sensitive restriction separation
`person_restrictions` holds the operational note (visible to normal admins). `sensitive_restriction_reasons` is a separate table requiring `restrictions.manage_sensitive` permission — enforced in the application layer on top of RLS space isolation.

### Immutable schedule versions
`assignments` rows reference `schedule_version_id`. Once a version is published, its assignments are never updated. Rollback creates a new version row pointing back to the old one via `rollback_source_version_id`.

### Circular FK between schedule_runs and schedule_versions
`schedule_runs.baseline_version_id` references `schedule_versions`, and `schedule_versions.source_run_id` references `schedule_runs`. The FK from `schedule_runs` is added via `ALTER TABLE` after both tables exist to resolve the circular dependency.

### updated_at trigger
A single `set_updated_at()` PL/pgSQL function is reused across all tables that need it, keeping the trigger logic DRY.

## How it connects
- The API's EF Core `DbContext` maps to these tables.
- The solver reads normalized planning data from these tables (via the API payload).
- RLS policies are activated by `TenantContextMiddleware` in the API (Step 004).
- Seed data (Step 007) inserts into these tables.

## How to run / verify

```bash
# With postgres container running:
docker compose -f infra/compose/docker-compose.yml exec postgres psql -U jobuler -d jobuler

# Run migrations in order:
\i /docker-entrypoint-initdb.d/000_extensions.sql
\i /docker-entrypoint-initdb.d/001_core_identity.sql
# ... etc

# Verify RLS is active:
SELECT tablename, rowsecurity FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename;
# All tenant tables should show rowsecurity = true
```

## What comes next
- Step 003: ASP.NET Core domain models map to these tables
- Step 004: API auth and tenant middleware sets `app.current_space_id`
- Step 007: Seed script populates demo data into these tables
