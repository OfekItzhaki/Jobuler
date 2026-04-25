# Step 032 — Group Member Phone Number in DTO

## Phase
Phase 8 — Group Alerts & Phone

## Purpose
Expose the `phone_number` field (already present on the `people` table from migration 010) through the `GetGroupMembers` query so the frontend can display it in the group detail member list and use it for SMS alert targeting.

## What was built

- `apps/api/Jobuler.Application/Groups/Queries/GetGroupsQuery.cs`
  - Added `string? PhoneNumber` as the fifth parameter to `GroupMemberDto`
  - Extended the `.Join` anonymous projection to include `p.PhoneNumber`
  - Extended the `.Select` call to pass `p.PhoneNumber` into the `GroupMemberDto` constructor

## Key decisions

- No migration needed — `phone_number` column already exists from migration 010.
- The field is nullable (`string?`) because not every person is required to have a phone number.

## How it connects

- `GroupMemberDto` is returned by `GET /api/groups/{groupId}/members` via `GroupsController`.
- The frontend group detail page reads this DTO to render the member list; subsequent tasks will surface the phone number in the UI and use it for alert dispatch.

## How to run / verify

Stop the running API server, then:

```bash
cd apps/api && dotnet build --no-restore
```

Expect `Build succeeded` with 0 errors.

## What comes next

- Task 1.2: Surface `phoneNumber` in the frontend `GroupMemberDto` TypeScript type and member list UI.
- Task 1.3: Add SMS alert dispatch using the phone numbers returned here.

## Git commit

```bash
git add -A && git commit -m "feat(group-alerts): add PhoneNumber to GroupMemberDto and projection"
```
