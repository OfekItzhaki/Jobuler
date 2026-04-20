---
inclusion: always
---

# Security Rules

These rules are always enforced. Security is never deferred.

## Authentication

- All endpoints except `/auth/register`, `/auth/login`, `/auth/refresh`, and `/health` MUST require `[Authorize]`.
- JWT access tokens expire in 15 minutes. Refresh tokens expire in 7 days and rotate on every use.
- Revoked refresh tokens remain in the DB for audit purposes — never delete them.
- BCrypt work factor MUST be 12 or higher. Never use MD5, SHA1, or plain text for passwords.
- Never log passwords, tokens, or sensitive personal data.

## Authorization

- Permission checks MUST happen in the Application layer via `IPermissionService`, not in controllers or the DB layer.
- Space owners implicitly hold all permissions — this is enforced in `PermissionService`, not duplicated elsewhere.
- Sensitive data (restriction reasons, sensitive logs) requires a separate elevated permission check — `restrictions.manage_sensitive` or `logs.view_sensitive`.
- Never return sensitive fields unless the caller's permission has been explicitly verified in the same request.

## Tenant isolation

- Every DB query on a tenant-scoped table MUST include `space_id` in the WHERE clause.
- PostgreSQL RLS is the last line of defense — the application layer must also filter by space.
- Never expose one tenant's data in another tenant's response, even partially.
- Cross-space queries are forbidden unless the caller is a system-level admin (not implemented in MVP).

## Input validation

- All API request bodies MUST be validated before dispatching a command.
- Use FluentValidation for command validation in the Application layer.
- Reject requests with unexpected or malformed JSON payloads at the middleware level.
- Never trust client-supplied IDs without verifying they belong to the current space.

## Transport

- All production traffic MUST use TLS. HTTP is only acceptable in local Docker dev.
- Never log full request/response bodies in production — log correlation IDs and status codes only.

## Audit trail

- The following actions MUST produce an audit log entry: publish, rollback, permission grant/revoke, ownership transfer, sensitive restriction create/update/delete, login failure (repeated).
- Audit log rows are append-only. Never update or delete them.
- Audit logs MUST include: actor_user_id, space_id, action, entity_type, entity_id, before/after snapshot where applicable, ip_address, correlation_id, timestamp.

## Secrets

- Never hardcode secrets, API keys, or connection strings in source code.
- All secrets come from environment variables or a secrets manager.
- The `.env` file is gitignored. Only `.env.example` with placeholder values is committed.
