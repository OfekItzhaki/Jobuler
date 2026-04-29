"""
Hard constraint implementations for the CP-SAT model.
Each function receives the model, decision variables, and relevant input data,
and adds the appropriate constraints to the model.
"""
from ortools.sat.python import cp_model
from models.solver_input import SolverInput, TaskSlot, HardConstraint
from datetime import datetime, timezone


def add_headcount_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    num_people: int
):
    """
    Each slot must be filled to AT LEAST required_headcount people.

    Using >= (not ==) is critical: if a slot cannot be fully staffed due to
    availability, rest constraints, or insufficient eligible people, the solver
    must still be able to produce a feasible (partial) solution rather than
    declaring the entire model INFEASIBLE.  The coverage objective in
    objectives.py penalises any shortfall, so the solver will always try to
    maximise staffing — but it won't fail the whole schedule just because one
    slot is short.

    Upper bound (at most num_people per slot) is implicitly enforced by the
    bool-var domain [0,1] and the no-duplicate constraint.
    """
    for s_idx, slot in enumerate(slots):
        model.add(
            sum(assign[(s_idx, p_idx)] for p_idx in range(num_people))
            >= slot.required_headcount
        )


def add_no_duplicate_assignment(
    model: cp_model.CpModel,
    assign: dict,
    num_slots: int,
    num_people: int
):
    """A person cannot be assigned twice to the same slot."""
    for s_idx in range(num_slots):
        for p_idx in range(num_people):
            model.add(assign[(s_idx, p_idx)] <= 1)


def add_no_overlap_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    num_people: int
):
    """
    A person cannot be assigned to two overlapping slots unless
    both task types explicitly allow overlap.
    """
    for p_idx in range(num_people):
        for s1_idx, slot1 in enumerate(slots):
            for s2_idx, slot2 in enumerate(slots):
                if s2_idx <= s1_idx:
                    continue

                # Check if slots overlap in time
                if not _slots_overlap(slot1, slot2):
                    continue

                # If either task type forbids overlap, enforce mutual exclusion
                if not (slot1.allows_overlap and slot2.allows_overlap):
                    model.add(
                        assign[(s1_idx, p_idx)] + assign[(s2_idx, p_idx)] <= 1
                    )


def add_min_rest_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    num_people: int,
    min_rest_hours: float = 8.0
):
    """
    A person must have at least min_rest_hours between the end of one
    assignment and the start of the next.
    """
    min_rest_seconds = int(min_rest_hours * 3600)

    for p_idx in range(num_people):
        for s1_idx, slot1 in enumerate(slots):
            for s2_idx, slot2 in enumerate(slots):
                if s2_idx <= s1_idx:
                    continue

                end1 = _to_timestamp(slot1.ends_at)
                start2 = _to_timestamp(slot2.starts_at)
                end2 = _to_timestamp(slot2.ends_at)
                start1 = _to_timestamp(slot1.starts_at)

                # slot1 ends before slot2 starts — check rest gap
                if end1 <= start2 and (start2 - end1) < min_rest_seconds:
                    model.add(
                        assign[(s1_idx, p_idx)] + assign[(s2_idx, p_idx)] <= 1
                    )

                # slot2 ends before slot1 starts — check rest gap
                if end2 <= start1 and (start1 - end2) < min_rest_seconds:
                    model.add(
                        assign[(s1_idx, p_idx)] + assign[(s2_idx, p_idx)] <= 1
                    )


def add_qualification_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    people,
    num_people: int
):
    """
    A person can only be assigned to a slot if they hold all required qualifications.
    """
    for s_idx, slot in enumerate(slots):
        if not slot.required_qualification_ids:
            continue
        required = set(slot.required_qualification_ids)
        for p_idx, person in enumerate(people):
            person_quals = set(person.qualification_ids)
            if not required.issubset(person_quals):
                model.add(assign[(s_idx, p_idx)] == 0)


def add_role_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    people,
    num_people: int
):
    """
    A person can only be assigned to a slot if they hold at least one required role.
    If no roles are required, anyone is eligible.
    """
    for s_idx, slot in enumerate(slots):
        if not slot.required_role_ids:
            continue
        required = set(slot.required_role_ids)
        for p_idx, person in enumerate(people):
            person_roles = set(person.role_ids)
            if not required.intersection(person_roles):
                model.add(assign[(s_idx, p_idx)] == 0)


