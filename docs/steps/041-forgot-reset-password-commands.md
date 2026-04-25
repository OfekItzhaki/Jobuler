# Step 041 — ForgotPassword and ResetPassword Commands

## Phase
Phase 8 — Auth: Password Reset Flow (Application Layer)

## Purpose
Implements the two MediatR commands that power the password reset flow:
- `ForgotPasswordCommand` — looks up a user by email, invalidates any existing active reset tokens, generates a new hashed token, and delivers it via `INotificationSender` (preferring phone number over email).
- `ResetPasswordCommand` — validates the raw token against its stored SHA-256 hash, re-hashes the new password with BCrypt (work factor 12), marks the token used, and revokes all active refresh tokens for the user.

## What was built

| File | Description |
|------|-------------|
| `apps/api/Jobuler.Application/Auth/Commands/ForgotPasswordCommand.cs` | Command record + handler. Silently no-ops for unknown emails (prevents user enumeration). Invalidates existing active tokens before issuing a new one. Delivers via `INotificationSender`. |
| `apps/api/Jobuler.Application/Auth/Commands/ResetPasswordCommand.cs` | Command record + FluentValidation validator (token not-empty, password ≥ 8 chars) + handler. Verifies token hash, updates password, marks token used, revokes all refresh tokens. |

## Key decisions

- **No email enumeration**: `ForgotPasswordCommandHandler` returns silently when the email is not found — the caller cannot distinguish "email not found" from "email sent".
- **Token delivery preference**: Phone number is preferred over email when both are present, matching the spec's WhatsApp/SMS-first design.
- **Hash-only storage**: Only the SHA-256 hash of the raw token is persisted (`IJwtService.HashToken`). The raw token is sent to the user and never stored.
- **Token invalidation on reuse**: Any existing unexpired/unused tokens are marked used before a new one is issued, preventing token accumulation.
- **Refresh token revocation on password reset**: All active refresh tokens are revoked after a successful password reset, forcing re-authentication on all devices.
- **BCrypt work factor 12**: Consistent with the rest of the auth layer.
- **Validator + handler guard**: Password length is checked both in `ResetPasswordCommandValidator` (FluentValidation pipeline) and defensively in the handler itself.

## How it connects

- Depends on `PasswordResetToken` domain entity (step 039) and `INotificationSender` abstraction (step 040).
- `AppDbContext.PasswordResetTokens` and `AppDbContext.RefreshTokens` DbSets are already present.
- `IJwtService.GenerateRefreshTokenRaw()` and `IJwtService.HashToken()` are reused from the existing login/refresh flow.
- The next step (controller endpoints) will wire `POST /auth/forgot-password` and `POST /auth/reset-password` to these commands.

## How to run / verify

Stop the running API process, then:

```bash
cd apps/api && dotnet build --no-restore
```

All four projects should report `succeeded` with 0 errors.

## Git commit

```bash
git add -A && git commit -m "feat(auth): ForgotPasswordCommand and ResetPasswordCommand handlers"
```
