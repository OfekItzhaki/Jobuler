# Step 029 — Group Ownership

## Phase
Phase 8 — Group Management

## Purpose

Groups previously had no ownership concept: the creator was not added as a member, any admin could remove any member, and there was no way to rename or delete a group from the UI. This step introduces:

- **Creator auto-membership** — the user who creates a group is automatically added as a member with `is_owner = true`
- **Owner removal protection** — the owner cannot be removed; ownership must be transferred first
- **Owner-only management actions** — rename, soft-delete (30-day recovery window), ownership transfer via email confirmation
- **Group avatars** — deterministic colored circles with the group's first letter
- **Dana** — a 5th seed user with no group memberships, for testing the add-by-email flow

## What was built

### Backend (tasks 1–11)

| File | Change |
|------|--------|
| `infra/migrations/009_group_ownership.sql` | Adds `is_owner` to `group_memberships`, `deleted_at` to `groups`, creates `pending_ownership_transfers` table |
| `apps/api/Jobuler.Domain/Groups/GroupMembership.cs` | Added `IsOwner`, `SetOwner()`, updated `Create()` |
| `apps/api/Jobuler.Domain/Groups/Group.cs` | Added `DeletedAt`, `SoftDelete()`, `Restore()`, `Rename()` |
| `apps/api/Jobuler.Domain/Groups/PendingOwnershipTransfer.cs` | New entity with token, expiry, `IsExpired` |
| `apps/api/Jobuler.Application/Common/IEmailSender.cs` | Interface for email sending |
| `apps/api/Jobuler.Infrastructure/Email/NoOpEmailSender.cs` | No-op implementation (logs at Debug) |
| `apps/api/Jobuler.Application/Common/ConflictException.cs` | HTTP 409 exception type |
| `apps/api/Jobuler.Application/Groups/Commands/CreateGroupCommand.cs` | Auto-membership with `isOwner: true` |
| `apps/api/Jobuler.Application/Groups/Commands/LeaveGroupCommand.cs` | Owner removal protection |
| `apps/api/Jobuler.Application/Groups/Commands/RenameGroupCommand.cs` | Owner-only rename |
| `apps/api/Jobuler.Application/Groups/Commands/SoftDeleteGroupCommand.cs` | Owner-only soft-delete |
| `apps/api/Jobuler.Application/Groups/Commands/RestoreGroupCommand.cs` | Owner-only restore + notifications |
| `apps/api/Jobuler.Application/Groups/Commands/InitiateOwnershipTransferCommand.cs` | Creates pending transfer, sends email |
| `apps/api/Jobuler.Application/Groups/Commands/ConfirmOwnershipTransferCommand.cs` | Atomic ownership swap |
| `apps/api/Jobuler.Application/Groups/Commands/CancelOwnershipTransferCommand.cs` | Owner-only cancellation |
| `apps/api/Jobuler.Application/Groups/Queries/GetGroupsQuery.cs` | Filters deleted groups, adds `ownerPersonId` and `isOwner` |
| `apps/api/Jobuler.Application/Groups/Queries/GetDeletedGroupsQuery.cs` | Returns recoverable deleted groups |
| `apps/api/Jobuler.Application/Persistence/AppDbContext.cs` | Added `PendingOwnershipTransfers` DbSet |
| `apps/api/Jobuler.Infrastructure/Persistence/Configurations/` | EF config for new columns and `PendingOwnershipTransfer` |
| `apps/api/Jobuler.Api/Controllers/GroupsController.cs` | 7 new endpoints + `[AllowAnonymous]` confirm-transfer |
| `apps/api/Jobuler.Api/Middleware/ExceptionHandlingMiddleware.cs` | `ConflictException` → 409 |

### Frontend (tasks 12–18)

| File | Change |
|------|--------|
| `infra/scripts/seed.sql` | Added Dana user, person, space membership, `space.view` permission — no group memberships |
| `apps/web/lib/api/groups.ts` | Added `ownerPersonId` to `GroupWithMemberCountDto`, `isOwner` to `GroupMemberDto`, new `DeletedGroupDto`, 6 new API functions |
| `apps/web/lib/utils/groupAvatar.ts` | New — `getAvatarColor()` and `getAvatarLetter()` utilities |
| `apps/web/app/groups/page.tsx` | Avatar circles on group cards; create form available to all logged-in users |
| `apps/web/app/groups/[groupId]/page.tsx` | Avatar in header; owner badge + hidden remove button; expanded settings panel with rename, delete, restore, transfer |
| `apps/web/app/groups/confirm-transfer/page.tsx` | New public page — reads `?token=`, calls API with plain `fetch`, shows Hebrew success/error |

### Tests

| File | What it tests |
|------|---------------|
| `apps/web/__tests__/groupAvatar.test.ts` | Properties 11 & 12 — avatar color determinism and letter uppercasing |
| `apps/web/__tests__/group-detail-tabs.test.ts` | Property 16 — transfer dropdown excludes the owner |

## Key decisions

- Owner checks happen in Application-layer handlers, not controllers — consistent with architecture rules
- `ConflictException` subclasses `InvalidOperationException` so the middleware switch matches it before the base type
- The confirm-transfer page uses plain `fetch` (not `apiClient`) — the token is the credential, no auth headers needed
- `getDeletedGroups` uses an on-read filter (30-day window) rather than a scheduled cleanup job
- Dana has no `group_memberships` rows by design — she exists solely to test the add-by-email flow
- Create group form is now available to all logged-in users (not just admins) since the creator becomes the owner automatically

## How it connects

- Migration 009 must be applied before starting the API
- `IEmailSender` / `NoOpEmailSender` is registered in Infrastructure DI — no SMTP config needed for local dev
- The confirm-transfer page at `/groups/confirm-transfer?token=...` is the landing page for ownership transfer emails
- All ownership checks are server-enforced; the UI hides buttons as a UX convenience only

## How to run / verify

```bash
# 1. Apply migration 009
psql $DATABASE_URL -f infra/migrations/009_group_ownership.sql

# 2. Re-seed (adds Dana)
psql $DATABASE_URL -f infra/scripts/seed.sql

# 3. Build API
dotnet build apps/api/Jobuler.Application/Jobuler.Application.csproj -v q

# 4. Start API and frontend
dotnet run --project apps/api/Jobuler.Api
cd apps/web && npm run dev

# 5. Verify
# - Create a group → you appear as a member with "בעלים" badge
# - Try to remove the owner via API → expect HTTP 400
# - Soft-delete a group → it disappears from the list
# - Go to settings → "קבוצות מחוקות" shows the deleted group with a restore button
# - Restore → group reappears
# - Initiate ownership transfer → "ממתין לאישור" status shown
# - Cancel transfer → form restored
# - Visit /groups/confirm-transfer?token=invalid → Hebrew error shown

# 6. Run frontend property tests
cd apps/web
node --require ts-node/register __tests__/groupAvatar.test.ts
node --require ts-node/register __tests__/group-detail-tabs.test.ts
```

## What comes next

- Wire a real `IEmailSender` implementation (SendGrid, SES, etc.) when email delivery is needed
- Add property tests P1–P15 (FsCheck, C# backend) for full backend coverage
- Consider adding a notification bell for pending ownership transfer acceptance

## Git commit

```bash
git add -A && git commit -m "feat(phase8): group ownership model, soft-delete, ownership transfer"
```
