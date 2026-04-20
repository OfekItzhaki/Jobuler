# AGENTS.md

## Purpose

This repository contains a secure, multilingual, multi-tenant scheduling SaaS for force/platoon/shift-based organizations.

All coding agents working in this repository must treat the technical specification in `/docs/kiro-forces-scheduler-tech-spec.md` as the primary source of truth.

## Source of truth

Before making architectural or scheduling-related changes, read:

- `/docs/kiro-forces-scheduler-tech-spec.md`

If code, comments, assumptions, or older docs conflict with that file, stop and raise the conflict before continuing.

## Locked decisions

The following decisions are already made and must not be silently changed:

- The scheduling engine is solver-first, not AI-first.
- The core scheduler must use a deterministic constraint solver.
- AI is optional and may only be used later as a helper for parsing, explanation, and summarization.
- Every meaningful admin change triggers recomputation of the full 7-day schedule horizon.
- The strongest schedule stability requirement is for today and tomorrow.
- Days 3-7 may change more freely, but unnecessary changes should still be minimized.
- Normal users are read-only.
- Admin mode is permission-gated.
- Published schedules are immutable snapshots and must never be edited in place.
- Rollback must create a new version based on an earlier version.
- The system must support multilingual UI: Hebrew first, then English and Russian.
- Multi-tenant isolation, security, auditability, and logs are first-class requirements.
- Dynamic operational roles exist per space and are user-defined data, not hardcoded enums.
- Space and group constraints can be hard or soft.
- Individual constraints are hard by default.

## Product rules

- Users authenticate and enter viewer mode by default.
- Authorized users may switch into admin mode.
- Normal users cannot modify scheduling, personnel, constraints, publication state, or permissions.
- Admin changes should be made in a draft workflow.
- Saving draft changes must trigger asynchronous re-optimization.
- The solver must prefer minimal disturbance from the current baseline.
- Disturbance penalties must be time-weighted, with the highest penalty in the next 48 hours.
- Emergency changes should be handled as admin-entered operational updates followed by automatic re-optimization.
- Manual editing should not be the normal workflow.

## Required tech stack

Unless explicitly instructed otherwise, follow this stack:

- Frontend: Next.js + TypeScript
- Backend API: ASP.NET Core
- Database: PostgreSQL
- Cache/queue: Redis
- Solver service: Python + OR-Tools CP-SAT
- Storage: S3-compatible object storage for exports if needed

If you believe a different stack is required, explain why before changing direction.

## Architecture rules

- Design for multi-tenancy from the start.
- Enforce tenant isolation in the database layer as well as the application layer.
- Prefer PostgreSQL Row-Level Security for tenant isolation.
- Treat schedule versions as top-level immutable entities with child assignments.
- Do not build published schedule data around mutable in-place assignment edits.
- Keep solver contracts explicit and testable.
- Keep scheduling logic out of the frontend.
- Separate orchestration logic from solver logic.
- Separate operational restriction visibility from sensitive reason visibility when possible.
- Make all critical state transitions auditable.

## Security rules

- Use least privilege by default.
- Do not add write access for normal users.
- Publish, rollback, permissions changes, and ownership transfer must require elevated permissions.
- Sensitive notes or reasons must not be broadly visible.
- Do not weaken authentication, authorization, logging, or tenant isolation for convenience.

## Implementation workflow

Before coding a major feature:

1. Read the spec.
2. Inspect the current repository state.
3. Identify impacted layers: frontend, backend, database, solver, queue, docs.
4. Produce a short plan.
5. Implement in small, reviewable steps.
6. Summarize changes, risks, and next steps.

## Coding expectations

- Prefer production-minded architecture over hacks.
- Prefer explicit domain models and contracts.
- Keep business rules centralized and testable.
- Do not invent product behavior when the spec is unclear; ask clarifying questions.
- Keep comments focused and useful.
- Update docs when architecture or implementation meaningfully evolves.
- Keep migrations, contracts, and configuration aligned.

## Scheduling-specific expectations

The solver must support:

- Full 7-day recomputation.
- Strongest stability weighting for today and tomorrow.
- Lower, but still present, stability weighting for later days.
- Hard constraints.
- Soft constraints.
- Fairness/burden balancing.
- No overlap by default.
- Explicit overlap support only where configured.
- Version-to-version diffing.
- Explanation-friendly output for logs and admin review.

## Phase discipline

Do not jump ahead and implement later-stage features when working on an earlier phase.

Specifically:
- Do not build AI assistant features before the scheduling core is working.
- Do not polish UI before core domain modeling, tenancy, permissions, and versioning are sound.
- Do not defer security and versioning until “later.”

## If a conflict is found

If you find any of the following, stop and explain before proceeding:

- The spec conflicts with the existing codebase.
- The requested change conflicts with a locked decision.
- A feature would break immutability, tenant isolation, or permission boundaries.
- The current structure makes the solver-first architecture hard to preserve.

## Preferred deliverable style

When asked to implement something, respond with:

1. Short understanding summary.
2. Proposed plan.
3. Files to create/change.
4. Implementation.
5. Summary of what changed.
6. Remaining risks or follow-ups.