# Step 034 — Group Alerts: Migration and Domain Entity

## Phase

Phase 8 — Group Alerts and Phone

## Purpose

Lay the database and domain foundation for the Group Alerts feature. This step creates the `group_alerts` table, the `GroupAlert` domain entity with its `AlertSeverity` enum, and wires the entity into `AppDbContext` with EF Fluent API configuration. Subsequent steps build the application-layer commands and the API endpoints on top of this foundation.

## What was built

| File | Action | Description |
|------|--------|-------------|
| `infra/migrations/012_group_alerts.sql` | Created | Adds the `group_alerts` table with all required columns, FK constraints to `spaces`, `groups`, and `people`, and a composite index on `(space_id, group_id, created_at DESC)` for efficient per-group listing |
| `apps/api/Jobuler.Domain/Groups/GroupAlert.cs` | Created | `GroupAlert` entity implementing `Entity` and `ITenantScoped`; `AlertSeverity` enum (`Info`, `Warning`, `Critical`); static factory `GroupAlert.Create(...)` |
| `apps/api/Jobuler.Application/Persistence/AppDbContext.cs` | Modified | Added `public DbSet<GroupAlert> GroupAlerts => Set<GroupAlert>();` under the Groups section |
| `apps/api/Jobuler.Infrastructure/Persistence/Configurations/GroupAlertConfiguration.cs` | Created | EF Fluent API mapping to `group_alerts` table — column names, max lengths, string conversion for `Severity` enum |

## Key decisions

- `AlertSeverity` is stored as a `VARCHAR(20)` string in the DB (via `HasConversion<string>()`) rather than an integer, keeping the column human-readable in raw SQL queries.
- No data annotations on the domain entity — all constraints live in `GroupAlertConfiguration` per the architecture rules.
- `GroupAlert.Create` trims `title` and `body` at the domain level so whitespace-only values are caught by the Application-layer validator before reaching the DB.
- Migration numbered 012 — 011 is `group_messages` (already exists). Numbers must not be reused.

## How it connects

- `GroupAlert` implements `ITenantScoped` (requires `SpaceId`) — every query against this table must include `space_id` in the WHERE clause, enforced by the Application-layer handlers in the next step.
- `GroupAlertConfiguration` is picked up automatically by `AppDbContext.OnModelCreating` via `ApplyConfigurationsFromAssembly(ConfigurationAssembly)` — no manual registration needed.
- The migration SQL is applied by the database init script alongside all other numbered migrations.

## How to run / verify

1. Apply the migration against a local Postgres instance:
   ```sql
   \i infra/migrations/012_group_alerts.sql
   ```
2. Confirm the table and index exist:
   ```sql
   \d group_alerts
   \di idx_group_alerts_group
   ```
3. Build the backend to confirm no compile errors:
   ```bash
   dotnet build --no-restore
   ```
   All four projects (`Domain`, `Application`, `Infrastructure`, `Tests`) should report `succeeded`.

## What comes next

- Task 5.1 — `GroupAlertDto`, `CreateGroupAlertCommand`, `GetGroupAlertsQuery`, `DeleteGroupAlertCommand` with handlers and validators
- Task 6.1 — Alert endpoints in `GroupsController`
- Task 7.x — Frontend alerts tab

## Git commit

```bash
git add -A && git commit -m "feat(group-alerts): migration 012, GroupAlert domain entity, EF configuration"
```
