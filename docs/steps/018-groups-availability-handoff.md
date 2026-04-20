# Step 018 — Groups UI, Availability API, Handoff Guide

## Phase
Post-MVP Completion

## Purpose
Build the groups management UI, availability/presence window API endpoints, and a comprehensive handoff guide so the project can be continued from any machine without Kiro.

## What was built

### Backend

| File | Description |
|---|---|
| `Application/Groups/Commands/CreateGroupCommand.cs` | Create group type, create group, add person to group |
| `Application/Groups/Queries/GetGroupsQuery.cs` | List group types, list groups with member counts, list group members |
| `Api/Controllers/GroupsController.cs` | Full CRUD for group types, groups, and memberships |
| `Application/People/Commands/AddAvailabilityWindowCommand.cs` | Add availability window for a person |
| `Application/People/Commands/AddPresenceWindowCommand.cs` | Add manual presence window (at_home / free_in_base) |
| `Application/People/Queries/GetAvailabilityQuery.cs` | List availability and presence windows per person |
| `Api/Controllers/AvailabilityController.cs` | `GET/POST /spaces/{id}/people/{id}/availability` and `/presence` |

### Frontend

| File | Description |
|---|---|
| `lib/api/availability.ts` | API client for availability and presence windows |
| `app/admin/groups/page.tsx` | Groups management: create types, create groups, manage members with person selector |
| `AppShell.tsx` | Groups link added to admin nav |
| `messages/*.json` | `admin.groups` translation key added in he/en/ru |

### Documentation

| File | Description |
|---|---|
| `HANDOFF.md` | Complete guide: local setup, project structure, API reference, AWS deployment, what's left |

## Key decisions

### Groups page layout
Three-column layout: left side has create forms + groups table, right panel shows members for the selected group with an add-person dropdown. This avoids a separate page per group for MVP.

### Availability vs presence
- Availability windows = when a person CAN be scheduled (opt-in)
- Presence windows = where a person physically is (at_home / free_in_base, manually set)
- `on_mission` is always derived from assignments, never manually set (enforced in domain)

### HANDOFF.md
Written so any developer can clone the repo and run it in 5 minutes without needing Kiro or any prior context. Includes exact commands, project structure, API reference, AWS steps, and what's left to build.

## Git commit

```bash
git add -A && git commit -m "feat: groups UI, availability API, handoff guide"
```