def add_restriction_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    people,
    num_people: int,
    hard_constraints: list[HardConstraint]
):
    """
    Apply individual no-assignment restrictions from hard constraints.
    rule_type: no_task_type_restriction
    payload: { "person_id": "...", "task_type_id": "..." }
    """
    restrictions = [
        c for c in hard_constraints
        if c.rule_type == "no_task_type_restriction"
    ]

    for constraint in restrictions:
        person_id = constraint.payload.get("person_id") or constraint.scope_id
        task_type_id = str(constraint.payload.get("task_type_id", ""))

        if not person_id or not task_type_id:
            continue

        for p_idx, person in enumerate(people):
            if person.person_id != person_id:
                continue
            for s_idx, slot in enumerate(slots):
                if slot.task_type_id == task_type_id:
                    model.add(assign[(s_idx, p_idx)] == 0)


def add_kitchen_frequency_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    people,
    num_people: int,
    hard_constraints: list[HardConstraint],
    fairness_counters
):
    """
    Kitchen cannot exceed max assignments per rolling 7-day window.
    rule_type: max_kitchen_per_week
    payload: { "max": 2, "task_type_name": "kitchen" }
    """
    kitchen_rules = [
        c for c in hard_constraints
        if c.rule_type == "max_kitchen_per_week"
    ]

    for rule in kitchen_rules:
        max_allowed = int(rule.payload.get("max", 2))
        task_type_name = str(rule.payload.get("task_type_name", "")).lower()

        kitchen_slot_indices = [
            s_idx for s_idx, slot in enumerate(slots)
            if slot.task_type_name.lower() == task_type_name or
               slot.task_type_id == rule.payload.get("task_type_id", "")
        ]

        if not kitchen_slot_indices:
            continue

        # Build a counter map from fairness history
        kitchen_history = {
            f.person_id: f.kitchen_count_7d
            for f in fairness_counters
        }

        for p_idx, person in enumerate(people):
            already_done = kitchen_history.get(person.person_id, 0)
            remaining_allowed = max(0, max_allowed - already_done)

            model.add(
                sum(assign[(s_idx, p_idx)] for s_idx in kitchen_slot_indices)
                <= remaining_allowed
            )


def add_availability_constraints(
    model: cp_model.CpModel,
    assign: dict,
    slots: list[TaskSlot],
    people,
    num_people: int,
    availability_windows,
    presence_windows
):
    """
    A person cannot be assigned to a slot if they are marked at_home
    or if the slot falls outside all their availability windows (when windows exist).
    """
    # Build at-home set: person_id -> list of (start, end) timestamps
    at_home = {}
    for pw in presence_windows:
        if pw.state == "at_home":
            at_home.setdefault(pw.person_id, []).append(
                (_to_timestamp(pw.starts_at), _to_timestamp(pw.ends_at))
            )

    # Build availability map: person_id -> list of (start, end) timestamps
    avail_map = {}
    for aw in availability_windows:
        avail_map.setdefault(aw.person_id, []).append(
            (_to_timestamp(aw.starts_at), _to_timestamp(aw.ends_at))
        )

    for p_idx, person in enumerate(people):
        pid = person.person_id

        for s_idx, slot in enumerate(slots):
            slot_start = _to_timestamp(slot.starts_at)
            slot_end   = _to_timestamp(slot.ends_at)

            # Block if person is at home during this slot
            if pid in at_home:
                for home_start, home_end in at_home[pid]:
                    if slot_start < home_end and slot_end > home_start:
                        model.add(assign[(s_idx, p_idx)] == 0)
                        break

            # Block if person has availability windows but none cover this slot
            if pid in avail_map:
                covered = any(
                    a_start <= slot_start and a_end >= slot_end
                    for a_start, a_end in avail_map[pid]
                )
                if not covered:
                    model.add(assign[(s_idx, p_idx)] == 0)


# ── Helpers ───────────────────────────────────────────────────────────────────

def _slots_overlap(slot1: TaskSlot, slot2: TaskSlot) -> bool:
    s1 = _to_timestamp(slot1.starts_at)
    e1 = _to_timestamp(slot1.ends_at)
    s2 = _to_timestamp(slot2.starts_at)
    e2 = _to_timestamp(slot2.ends_at)
    return s1 < e2 and s2 < e1


def _to_timestamp(dt) -> int:
    """Convert datetime or ISO string to Unix timestamp (seconds)."""
    if isinstance(dt, (int, float)):
        return int(dt)
    if isinstance(dt, str):
        dt = datetime.fromisoformat(dt.replace("Z", "+00:00"))
    if dt.tzinfo is None:
        dt = dt.replace(tzinfo=timezone.utc)
    return int(dt.timestamp())
