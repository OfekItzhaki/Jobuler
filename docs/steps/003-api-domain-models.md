# Step 003 — API Domain Models

## Phase
Phase 1 — Foundation

## Purpose
Define the core C# domain entities and project structure for the ASP.NET Core API using a clean layered architecture (Domain → Application → Infrastructure → Api). These models are the in-memory representation of the database schema and the foundation for all business logic.

## What was built

| File | Description |
|---|---|
| `apps/api/Jobuler.sln` | Solution file linking all four projects |
| `apps/api/Jobuler.Domain/Jobuler.Domain.csproj` | Domain project — no external dependencies |
| `apps/api/Jobuler.Application/Jobuler.Application.csproj` | Application project — MediatR + FluentValidation |
| `apps/api/Jobuler.Infrastructure/Jobuler.Infrastructure.csproj` | Infrastructure — EF Core + Npgsql + Redis + BCrypt |
| `apps/api/Jobuler.Api/Jobuler.Api.csproj` | Web API — JWT auth + Swagger + Serilog |
| `apps/api/Jobuler.Domain/Common/Entity.cs` | Base `Entity` and `AuditableEntity` classes |
| `apps/api/Jobuler.Domain/Common/ITenantScoped.cs` | Interface marking all tenant-owned entities |
| `apps/api/Jobuler.Domain/Identity/User.cs` | `User` domain entity with factory method |
| `apps/api/Jobuler.Domain/Identity/RefreshToken.cs` | `RefreshToken` entity with expiry/revocation logic |

## Key decisions

### Four-layer architecture
```
Jobuler.Domain        — entities, value objects, domain interfaces (no dependencies)
Jobuler.Application   — use cases, commands/queries (MediatR), validation (FluentValidation)
Jobuler.Infrastructure — EF Core DbContext, repositories, Redis, external services
Jobuler.Api           — controllers, middleware, DI wiring, Swagger
```
This keeps business rules in Domain/Application, testable without a database or HTTP context.

### Factory methods on entities
Entities use private constructors and static `Create()` factory methods. This prevents invalid state — you cannot construct a `User` without required fields.

### ITenantScoped interface
Every entity that belongs to a space implements `ITenantScoped`. This makes it easy to write generic repository helpers that automatically filter by `SpaceId` and to audit which entities are tenant-scoped vs global.

### No data annotations on domain entities
Domain entities are clean of EF Core attributes. Mapping is done in `Infrastructure` via Fluent API configurations. This keeps the domain layer free of infrastructure concerns.

## How it connects
- Domain entities map 1:1 to the PostgreSQL tables defined in Step 002.
- Application layer commands/queries (added in later steps) use these entities.
- Infrastructure layer EF Core configurations map these entities to the DB schema.
- API controllers call Application layer handlers, never touching domain entities directly.

## How to run / verify

```bash
cd apps/api
dotnet build Jobuler.sln
# Should build with 0 errors
```

## What comes next
- Step 004: Auth (JWT), EF Core DbContext, TenantContextMiddleware, Spaces API
- Remaining domain entities (Space, People, Tasks, etc.) are added in Step 004 alongside their API endpoints
