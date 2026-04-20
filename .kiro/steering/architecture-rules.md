---
inclusion: always
---

# Architecture Rules

These rules are always enforced. Do not deviate without explaining the conflict first.

## Layering

The API follows a strict 4-layer architecture. Dependencies only flow inward:

```
Api → Application → Domain
Infrastructure → Application → Domain
```

- `Domain` has zero external dependencies. No EF Core, no MediatR, no HTTP.
- `Application` depends only on `Domain` and interface contracts. No EF Core directly.
- `Infrastructure` implements interfaces defined in `Application` and `Domain`.
- `Api` wires everything together via DI. Controllers call MediatR, never repositories directly.

Violations to flag:
- EF Core types (`DbContext`, `DbSet`) appearing in `Domain` or `Application` handlers directly — use the `AppDbContext` only in handlers that are in `Application` and have it injected, or move DB access to a repository in `Infrastructure`.
- Domain entities with data annotations (`[Required]`, `[MaxLength]`) — use Fluent API in `Infrastructure` instead.
- Business logic in controllers — controllers dispatch commands/queries only.

## Multi-tenancy

- Every tenant-scoped entity MUST implement `ITenantScoped`.
- Every query against a tenant-scoped table MUST include a `space_id` filter.
- Never query across spaces in a single request.
- `TenantContextMiddleware` MUST run after `UseAuthentication` and before any controller logic.
- PostgreSQL session variables `app.current_space_id` and `app.current_user_id` MUST be set before any RLS-protected query runs.

## Immutability

- Published `schedule_versions` are never updated in place.
- `assignments` rows are never mutated after their version is published.
- Rollback = create a new version with `rollback_source_version_id` set. Never delete or update old versions.
- Audit log rows are append-only. Never update or delete them.

## Permission checks

- Every controller action that writes data MUST call `IPermissionService.RequirePermissionAsync` before dispatching the command.
- Every controller action that reads sensitive data MUST check the relevant sensitive permission and pass the result into the query.
- Never trust the frontend to enforce permissions. All checks happen server-side.

## Solver

- The solver service is stateless. All context is sent in the input payload.
- The API never calls the solver synchronously from a controller. Always via the job queue.
- The solver result is stored as a draft version. It is never directly published without admin review.
- Stability weights MUST be included in every solver payload. Never hardcode them in the solver.

## Error handling

- All exceptions bubble up to `ExceptionHandlingMiddleware`. Do not catch-and-swallow in handlers.
- `UnauthorizedAccessException` → 403. `KeyNotFoundException` → 404. `InvalidOperationException` → 400.
- Never return stack traces to clients.
- Log all 500-level errors with full context via Serilog.
