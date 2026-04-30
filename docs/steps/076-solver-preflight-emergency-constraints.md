# Step 076 — Solver Pre-flight Check, Emergency Constraints & Double-Shift Awareness

## Phase
Phase 4 — Scheduling Engine

## Purpose
Three improvements to the scheduling engine:

1. **Pre-flight capacity check** — Before calling the solver, validate that there are enough people to cover all tasks given the shift duration, rest constraints, and headcount requirements. Fail immediately with a clear explanation instead of timing out.

2. **Emergency constraints** — A new `emergency` severity that bypasses all hard and soft constraints. Useful for urgent situations where a schedule is needed regardless of availability, rest, qualifications, or roles.

3. **Double-shift & overlap awareness** — The pre-flight check now reads `AllowsDoubleShift` and `AllowsOverlap` directly from the task slots to calculate the correct minimum people needed.

## What was built

### Backend — API

- **`apps/api/Jobuler.Domain/Constraints/ConstraintRule.cs`**
  - Added `Emergency` to `ConstraintSeverity` enum with full documentation.

- **`apps/api/Jobuler.Application/Scheduling/Models/SolverInputDto.cs`**
  - Added `EmergencyConstraints` list to `SolverInputDto`.
  - Added `AllowsDoubleShift` field to `TaskSlotDto` (default `false`).

- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverPayloadNormalizer.cs`**
  - Passes `AllowsDoubleShift` from `GroupTask` into each generated `TaskSlotDto`.
  - Builds and passes `EmergencyConstraints` list (constraints with `Severity == Emergency`).

- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverWorkerService.cs`**
  - Replaced the naive "peak daily headcount" pre-flight with a correct capacity formula:
    - `shiftsPerDay = 24 / shiftHours`
    - `effectiveBlock = shiftHours × 2 + restHours` (if double-shift) or `shiftHours + restHours`
    - `maxShiftsPerPerson = floor(24 / effectiveBlock) × shiftsPerCycle`
    - `minPeopleForTask = ceil(shiftsPerDay / maxShiftsPerPerson) × requiredHeadcount`
  - Non-overlap tasks sum their requirements; overlap tasks share the pool (max of overlap group).
  - Rest hours read from hard constraints first, then soft, then defaults to 0.
  - Failure notification includes per-task breakdown: e.g. "תל 7 (3), תל 9 (3)".

### Backend — Solver (Python)

- **`apps/solver/models/solver_input.py`**
  - Added `allows_double_shift: bool = False` to `TaskSlot`.
  - Added `emergency_constraints: list[HardConstraint] = []` to `SolverInput`.

- **`apps/solver/solver/engine.py`**
  - Calls `_build_emergency_bypass(input)` before building constraints to get bypass sets.
  - Passes `emergency_person_ids` to overlap, qualification, role, rest, and availability constraint functions.
  - Added `_build_emergency_bypass()` helper that parses emergency constraints into person/slot bypass sets. Supports three rule types: `emergency_person_bypass`, `emergency_slot_bypass`, `emergency_space_bypass`.

- **`apps/solver/solver/constraints.py`**
  - All constraint functions now accept optional `emergency_person_ids: set` parameter.
  - `add_no_overlap_constraints` — bypassed people skip overlap enforcement.
  - `add_min_rest_constraints` — bypassed people skip rest gap enforcement.
  - `add_qualification_constraints` — bypassed people skip qualification checks.
  - `add_role_constraints` — bypassed people skip role checks.
  - `add_availability_constraints` — bypassed people skip at-home and availability window checks.

### Database

- **`infra/migrations/025_constraint_emergency_severity.sql`**
  - Drops and recreates `chk_constraint_severity` to include `'emergency'`.

### Frontend

- **`apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx`**
  - Added three emergency rule types to the rule type selector.
  - Added `🚨 חירום (Emergency)` option to both create and edit severity dropdowns.
  - Severity badge displays "🚨 חירום" for emergency constraints.

- **`apps/web/app/groups/[groupId]/types.ts`**
  - Added orange styling for `emergency` severity in `SEVERITY_STYLES` and `SEVERITY_DOTS`.

## Key decisions

- **Pre-flight uses conservative sum** — non-overlap tasks each need their own people pool; overlap tasks share. This is the safe lower bound.
- **Emergency bypasses availability/rest/qualifications/roles** — but NOT headcount. You still need the minimum number of people per slot; emergency just removes eligibility restrictions.
- **`emergency_space_bypass`** — adds every person and every slot to the bypass sets, effectively disabling all eligibility constraints for the entire run.
- **Double-shift in pre-flight** — when `allowsDoubleShift = true`, a person works 2 shifts back-to-back before resting, so `maxShiftsPerPerson` doubles.

## How to run / verify

1. With 3 people and 2 tasks (4h shifts, 8h rest): trigger solver → immediate failure notification "נדרשים לפחות 6 חברים".
2. Add 6+ people to groups → trigger solver → draft produced with full 7-day coverage.
3. Create an `emergency_space_bypass` constraint with `emergency` severity → trigger solver → all availability/rest/role restrictions ignored.

## Git commit

```bash
git add -A && git commit -m "feat(scheduling): pre-flight capacity check, emergency constraints, double-shift awareness"
```
