from fastapi import APIRouter, HTTPException
from models.solver_input import SolverInput
from models.solver_output import SolverOutput
from solver.engine import solve as run_solver
import logging

router = APIRouter()
logger = logging.getLogger(__name__)


@router.post("/solve", response_model=SolverOutput)
def solve(payload: SolverInput) -> SolverOutput:
    logger.info("Solver run started: run_id=%s space_id=%s trigger=%s",
                payload.run_id, payload.space_id, payload.trigger_mode)
    try:
        result = run_solver(payload)
        logger.info("Solver run finished: run_id=%s feasible=%s timed_out=%s",
                    result.run_id, result.feasible, result.timed_out)
        return result
    except Exception as e:
        logger.exception("Solver run failed: run_id=%s", payload.run_id)
        raise HTTPException(status_code=500, detail=str(e))
