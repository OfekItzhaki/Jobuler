from fastapi import APIRouter, HTTPException
from models.solver_input import SolverInput
from models.solver_output import SolverOutput
from solver.engine import solve as run_solver
import logging

router = APIRouter()
logger = logging.getLogger(__name__)


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
        result = run_solver(payload)
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
    except Exception as e:
        logger.exception("Solver run failed: run_id=%s", payload.run_id)
        raise HTTPException(status_code=500, detail=str(e))
