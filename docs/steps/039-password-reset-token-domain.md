# Step 039 — Password Reset Token: Migration, Domain Entity, and EF Registration

## Phase
Phase 9 — Password Reset Flow

## Purpose
Lay the data foundation for password reset: a dedicated `password_reset_tokens` table, a `PasswordResetToken` domain entity, a `SetPasswordHash` method on `User`, and the EF Core wiring to persist it all.

## What was built

| File | Change |
|------|--------|
| `infra/migrations/013_password_reset_tokens.sql` | New migration — creates `password_reset_tokens` table with `id`, `user_id` (FK → users), `token_hash` (unique), `created_at`, `expires_at`, `used_at`; adds two indexes |
| `apps/api/Jobuler.Domain/Identity/PasswordResetToken.cs` | New domain entity extending `Entity`; exposes `IsExpired`, `IsUsed`, `IsValid` computed properties; `Create` factory sets a 1-hour expiry; `MarkUsed` stamps `UsedAt` |
| `apps/api/Jobuler.Domain/Identity/User.cs` | Added `SetPasswordHash(string hash)` method — updates `PasswordHash` and calls `Touch()` |
| `apps/api/Jobuler.Application/Persistence/AppDbContext.cs` | Added `DbSet<PasswordResetToken> PasswordResetTokens` under the Identity section |
| `apps/api/Jobuler.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs` | New EF Fluent API configuration — maps to `password_reset_tokens`, column names, unique index on `token_hash` |

## Key decisions
- `PasswordResetToken` extends `Entity` (not `AuditableEntity`) — same pattern as `RefreshToken`; no `UpdatedAt` needed since tokens are immutable after creation.
- Token validity is 1 hour, enforced in the domain (`ExpiresAt = DateTime.UtcNow.AddHours(1)`).
- `IsValid` is a pure computed property — no DB column, no EF mapping needed.
- `SetPasswordHash` lives on `User` to keep password mutation inside the aggregate, consistent with the existing `UpdatePhone` pattern.

## How it connects
- The `PasswordResetToken` entity will be used by upcoming `RequestPasswordResetCommand` and `ResetPasswordCommand` handlers.
- `AppDbContext.PasswordResetTokens` gives handlers direct access without any extra repository abstraction.
- `PasswordResetTokenConfiguration` is auto-discovered by `modelBuilder.ApplyConfigurationsFromAssembly(ConfigurationAssembly)` in `AppDbContext.OnModelCreating`.

## How to run / verify
1. Apply the migration against your local Postgres instance:
   ```bash
   psql $DATABASE_URL -f infra/migrations/013_password_reset_tokens.sql
   ```
2. Build the solution:
   ```bash
   dotnet build apps/api --no-restore
   ```
   Domain, Application, Infrastructure, and Tests all compile cleanly.

## What comes next
- Task 9.5: `RequestPasswordResetCommandHandler` — generates a secure token, hashes it, persists a `PasswordResetToken`, and sends the reset email.
- Task 9.6: `ResetPasswordCommandHandler` — validates the token, calls `user.SetPasswordHash(newHash)`, and marks the token used.

## Git commit
```bash
git add -A && git commit -m "feat(phase9): password reset token migration, domain entity, and EF registration"
```
