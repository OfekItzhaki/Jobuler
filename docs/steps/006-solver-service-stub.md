# Step 006 — Python Solver Service (Stub + Contract)

## Phase
Phase 1 — Foundation (solver contract); Phase 3 — full constraint implementation

## Purpose
Stand up the Python OR-Tools solver service with a real HTTP contract, typed input/output models, and a working CP-SAT skeleton. The stub is functional enough to accept a payload and return a structured response. Full hard constraints and fairness objectives are Phase 3.

## What was built

| File | Description |
|---|---|
| `apps/solver/requirements.txt` | FastAPI, uvicorn, ortools, pydantic, python-dotenv |
| `apps/solver/main.py` | FastAPI app entry point |
| `apps/solver/routers/health.py` | `GET /health` — liveness check |
| `apps/solver/routers/solve.py` | `POST /solve` — accepts `SolverInput`, returns `SolverOutput` |
| `apps/solver/models/solver_input.py` | Full typed input contract (people, slots, constraints, baseline, fairness counters, stability weights) |
| `apps/solver/models/solver_output.py` | Full typed output contract (assignments, conflicts, stability metrics, fairness metrics, explanation fragments) |
| `apps/solver/solver/engine.py` | CP-SAT model: headcount constraints, no-duplicate-assignment, stability penalty objective |

## Key decisions

### Contracts match spec Section 17
`SolverInput` and `SolverOutput` are direct implementations of the spec's Section 17.1 and 17.2. The API backend will serialize to this exact shape when enqueuing solver jobs.

### Stability weights are in the input payload
The API sends `stability_weights` (today_tomorrow, days_3_4, days_5_7) with every solve request. This means the weights can be tuned per space or per trigger mode without redeploying the solver.

### Timeout returns best-known result
If CP-SAT hits `SOLVER_TIMEOUT_SECONDS`, it returns `timed_out: true` with the best feasible assignments found so far. The API stores this with a warning flag. Admins see the warning in the draft review UI.

### Solver is stateless
The solver service holds no state between requests. All context (baseline assignments, fairness counters, constraints) is sent in the input payload. This makes horizontal scaling trivial.

## How it connects
- The API's `SolverWorker` (Phase 3) calls `POST /solve` with a normalized payload.
- The solver output is stored as a `schedule_version` with its `assignments` rows.
- `explanation_fragments` are stored in `result_summary_json` on the `schedule_run` record and shown in the admin diff UI.

## How to run / verify

```bash
cd apps/solver
pip install -r requirements.txt
uvicorn main:app --reload

# Health check
curl http://localhost:8000/health

# Minimal solve test
curl -X POST http://localhost:8000/solve \
  -H "Content-Type: application/json" \
  -d '{
    "space_id": "test",
    "run_id": "run-001",
    "trigger_mode": "standard",
    "horizon_start": "2026-04-20",
    "horizon_end": "2026-04-27",
    "stability_weights": {"today_tomorrow": 10.0, "days_3_4": 3.0, "days_5_7": 1.0},
    "people": [{"person_id": "p1", "role_ids": [], "qualification_ids": [], "group_ids": []}],
    "availability_windows": [],
    "presence_windows": [],
    "task_slots": [{
      "slot_id": "s1", "task_type_id": "t1", "task_type_name": "Guard",
      "burden_level": "neutral", "starts_at": "2026-04-20T08:00:00",
      "ends_at": "2026-04-20T16:00:00", "required_headcount": 1,
      "priority": 5, "required_role_ids": [], "required_qualification_ids": [], "allows_overlap": false
    }],
    "hard_constraints": [], "soft_constraints": [],
    "baseline_assignments": [], "fairness_counters": []
  }'
```

## What comes next
- Phase 3: Full hard constraints (rest windows, no-overlap, kitchen limits, qualification checks)
- Phase 3: Fairness objective (burden distribution)
- Phase 4: API worker enqueues jobs to this service via Redis queue
