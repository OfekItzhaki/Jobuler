# Step 040 — INotificationSender Abstraction

## Phase
Phase 8 — Group Alerts & Phone

## Purpose
Introduce a dedicated `INotificationSender` interface for user-facing notifications (password reset delivery). This is intentionally separate from `IEmailSender`, which handles system emails such as ownership-transfer messages. Decoupling the two means the delivery channel for user notifications (WhatsApp, SMS, email) can be swapped by changing a single DI registration without touching any business logic.

## What was built

| File | Action | Description |
|------|--------|-------------|
| `apps/api/Jobuler.Application/Common/INotificationSender.cs` | Created | Interface with a single `SendPasswordResetAsync(to, token, ct)` method |
| `apps/api/Jobuler.Infrastructure/Notifications/NoOpNotificationSender.cs` | Created | Dev-time no-op implementation; logs the token at Warning level so it can be copied from the console |
| `apps/api/Jobuler.Api/Program.cs` | Modified | Registered `INotificationSender → NoOpNotificationSender` as a scoped service, immediately after the `IEmailSender` registration |

## Key decisions
- Kept `INotificationSender` in `Application.Common` (same layer as `IEmailSender`) so handlers can depend on it without referencing Infrastructure.
- Used `LogWarning` (not `LogDebug`) in the no-op so the token is visible in default console output during development.
- Scoped lifetime mirrors `IEmailSender` — consistent with the rest of the service registrations.

## How it connects
- Future password-reset command handlers will inject `INotificationSender` and call `SendPasswordResetAsync`.
- A real WhatsApp/SMS/email provider is wired in by replacing `NoOpNotificationSender` in DI — zero changes to Application or Domain layers.

## How to run / verify
Stop any running API process, then:
```bash
dotnet build apps/api --no-restore
```
All projects should compile with 0 errors. At runtime, triggering a password-reset flow will emit a `[WARN] [NoOp] Password reset for …` line in the console.

## What comes next
- Task 10.4+: Implement the `ForgotPassword` / `ResetPassword` command handlers that inject `INotificationSender`.

## Git commit
```bash
git add -A && git commit -m "feat(phase8): add INotificationSender interface and NoOpNotificationSender"
```
