# Step 005 — Spaces API + Permission Service

## Phase
Phase 1 — Foundation

## Purpose
Implement the core multi-tenant workspace API: create spaces, manage memberships, grant/revoke permissions, transfer ownership. This is the foundation of all tenant isolation — every subsequent feature is scoped to a space.

## What was built

| File | Description |
|---|---|
| `apps/api/Jobuler.Domain/Spaces/Space.cs` | Space aggregate with factory method and ownership transfer |
| `apps/api/Jobuler.Domain/Spaces/SpaceMembership.cs` | Links users to spaces |
| `apps/api/Jobuler.Domain/Spaces/SpacePermissionGrant.cs` | Per-user per-space permission grants; `Permissions` static class with all known keys |
| `apps/api/Jobuler.Domain/Spaces/SpaceRole.cs` | Dynamic operational roles (data, not enums) |
| `apps/api/Jobuler.Domain/Spaces/OwnershipTransferHistory.cs` | Immutable log of every ownership change |
| `apps/api/Jobuler.Application/Spaces/Commands/CreateSpaceCommand.cs` | Creates space + auto-grants all permissions to owner |
| `apps/api/Jobuler.Application/Spaces/Commands/TransferOwnershipCommand.cs` | Transfers ownership + writes history record |
| `apps/api/Jobuler.Application/Spaces/Queries/GetSpaceQuery.cs` | Fetch single space |
| `apps/api/Jobuler.Application/Spaces/Queries/GetMySpacesQuery.cs` | List all spaces the current user belongs to |
| `apps/api/Jobuler.Api/Controllers/SpacesController.cs` | `GET /spaces`, `GET /spaces/{id}`, `POST /spaces`, `POST /spaces/{id}/transfer-ownership` |
| `apps/api/Jobuler.Application/Common/IPermissionService.cs` | Interface for permission checks |
| `apps/api/Jobuler.Infrastructure/Auth/PermissionService.cs` | Checks DB grants; space owner always has all permissions |
| `apps/api/Jobuler.Infrastructure/Persistence/AppDbContext.cs` | EF Core DbContext with all entity sets |
| `apps/api/Jobuler.Infrastructure/Persistence/Configurations/*.cs` | Fluent API mappings for all entities |

## Key decisions

### Owner always has all permissions
`PermissionService` checks `spaces.owner_user_id` first. If the requesting user is the owner, all permission checks pass without hitting the grants table. This prevents lockout scenarios.

### Auto-grant on space creation
When a user creates a space, they automatically receive all 12 permission keys. This is done atomically in `CreateSpaceCommandHandler` within a single `SaveChangesAsync` call.

### Ownership transfer is atomic + logged
`TransferOwnershipCommand` updates `spaces.owner_user_id` and inserts into `ownership_transfer_history` in the same transaction. The history record is immutable — it is never updated or deleted.

### Dynamic operational roles
`SpaceRole` entities are created by admins per space. They are not hardcoded enums anywhere in the codebase. The solver receives role IDs as strings in its input payload.

## How it connects
- All subsequent domain features (people, tasks, constraints, scheduling) are scoped to a `space_id`.
- `IPermissionService` is injected into every command/query handler that requires elevated access.
- `TenantContextMiddleware` (Step 004) sets the PostgreSQL session variable that activates RLS on these tables.

## How to run / verify

```bash
# With API running and seed data loaded:

# Login as admin
TOKEN=$(curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.local","password":"Demo1234!"}' | jq -r .accessToken)

# List my spaces
curl http://localhost:5000/spaces -H "Authorization: Bearer $TOKEN"

# Get specific space
curl http://localhost:5000/spaces/10000000-0000-0000-0000-000000000001 \
  -H "Authorization: Bearer $TOKEN"

# Create a new space
curl -X POST http://localhost:5000/spaces \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Space","locale":"he"}'
```

## What comes next
- Step 006: Frontend shell calls `/spaces` to populate the space selector
- Phase 2: People, groups, tasks APIs all use `IPermissionService` for access control
