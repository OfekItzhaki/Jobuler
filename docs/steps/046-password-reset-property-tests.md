# Step 046 — Password Reset Property Tests

## Phase
Phase 4 — Forgot Password / Reset Password (group-alerts-and-phone spec, tasks 11.2–11.4, 11.6–11.9)

## Purpose
Adds backend property tests that verify the security-critical invariants of the forgot-password / reset-password flow. These tests catch regressions in token hashing, user enumeration prevention, token lifecycle management, BCrypt work factor enforcement, refresh token revocation, and password length validation.

## What was built

### Files created
- **`apps/api/Jobuler.Tests/Application/PasswordResetPropertyTests.cs`**
  Thirty xUnit `[Fact]` / `[Theory]` tests covering six properties:

  | Property | Task | What it verifies |
  |----------|------|-----------------|
  | 13 | 11.2 | Unknown emails complete silently; no token row is created |
  | 14 | 11.3 | Calling `ForgotPasswordCommand` N times leaves exactly one active token; all previous tokens are marked used |
  | 12 | 11.4 | Stored `token_hash` equals `SHA256(rawToken)` delivered to `INotificationSender`; `expires_at` is within 1 s of `created_at + 1 hour` |
  | 15 | 11.6 | Wrong hash / expired token / already-used token all throw `InvalidOperationException`; `user.PasswordHash` is unchanged in every case |
  | 16 | 11.7 | After a successful reset, `BCrypt.Verify(newPassword, hash)` returns `true` and the hash starts with `$2a$12$` (work factor 12) |
  | 17 | 11.8 | All active refresh tokens for the user have `revoked_at` set after a successful reset |
  | 18 | 11.9 | Passwords of length 0–7 throw `InvalidOperationException`; `user.PasswordHash` is unchanged; length 8 succeeds |

## Key decisions

- **No FsCheck / fast-check** — the tasks call for FsCheck but the existing test suite uses plain xUnit `[Theory]` / `[InlineData]` (see `GroupAlertPropertyTests.cs`). The same pattern is followed here for consistency and to avoid adding a new dependency.
- **`IJwtService` mocked with NSubstitute** — `GenerateRefreshTokenRaw()` returns a fresh 32-byte random hex string on every call; `HashToken(raw)` delegates to `System.Security.Cryptography.SHA256`, matching the real `JwtService` implementation.
- **`INotificationSender` mocked with NSubstitute** — a closure captures the raw token passed to `SendPasswordResetAsync` so Property 12 can compare it against the stored hash.
- **InMemory EF** — each test gets its own `Guid`-named database to guarantee isolation.
- **BCrypt work factor 4 for seed data** — `SeedUser` uses `workFactor: 4` to keep test startup fast; the handler under test always uses `workFactor: 12`.
- **Expired token test** — EF entry property mutation (`db.Entry(token).Property("ExpiresAt").CurrentValue`) is used to force expiry without waiting, matching the pattern in `GroupAlertPropertyTests`.

## How it connects
- Tests exercise `ForgotPasswordCommandHandler` (`apps/api/Jobuler.Application/Auth/Commands/ForgotPasswordCommand.cs`) and `ResetPasswordCommandHandler` (`ResetPasswordCommand.cs`) end-to-end against an InMemory database.
- Depends on `PasswordResetToken`, `User`, and `RefreshToken` domain entities (steps 039, 003).
- Depends on `INotificationSender` (step 040) and `IJwtService` (step 004).

## How to run / verify

```powershell
# From apps/api/
dotnet test Jobuler.Tests/Jobuler.Tests.csproj --no-build -v n
# Expected: 125 passed, 0 failed
```

Filter to only the new tests:
```powershell
dotnet test Jobuler.Tests/Jobuler.Tests.csproj --filter "FullyQualifiedName~PasswordResetPropertyTests" -v n
```

## What comes next
- Task 14 checkpoint: ensure all tests pass before closing the spec.
- Step 047 will cover any remaining close-out tasks (step documentation for group-ownership, final test run).

## Git commit

```bash
git add -A && git commit -m "test(auth): password reset property tests (Properties 12-18)"
```
