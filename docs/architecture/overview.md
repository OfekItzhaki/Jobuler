# Architecture Overview

## System Components

```
┌─────────────────────────────────────────────────────────────────┐
│  Browser                                                        │
│  Next.js 14 + TypeScript                                        │
│  - Viewer mode (read-only, default)                             │
│  - Admin mode (permission-gated)                                │
│  - Hebrew RTL / English / Russian                               │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTPS / JWT Bearer
┌────────────────────────▼────────────────────────────────────────┐
│  ASP.NET Core 8 API                                             │
│  - Auth (JWT + refresh token rotation)                          │
│  - Permission enforcement (IPermissionService)                  │
│  - Tenant context middleware (sets PostgreSQL session vars)      │
│  - CRUD: spaces, people, groups, tasks, constraints             │
│  - Draft workflow: save → enqueue → review → publish/rollback   │
│  - Audit + system log generation                                │
└──────┬──────────────────────────────────────┬───────────────────┘
       │ Npgsql / EF Core                     │ Redis queue
┌──────▼──────────────┐              ┌────────▼────────────────────┐
│  PostgreSQL 16      │              │  Solver Worker              │
│  - RLS on all       │              │  (background service)       │
│    tenant tables    │              │  - Dequeues solve jobs      │
│  - Immutable        │              │  - Calls Python solver      │
│    schedule         │              │  - Stores draft version     │
│    versions         │◄─────────────│  - Writes diff + metrics    │
│  - Full audit trail │              └────────┬────────────────────┘
└─────────────────────┘                       │ HTTP POST /solve
                                   ┌──────────▼──────────────────┐
                                   │  Python Solver Service      │
                                   │  OR-Tools CP-SAT            │
                                   │  - Stateless                │
                                   │  - Hard constraints         │
                                   │  - Stability objective      │
                                   │  - Fairness objective       │
                                   │  - Returns best-known on    │
                                   │    timeout                  │
                                   └─────────────────────────────┘
```

## Multi-Tenancy Model

Every tenant-scoped table has:
1. A `space_id` column
2. PostgreSQL Row-Level Security enabled
3. An RLS policy that checks `current_setting('app.current_space_id')`

The API's `TenantContextMiddleware` sets this session variable from the route parameter on every request. Even if application-layer permission checks have a bug, the database will not return another tenant's rows.

## Schedule Versioning Model

```
schedule_versions (immutable snapshots)
  ├── status: draft | published | rolled_back | archived
  ├── baseline_version_id → previous version
  ├── source_run_id → the solver run that produced this version
  └── assignments[] → owned by this version, never mutated

Rollback = create new version with rollback_source_version_id pointing to old version
Publish  = set status = published, record published_at + published_by
```

## Permission Model

```
All users → viewer mode by default (space.view)
           ↓ explicit grant required
Admin mode → space.admin_mode
           ↓ further grants required per action
Publish    → schedule.publish
Rollback   → schedule.rollback
Sensitive  → restrictions.manage_sensitive
Ownership  → ownership.transfer
```

## Solver Flow

```
Admin saves changes
  → API validates + stores
  → API enqueues SolverJob (Redis)
  → SolverWorker dequeues
  → Normalizes planning data into SolverInput
  → POST /solve to Python service
  → Python CP-SAT runs with timeout
  → Returns SolverOutput (assignments + metrics + explanation)
  → Worker stores schedule_version (draft) + assignments
  → Worker computes diff vs baseline
  → Admin reviews draft in UI
  → Admin publishes or rolls back
```

## Stability Weights

| Time window | Penalty weight |
|---|---|
| Today + tomorrow | 10.0 (very high) |
| Days 3–4 | 3.0 (medium) |
| Days 5–7 | 1.0 (low) |

These are sent in every solver payload and can be tuned per space without redeploying.
