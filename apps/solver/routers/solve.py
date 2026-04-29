from fastapi import APIRouter, HTTPException
from models.solver_input import SolverInput
from models.solver_output import SolverOutput
from solver.engine import solve as run_solver
import logging
import concurrent.futures
import os

router = APIRouter()
logger = logging.getLogger(__name__)

# Hard timeout for a single solve request — prevents the process from hanging forever
SOLVE_TIMEOUT_SECONDS = int(os.getenv("SOLVE_TIMEOUT_SECONDS", "45"))

# Thread pool so the solve runs in a separate thread and can be interrupted
_executor = concurrent.futures.ThreadPoolExecutor(max_workers=4)


@router.post("/solve", response_model=SolverOutput)
def solve(payload: SolverInput) -> SolverOutput:
    logger.info(
        "Solver run started: run_id=%s space_id=%s trigger=%s "
        "people=%d slots=%d hard_constraints=%d soft_constraints=%d "
        "availability_windows=%d presence_windows=%d",
        payload.run_id,
        payload.space_id,
        payload.trigger_mode,
        len(payload.people),
        len(payload.task_slots),
        len(payload.hard_constraints),
        len(payload.soft_constraints),
        len(payload.availability_windows),
        len(payload.presence_windows),
    )
    try:
        future = _executor.submit(run_solver, payload)
        result = future.result(timeout=SOLVE_TIMEOUT_SECONDS)
        logger.info(
            "Solver run finished: run_id=%s feasible=%s timed_out=%s "
            "assignments=%d uncovered=%d hard_conflicts=%d",
            result.run_id,
            result.feasible,
            result.timed_out,
            len(result.assignments),
            len(result.uncovered_slot_ids),
            len(result.hard_conflicts),
        )
        return result
    except concurrent.futures.TimeoutError:
        logger.error("Solver run timed out after %ds: run_id=%s", SOLVE_TIMEOUT_SECONDS, payload.run_id)
        raise HTTPException(status_code=504, detail=f"Solver timed out after {SOLVE_TIMEOUT_SECONDS}s")
    except Exception as e:
        logger.exception("Solver run failed: run_id=%s", payload.run_id)
        raise HTTPException(status_code=500, detail=str(e))
