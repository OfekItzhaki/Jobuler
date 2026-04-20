# Step 013 — Phase 6: Exports, Validation, Rate Limiting, Observability

## Phase
Phase 6 — Exports and Hardening

## Purpose
Harden the system for production: add FluentValidation on all commands, rate limiting on auth and API endpoints, CSV export, audit logging on critical actions, and structured system logging from the solver worker.

## What was built

### Validation

| File | Description |
|---|---|
| `Application/Common/ValidationBehavior.cs` | MediatR pipeline behavior — runs all registered validators before every command handler |
| `Auth/Validators/LoginCommandValidator.cs` | Email format, password length |
| `Auth/Validators/RegisterCommandValidator.cs` | Email, display name, password strength (uppercase + digit), locale whitelist |
| `Spaces/Validators/CreateSpaceCommandValidator.cs` | Name length, locale whitelist |
| `People/Validators/CreatePersonCommandValidator.cs` | Full name, display name, restriction date ordering |
| `Tasks/Validators/CreateTaskTypeCommandValidator.cs` | Name, priority range (1–10), slot time ordering, headcount range |
| `Constraints/Validators/CreateConstraintCommandValidator.cs` | Rule type, valid JSON payload, date ordering |

### Rate limiting

Added to `Program.cs`:
- `auth` policy: 10 requests/minute per IP — applied to `AuthController`
- `api` policy: 200 requests/minute per IP — global fallback
- Returns HTTP 429 on breach

### Audit logging

| File | Description |
|---|---|
| `Application/Common/IAuditLogger.cs` | Interface for append-only audit entries |
| `Infrastructure/Logging/AuditLogger.cs` | Writes `AuditLog` rows to DB |
| `PublishVersionCommand.cs` | Now writes `publish_schedule` audit entry |
| `RollbackVersionCommand.cs` | Now writes `rollback_schedule` audit entry |

### System logging

| File | Description |
|---|---|
| `Infrastructure/Logging/SystemLogger.cs` | Writes `SystemLog` rows to DB + emits to Serilog for external observability |
| `SolverWorkerService.cs` | Logs `solver_completed`, `solver_infeasible`, `solver_failed` events after every run |

### CSV Export

| File | Description |
|---|---|
| `Application/Exports/Commands/ExportScheduleCsvCommand.cs` | Builds CSV from assignments joined with person + task names; version-linked |
| `Api/Controllers/ExportsController.cs` | `GET /spaces/{id}/exports/{versionId}/csv` — requires `schedule.publish` permission |

## Key decisions

### ValidationBehavior runs before every command
Registered as a MediatR pipeline behavior. All validators in the Application assembly are auto-discovered via `AddValidatorsFromAssembly`. No manual wiring per command needed.

### ValidationException → 400 with error list
`ExceptionHandlingMiddleware` now catches `FluentValidation.ValidationException` and returns a structured `{ error, errors[] }` response. Clients get field-level error messages.

### Rate limiting is IP-based
Uses ASP.NET Core's built-in `AddRateLimiter` with fixed window policies. Auth endpoints get a strict 10 req/min limit to prevent brute force. The general API limit is 200 req/min.

### Audit logs are append-only
`AuditLogger` only calls `_db.AuditLogs.Add()` — never `Update()` or `Remove()`. The DB table has no update trigger. This is enforced by the domain entity having no setters.

### CSV export is synchronous for MVP
The export is generated in-memory and streamed directly. For large spaces this could be slow — Phase 6+ extension point is to make it async with S3 storage and a download link.

## How to run / verify

```bash
# Test validation — should return 400 with errors array
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"not-an-email","displayName":"x","password":"weak"}'

# Test rate limiting — run 11 times quickly on auth endpoint
for i in {1..11}; do curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test1234!"}'; done
# Last request should return 429

# Export CSV (requires auth + schedule.publish permission)
curl "http://localhost:5000/spaces/$SPACE/exports/$VERSION_ID/csv" \
  -H "Authorization: Bearer $TOKEN" -o schedule.csv
```

## What comes next
- Phase 7 (optional): AI assistant layer for natural language rule parsing and schedule summaries
- Production hardening: HTTPS enforcement, secrets manager, container health checks, AWS deployment

## Git commit

```bash
git add -A && git commit -m "feat(phase6): validation, rate limiting, audit logs, CSV export, observability"
```
