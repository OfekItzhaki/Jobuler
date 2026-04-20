"""
Solver output contract — matches the payload the solver returns to the API.
All fields mirror the spec's Section 17.2.
"""
from pydantic import BaseModel
from typing import Optional


class AssignmentResult(BaseModel):
    slot_id: str
    person_id: str
    source: str = "solver"  # solver | override


class HardConflict(BaseModel):
    constraint_id: str
    rule_type: str
    description: str
    affected_slot_ids: list[str]
    affected_person_ids: list[str]


class StabilityMetrics(BaseModel):
    today_tomorrow_changes: int
    days_3_4_changes: int
    days_5_7_changes: int
    total_stability_penalty: float


class FairnessMetrics(BaseModel):
    person_id: str
    hated_tasks_assigned: int
    disliked_tasks_assigned: int
    total_assigned: int


class SolverOutput(BaseModel):
    run_id: str
    feasible: bool
    timed_out: bool = False          # True if solver hit timeout but returned best-known result
    assignments: list[AssignmentResult]
    uncovered_slot_ids: list[str]    # slots with fewer than required_headcount filled
    hard_conflicts: list[HardConflict]
    soft_penalty_total: float
    stability_metrics: StabilityMetrics
    fairness_metrics: list[FairnessMetrics]
    explanation_fragments: list[str]  # human-readable notes for admin UI
