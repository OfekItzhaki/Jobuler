# Step 042 — ForgotPassword and ResetPassword API Endpoints

## Phase
Phase 8 — Password Reset Flow

## Purpose
Expose the `ForgotPasswordCommand` and `ResetPasswordCommand` (already implemented in the Application layer) via HTTP endpoints so clients can trigger a password reset flow.

## What was built

- `apps/api/Jobuler.Api/Controllers/AuthController.cs` — added two new endpoints and two request records:
  - `POST /auth/forgot-password` — accepts `{ email }`, dispatches `ForgotPasswordCommand`, always returns `200 OK` to prevent user enumeration
  - `POST /auth/reset-password` — accepts `{ token, newPassword }`, dispatches `ResetPasswordCommand`, returns `204 No Content` on success
  - `ForgotPasswordRequest(string Email)` record
  - `ResetPasswordRequest(string Token, string NewPassword)` record

## Key decisions

- Both endpoints are `[AllowAnonymous]` — users are not authenticated when resetting a password.
- `ForgotPassword` always returns `200 OK` regardless of whether the email exists, preventing user enumeration (mirrors the handler's silent no-op for unknown emails).
- `ResetPassword` returns `204 No Content` on success, consistent with other mutation endpoints in the controller.
- No permission check is needed in the controller — the Application layer handles all validation and token verification.

## How it connects

- Delegates to `ForgotPasswordCommand` and `ResetPasswordCommand` in `Jobuler.Application/Auth/Commands/`.
- Errors (invalid token, expired token, weak password) bubble up to `ExceptionHandlingMiddleware` which maps `InvalidOperationException` → 400.

## How to run / verify

```bash
# Request a reset token (always 200)
curl -X POST http://localhost:5000/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'

# Reset password with the token received via notification
curl -X POST http://localhost:5000/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{"token":"<raw-token>","newPassword":"NewPass123!"}'
```

## What comes next

- Frontend pages for forgot-password and reset-password forms (task 12.2+).

## Git commit

```bash
git add -A && git commit -m "feat(auth): add forgot-password and reset-password endpoints to AuthController"
```
