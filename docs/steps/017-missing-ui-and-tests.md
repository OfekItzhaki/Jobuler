# Step 017 — Missing UI Pages, Error Boundary, Responsive Layout, More Tests

## Phase
Post-MVP Completion

## Purpose
Build the remaining admin UI pages (tasks, constraints), add an error boundary, make the layout responsive, add the roles API, and expand the test suite with constraint-level solver tests and validation tests.

## What was built

### New API

| File | Description |
|---|---|
| `Application/Spaces/Commands/CreateSpaceRoleCommand.cs` | Create dynamic operational role |
| `Application/Spaces/Queries/GetSpaceRolesQuery.cs` | List roles for a space |
| `Api/Controllers/RolesController.cs` | `GET/POST /spaces/{id}/roles` |

### New frontend API clients

| File | Description |
|---|---|
| `lib/api/tasks.ts` | getTaskTypes, createTaskType, getTaskSlots, createTaskSlot |
| `lib/api/constraints.ts` | getConstraints, createConstraint |
| `lib/api/groups.ts` | getSpaceRoles, createSpaceRole |

### New UI pages

| File | Description |
|---|---|
| `app/admin/tasks/page.tsx` | Task types + task slots management with tabbed view and create forms |
| `app/admin/constraints/page.tsx` | Constraint list + manual create form + AI parser integration |

### Error boundary

| File | Description |
|---|---|
| `components/ErrorBoundary.tsx` | React class component error boundary with reload button |
| `app/layout.tsx` | Wraps all pages in ErrorBoundary |

### Responsive layout

- `AppShell.tsx` — Nav items use `whitespace-nowrap` and `overflow-x-auto` for mobile. Padding uses `md:` breakpoints. Display name hidden on small screens.
- Tasks link and Constraints link added to admin nav.

### New tests

| File | Description |
|---|---|
| `Tests/Domain/SpaceTests.cs` | Space create, transfer ownership, deactivate |
| `Tests/Domain/TaskSlotTests.cs` | Slot time validation, cancel |
| `Tests/Validation/RegisterCommandValidatorTests.cs` | Email, password strength, locale whitelist |
| `solver/tests/test_constraints.py` | No-overlap, min-rest, qualification constraints — unit tests on CP-SAT model building |

## Key decisions

### Constraints page embeds AI parser
The AI parser component sits at the top of the constraints page. When the admin confirms a parsed constraint, it calls `createConstraint` directly. If AI is not configured, the parser shows a graceful fallback message.

### Tasks page uses tabs
Task types and task slots are on the same page with a tab switcher. This keeps the admin nav clean while giving access to both.

### Error boundary at root
The `ErrorBoundary` wraps the entire app in `layout.tsx`. Any unhandled React render error shows a friendly message with a reload button instead of a blank white screen.

## Git commit

```bash
git add -A && git commit -m "feat: tasks UI, constraints UI, roles API, error boundary, responsive layout, more tests"
```
