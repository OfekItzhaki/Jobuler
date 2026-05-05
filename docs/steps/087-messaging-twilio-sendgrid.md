# Step 087 — Messaging: Twilio (WhatsApp) + SendGrid (Email)

## Phase
Phase 6 — Hardening / Integrations

## Purpose
Replace the no-op notification stubs with real message delivery via Twilio (WhatsApp) and SendGrid (Email). The system already had the right abstractions (`INotificationSender`, `IEmailSender`, `IInvitationSender`) — this step wires in the real providers behind those interfaces.

## What was built

### New files

| File | Description |
|---|---|
| `Infrastructure/Email/SendGridEmailSender.cs` | Sends emails via SendGrid API. Graceful no-op if `SendGrid:ApiKey` is not configured. |
| `Infrastructure/Notifications/TwilioWhatsAppSender.cs` | Sends WhatsApp messages via Twilio. Graceful no-op if credentials are missing. Handles E.164 phone number formatting. |
| `Infrastructure/Notifications/RoutingNotificationSender.cs` | Routes `INotificationSender` calls: phone number → WhatsApp, email address → SendGrid. Replaces `NoOpNotificationSender` in production. |
| `Infrastructure/Notifications/ScheduleNotificationSender.cs` | Implements `IScheduleNotificationSender` — sends schedule-published and assignment notifications via the appropriate channel. |
| `Application/Common/IScheduleNotificationSender.cs` | New interface for schedule-specific notifications (publish, assignment). |

### Modified files

| File | Change |
|---|---|
| `Infrastructure/Notifications/WhatsAppInvitationSender.cs` | Now uses `TwilioWhatsAppSender` directly instead of `INotificationSender`. Sends a proper Hebrew invitation message. |
| `Infrastructure/Jobuler.Infrastructure.csproj` | Added `SendGrid` (9.29.3) and `Twilio` (7.3.1) NuGet packages. |
| `Api/Program.cs` | Conditional DI registration: uses real providers when credentials are configured, falls back to no-op. Registers `IScheduleNotificationSender`. |
| `Api/appsettings.json` | Added `SendGrid` and `Twilio` config sections with empty defaults. |
| `infra/compose/.env.example` | Added `SENDGRID_*` and `TWILIO_*` environment variable placeholders. |
| `infra/compose/docker-compose.yml` | Passes SendGrid and Twilio env vars to the API container. |

## Key decisions

### Graceful no-op fallback
Both providers check for credentials at startup and log a warning if not configured. The app starts and runs normally without any credentials — notifications are just logged instead of sent. This means local dev works without any external accounts.

### Routing by contact type
`RoutingNotificationSender` detects whether the `to` field is an email address (contains `@`) or a phone number (starts with `+`). This means the same `INotificationSender.SendPasswordResetAsync` call works for both channels without any changes to business logic.

### TwilioWhatsAppSender is always registered
`TwilioWhatsAppSender` is registered as a concrete class (not behind an interface) so it can be injected directly into `WhatsAppInvitationSender` and `ScheduleNotificationSender`. It handles its own no-op fallback internally.

### WhatsApp phone number format
Twilio requires numbers in `whatsapp:+E164` format. `TwilioWhatsAppSender` automatically prepends `whatsapp:` if not already present.

### Twilio sandbox for testing
The default `Twilio:WhatsAppFrom` is `whatsapp:+14155238886` — the Twilio sandbox number. For production, replace with your approved WhatsApp Business number.

## How to configure

### Local dev (no real messages)
Leave all credentials empty in `.env` — the app logs notifications to the console.

### Real SendGrid
```bash
# In infra/compose/.env or as environment variables:
SENDGRID_API_KEY=SG.your-api-key-here
SENDGRID_FROM_EMAIL=noreply@yourdomain.com
SENDGRID_FROM_NAME=Shifter
```

### Real Twilio WhatsApp
```bash
TWILIO_ACCOUNT_SID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
TWILIO_AUTH_TOKEN=your-auth-token
TWILIO_WHATSAPP_FROM=whatsapp:+14155238886  # sandbox, or your approved number
```

For the Twilio sandbox: go to https://console.twilio.com/us1/develop/sms/try-it-out/whatsapp-learn and follow the join instructions.

## Notification points wired

| Event | Channel | Implementation |
|---|---|---|
| Password reset | Phone → WhatsApp, Email → SendGrid | `RoutingNotificationSender.SendPasswordResetAsync` |
| Group invitation | WhatsApp | `WhatsAppInvitationSender` via `TwilioWhatsAppSender` |
| Group invitation | Email | `EmailInvitationSender` via `SendGridEmailSender` |
| Schedule published | Phone → WhatsApp, Email → SendGrid | `ScheduleNotificationSender.SendSchedulePublishedAsync` |
| Assignment notification | Phone → WhatsApp, Email → SendGrid | `ScheduleNotificationSender.SendAssignmentNotificationAsync` |
| Ownership transfer | Email | `InitiateOwnershipTransferCommand` via `IEmailSender` |
| Group restore | Email | `RestoreGroupCommand` via `IEmailSender` |

## How to verify

```bash
# 1. Build
cd apps/api && dotnet build Jobuler.sln

# 2. Without credentials — should log warnings, not crash
dotnet run --project Jobuler.Api
# Look for: "SendGrid:ApiKey not configured" and "Twilio credentials not configured"

# 3. With credentials — trigger a password reset
curl -X POST http://localhost:5000/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"your-test-email@example.com"}'
# Should receive a real email if SendGrid is configured
# Should receive a WhatsApp message if Twilio is configured and user has a phone number
```

## What comes next
- Wire `IScheduleNotificationSender` into `PublishVersionCommand` to notify members when a schedule is published
- Add member notification preferences (opt-in/opt-out per channel)
- Add Twilio WhatsApp template messages for better delivery rates in production

## Git commit

```bash
git add -A && git commit -m "feat(messaging): Twilio WhatsApp + SendGrid email integration"
```
