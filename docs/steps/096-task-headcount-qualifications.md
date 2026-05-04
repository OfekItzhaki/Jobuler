# Step 096 — Task Required Headcount & Qualifications

## Phase
Phase 9 — Quality & Polish

## Purpose
Two related features for group tasks (missions):
1. Required headcount per shift was already stored but not prominently editable — confirmed it's in the form.
2. Required qualifications per task — at least one assignee per shift must hold the specified qualification(s). This is a new field end-to-end: DB → domain → API → solver → frontend.

## What was built

### DB migration — `034_task_required_qualifications.sql`
- Adds `required_qualification_names text[] NOT NULL DEFAULT '{}'` to the `tasks` table.
- Stores qualification names (not IDs) because qualifications are group-scoped and identified by name. The solver already matches people to qualifications by name string.

### Domain — `GroupTask.cs`
- Added `RequiredQualificationNames: List<string>` property.
- `Create()` and `Update()` accept an optional `List<string>? requiredQualificationNames` parameter.

### Application — `GroupTaskCommands.cs`
- `GroupTaskDto` record gains `List<string> RequiredQualificationNames`.
- `CreateGroupTaskCommand` and `UpdateGroupTaskCommand` gain `List<string>? RequiredQualificationNames`.
- Handlers pass the field through to `GroupTask.Create()` / `task.Update()`.

### Application — `GetGroupTasksQuery.cs`
- Maps `t.RequiredQualificationNames` into the returned DTO.

### Infrastructure — `TasksConfiguration.cs`
- `GroupTaskConfiguration` maps `RequiredQualificationNames` to `required_qualification_names` as a `text[]` column with a proper value comparer.

### Infrastructure — `SolverPayloadNormalizer.cs`
- GroupTask shift slots now pass `task.RequiredQualificationNames` as `RequiredQualificationIds` in the solver payload (the field is `List<string>` in both cases — the solver matches by name string).

### API — `TasksController.cs`
- `CreateGroupTaskRequest` and `UpdateGroupTaskRequest` gain `List<string>? RequiredQualificationNames`.
- Both endpoints pass the field to their respective commands.

### Frontend — `tasks.ts`
- `GroupTaskDto` gains `requiredQualificationNames: string[]`.
- `GroupTaskPayload` gains `requiredQualificationNames?: string[]`.

### Frontend — `TasksTab.tsx`
- `TaskForm` interface gains `requiredQualificationNames: string[]`.
- Props gain `groupQualifications: GroupQualificationDto[]`.
- Task cards show qualification badges (violet) when qualifications are set.
- Form shows a checkbox list of group qualifications when any exist.

### Frontend — `page.tsx`
- `DEFAULT_TASK_FORM` includes `requiredQualificationNames: []`.
- Tasks tab load effect also loads `groupQualifications` (needed for the picker).
- `handleTaskSubmit` includes `requiredQualificationNames` in the payload.
- Edit task handler populates `requiredQualificationNames` from the existing task.
- `TasksTab` receives `groupQualifications` prop.

## Key decisions
- Qualification names are stored directly (not IDs) to avoid a join and because the solver already uses name strings for matching.
- The qualification picker only appears if the group has qualifications defined — no clutter for groups that don't use them.
- The headcount field was already in the form; this step confirms it's wired end-to-end.

## How to verify
1. Run migration: `psql $DATABASE_URL -f infra/migrations/034_task_required_qualifications.sql`
2. Open a group → Tasks tab → create a task → qualification checkboxes appear if the group has qualifications
3. Save the task → qualification badges appear on the task card
4. Run the solver → the solver payload includes `required_qualification_ids` for tasks with qualifications
5. The solver will only assign people who hold the required qualification to those shifts

## Git commit

```bash
git add -A && git commit -m "feat(tasks): required headcount confirmed, required qualifications per task end-to-end"
```
