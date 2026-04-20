# Step 008 — Seed Data + Migration Scripts

## Phase
Phase 1 — Foundation

## Purpose
Provide a realistic demo space with users, roles, groups, people, and task types so developers can run the system immediately without manual setup. Also provides shell scripts to run migrations and seed data against the local postgres container.

## What was built

| File | Description |
|---|---|
| `infra/scripts/seed.sql` | Demo space "מחלקה א׳" with 4 users, 6 people, 4 roles, 2 group types, 2 groups, 6 task types |
| `infra/scripts/migrate.sh` | Runs all `infra/migrations/*.sql` files in order |
| `infra/scripts/seed.sh` | Loads `seed.sql` into the local postgres instance |

## Demo accounts

| Email | Password | Role |
|---|---|---|
| `admin@demo.local` | `Demo1234!` | Space owner — all permissions |
| `ofek@demo.local` | `Demo1234!` | Member — no explicit permissions (viewer) |
| `yael@demo.local` | `Demo1234!` | Member — no explicit permissions (viewer) |
| `viewer@demo.local` | `Demo1234!` | Member — `space.view` only |

## Demo task types

| Name | Burden Level |
|---|---|
| עמדה 1 | neutral |
| עמדה 2 | neutral |
| מטבח | disliked |
| חדר מלחמה | hated |
| סיור | disliked |
| מילואים | favorable |

## Key decisions

### Fixed UUIDs for seed data
All seed records use fixed UUIDs (e.g. `00000000-0000-0000-0000-000000000001`). This makes it easy to reference them in tests and curl commands without querying the DB first.

### BCrypt hash pre-computed
The password hash for `Demo1234!` is pre-computed and embedded in the SQL. This avoids a dependency on the API being running during seeding.

### `ON CONFLICT DO NOTHING`
All inserts use `ON CONFLICT DO NOTHING` so the seed script is idempotent — safe to run multiple times.

## How to run / verify

```bash
# Make scripts executable (Linux/Mac)
chmod +x infra/scripts/migrate.sh infra/scripts/seed.sh

# Run migrations
./infra/scripts/migrate.sh

# Load seed data
./infra/scripts/seed.sh

# Verify
docker compose -f infra/compose/docker-compose.yml exec postgres \
  psql -U jobuler -d jobuler -c "SELECT email, display_name FROM users;"
```

## What comes next
- Phase 2 seed additions: availability windows, restrictions, task slots for the demo space
- Phase 3 seed additions: constraint rules and a baseline schedule version for solver testing
