# Step 004 — Auth: JWT + Refresh Token Rotation

## Phase
Phase 1 — Foundation

## Purpose
Implement in-house authentication using BCrypt password hashing, short-lived JWT access tokens (15 min), and rotating refresh tokens (7 days). No external auth provider — full control over session policy, audit trail, and sensitive data handling.

## What was built

| File | Description |
|---|---|
| `apps/api/Jobuler.Infrastructure/Auth/JwtService.cs` | Generates/validates JWTs, generates refresh tokens, hashes tokens with SHA-256 |
| `apps/api/Jobuler.Application/Auth/Commands/LoginCommand.cs` | Login command + result record |
| `apps/api/Jobuler.Application/Auth/Commands/LoginCommandHandler.cs` | Validates credentials, issues token pair, records login timestamp |
| `apps/api/Jobuler.Application/Auth/Commands/RegisterCommand.cs` | Register command |
| `apps/api/Jobuler.Application/Auth/Commands/RegisterCommandHandler.cs` | Creates user with BCrypt hash (work factor 12) |
| `apps/api/Jobuler.Application/Auth/Commands/RefreshTokenCommand.cs` | Refresh command |
| `apps/api/Jobuler.Application/Auth/Commands/RefreshTokenCommandHandler.cs` | Validates refresh token, rotates it, issues new pair |
| `apps/api/Jobuler.Application/Auth/Commands/RevokeTokenCommand.cs` | Logout — revokes refresh token |
| `apps/api/Jobuler.Api/Controllers/AuthController.cs` | `POST /auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout` |
| `apps/api/Jobuler.Api/Middleware/TenantContextMiddleware.cs` | Sets `app.current_user_id` and `app.current_space_id` PostgreSQL session vars for RLS |
| `apps/api/Jobuler.Api/Middleware/ExceptionHandlingMiddleware.cs` | Converts exceptions to consistent JSON error responses |
| `apps/api/Jobuler.Api/Program.cs` | Full DI wiring, JWT bearer config, Serilog, Swagger, CORS, middleware pipeline |
| `apps/api/Jobuler.Api/appsettings.json` | Default config (JWT, DB, Redis, Solver, Serilog) |

## Key decisions

### In-house auth (no external provider)
Chosen for full control over session policy, audit trail, and sensitive data. No vendor dependency. BCrypt work factor 12 balances security and login latency.

### Refresh token rotation
Every refresh call revokes the old token and issues a new one. This limits the window of exposure if a refresh token is stolen. Revoked tokens remain in the DB for audit purposes.

### Token storage
Access token: `Authorization: Bearer` header. Refresh token: sent in request body (not cookie) to avoid CSRF complexity at this stage. Can be moved to HttpOnly cookie in a hardening pass.

### TenantContextMiddleware placement
Runs after `UseAuthentication` and `UseAuthorization` so the user identity is already resolved when we set the PostgreSQL session variables. This ensures RLS policies have both `app.current_user_id` and `app.current_space_id` available.

### ExceptionHandlingMiddleware
Runs first in the pipeline so all exceptions — including auth failures — are caught and returned as consistent JSON. Stack traces never reach the client in production.

## How it connects
- `TenantContextMiddleware` activates the RLS policies defined in Step 002.
- `PermissionService` (Step 004b) uses the DB to check per-space permission grants.
- All subsequent API controllers depend on the JWT bearer auth configured here.
- The frontend auth store (Step 006) calls these endpoints and stores tokens.

## How to run / verify

```bash
# Start postgres + API
docker compose -f infra/compose/docker-compose.yml up -d postgres
cd apps/api && dotnet run --project Jobuler.Api

# Register
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","displayName":"Test","password":"Test1234!"}'

# Login
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test1234!"}'
# Returns: accessToken, refreshToken, userId, displayName, preferredLocale

# Health check (no auth required)
curl http://localhost:5000/health
```

## What comes next
- Step 005: Spaces API uses `IPermissionService` for permission-gated operations
- Step 006: Frontend auth store calls these endpoints
- Step 008: Audit logging hooks into auth events
