# Step 078 — Solver Constraint Bugs + Pipeline Tests

## Phase
Phase 3 — Scheduling Core (bugfix)

## Purpose
Two bugs in the Python solver were causing empty drafts and incorrect assignments:
1. The `blocked`/`at_home`/`on_mission` presence states were not all enforced — only `at_home` was checked, so blocked people got assigned freely.
2. The headcount constraint was a hard `>=` lower bound, making the model INFEASIBLE whenever any slot couldn't be fully staffed. This produced empty drafts instead of partial results.

Additionally, the `SolverPayloadNormalizer` called `ExecuteSqlRawAsync` unconditionally, crashing when used with an in-memory DB in tests.

## What Was Built

### Modified — Python solver
- **`apps/solver/solver/constraints.py`**
  - `add_headcount_constraints`: Changed from hard `>= required_headcount` to `<= required_headcount` (cap over-staffing only). Shortfalls are now handled entirely by the soft coverage objective (weight=1000), which produces partial results instead of INFEASIBLE.
  - `add_availability_constraints`: Now blocks all three presence states (`blocked`, `at_home`, `on_mission`) instead of only `at_home`. Introduced `BLOCKING_STATES` set.

- **`apps/solver/solver/engine.py`**
  - `_build_hard_conflicts`: Updated eligibility analysis to use `BLOCKING_STATES` instead of only checking `at_home`.

### Modified — .NET API
- **`apps/api/Jobuler.Infrastructure/Scheduling/SolverPayloadNormalizer.cs`**
  - Guarded `ExecuteSqlRawAsync` with `if (_db.Database.IsRelational())` so the normalizer works with in-memory DB in tests.

### Created — Tests
- **`apps/api/Jobuler.Tests/Application/SolverEndToEndTests.cs`** (8 tests)
  - Hits the live Python solver directly with known-good and known-bad payloads.
  - Covers: health, feasible 1-person, feasible multi-person/multi-slot, infeasible (0 people), infeasible (headcount > people), blocked person not assigned, response shape, empty input.

- **`apps/api/Jobuler.Tests/Integration/SolverWorkerPipelineTests.cs`** (5 tests)
  - Seeds real domain entities into in-memory DB, runs the normalizer, calls the live solver.
  - Covers: feasible scenario with group tasks, no-people scenario, partial coverage (1 person / headcount=2), at_home person not assigned, presence state string mapping.

## Key Decisions
- **Soft headcount instead of hard**: The coverage objective (weight=1000) already heavily penalises shortfalls. A hard `>=` constraint is redundant and harmful — it makes the model INFEASIBLE when it should produce a partial result.
- **All blocking states**: `blocked`, `at_home`, and `on_mission` all mean the person cannot be assigned. The solver now treats all three identically.
- **In-memory guard in normalizer**: The RLS session variable call is a no-op in tests (no RLS enforcement in in-memory DB), so skipping it is safe and correct.

## How It Connects
- The Python solver is called by `SolverWorkerService` via `SolverHttpClient`.
- The normalizer builds the payload from the DB and passes it to the solver.
- With these fixes, the solver now always returns a feasible result with partial assignments when full coverage isn't possible, instead of returning INFEASIBLE and producing an empty draft.

## How to Run / Verify
```bash
# Run all solver tests (requires Python solver on localhost:8000)
dotnet test Jobuler.Tests/Jobuler.Tests.csproj --filter "SolverEndToEnd|SolverWorkerPipeline"

# Run full suite
dotnet test Jobuler.Tests/Jobuler.Tests.csproj
```
Expected: 299/299 pass.

## What Comes Next
- Trigger a real solver run from the UI and verify a draft with assignments appears.
- The `personal-and-role-constraints` spec can now proceed — the solver correctly handles constraint violations as partial results rather than INFEASIBLE.

## Git Commit
```bash
git add -A && git commit -m "fix(solver): hard headcount constraint causes empty drafts + blocked presence state ignored"
```
