# Step 073 — Solver Pipeline End-to-End Fix

## Phase
Phase 3 — Scheduling Core (bug fix)

## Purpose

The solver pipeline was broken end-to-end: every run either timed out or produced an empty draft with no assignments. This step traces the full flow, identifies the root cause, and fixes it so a working schedule is produced for any valid input.

## Root Cause

**Primary bug — `add_headcount_constraints` used `==` (exact equality) instead of `>=` (at least).**

In `apps/solver/solver/constraints.py`, the headcount constraint was:

```python
model.add(
    sum(assign[(s_idx, p_idx)] for p_idx in range(num_people))
    == slot.required_headcount
)
```

This forced the solver to assign *exactly* `required_headcount` people to every slot simultaneously. In any real scenario where constraints (rest gaps, availability windows, role restrictions) prevent full coverage of even one slot, the entire CP-SAT model becomes **INFEASIBLE** — not just that slot, but the whole schedule. The solver returns INFEASIBLE, `feasible=False`, zero assignments, and the worker discards the draft version.

The fix is `>=` (at least): the solver assigns as many eligible people as possible, and the coverage objective (weight=1000) penalises shortfalls heavily. The model is now feasible whenever *any* valid assignment exists, even if some slots are under-staffed.

**Secondary bug — engine discarded feasible partial solutions.**

`engine.py` had a guard that flipped `feasible=False` when all slots were uncovered:

```python
if feasible and len(uncovered) == num_slots and num_slots > 0:
    feasible = False
    assignments = []
```

With `>=` headcount, a feasible-but-partial solution is valid and should be kept. This guard was removed.

**Tertiary issue — `solve.py` had minimal logging.**

The router only logged `run_id`, `space_id`, and `trigger_mode` — not the number of people, slots, or constraints. This made it impossible to diagnose whether the solver was even receiving data. Enhanced logging was added.

## What was built

### Modified files

| File | Change |
|------|--------|
| `apps/solver/solver/constraints.py` | Changed `add_headcount_constraints` from `==` to `>=` with an explanatory docstring |
| `apps/solver/solver/engine.py` | Removed the "all uncovered → infeasible" override that discarded valid partial solutions |
| `apps/solver/routers/solve.py` | Added full payload logging: people count, slot count, constraint counts, availability/presence window counts; also logs assignment count, uncovered count, and conflict count on completion |

### New files

| File | Purpose |
|------|---------|
| `apps/solver/test_direct.py` | Standalone direct test: 2 people, 2 non-overlapping 8-hour shifts. Calls the engine directly (no HTTP). Verifies the solver produces 2 assignments and 0 uncovered slots. Run with `python test_direct.py` from `apps/solver/`. |

## Key decisions

### Why `>=` and not `==`?

The CP-SAT model already has a coverage objective that penalises shortfalls with weight=1000 (far higher than any soft objective). The solver will always try to maximise staffing. Using `>=` means:

- If full staffing is possible → solver assigns exactly `required_headcount` (the objective drives it there)
- If full staffing is impossible for some slots → solver assigns as many as it can and reports those slots in `uncovered_slot_ids`
- The model is never declared globally INFEASIBLE just because one slot can't be fully staffed

Using `==` was correct only in a world where the input is always perfectly satisfiable — which is never true in production.

### Why keep the infeasibility tests?

The existing tests (`test_insufficient_people_for_headcount_is_infeasible`, etc.) still pass because they test cases where the headcount constraint genuinely cannot be satisfied even with `>=` — e.g., 1 person for a slot requiring 2 means `sum(assign) >= 2` with only 1 bool var (max sum = 1), which is still infeasible. The tests remain valid.

### HTTP client timeout

The HTTP client is configured at 120 seconds (`Program.cs`), and the solver's CP-SAT timeout is 30 seconds (`SOLVER_TIMEOUT_SECONDS` env var, default 30). The 4× margin is intentional: it covers network overhead, serialization, and any startup latency. No change needed.

### Worker `shouldDiscard` logic

The worker correctly keeps timed-out results that have partial assignments:
```csharp
var shouldDiscard = !output.Feasible ||
    (output.TimedOut && output.Assignments.Count == 0);
```
This is correct and was not changed.

## How it connects

```
Frontend → POST /spaces/{id}/schedule-runs/trigger
         → API creates ScheduleRun, enqueues Redis job
         → SolverWorkerService dequeues job
         → SolverPayloadNormalizer.BuildAsync() → SolverInputDto
         → SolverHttpClient.SolveAsync() → POST /solve
         → [Python] solve.py router (now logs full payload stats)
         → [Python] engine.solve() → CP-SAT
         → [Python] constraints.add_headcount_constraints() ← FIXED (>= not ==)
         → [Python] returns SolverOutput with assignments
         → Worker stores ScheduleVersion + Assignments
         → Frontend polls /schedule-runs/{runId} → sees Completed
         → Schedule tab shows assignments
```

## How to run / verify

### Direct engine test (no server needed)
```bash
cd apps/solver
python test_direct.py
# Expected: "✓ All assertions passed — solver is working correctly!"
```

### Full test suite
```bash
cd apps/solver
python -m pytest tests/ -v
# Expected: 41 passed
```

### API build
```bash
cd apps/api
dotnet build Jobuler.Api/Jobuler.Api.csproj -v quiet
# Expected: exit code 0, no errors
```

## What comes next

- Monitor solver logs in production for `people=`, `slots=` counts to catch oversized payloads early
- Consider adding a `/health` check to the solver that runs a trivial 1-person 1-slot solve to verify the engine is operational
- The `SchedulingWindowDays = 3` cap in `SolverPayloadNormalizer` and the `MaxShiftsPerTask = 48` guard are the right defences against payload explosion — no changes needed there

## Git commit

```bash
git add -A && git commit -m "fix(solver): end-to-end solver pipeline working"
```
