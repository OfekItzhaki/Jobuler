---
inclusion: always
---

# Step Documentation Rule

For every implementation step, feature, or meaningful change made in this repository, you MUST create a corresponding documentation file under `docs/steps/`.

## File naming convention

`docs/steps/{NNN}-{short-kebab-description}.md`

Examples:
- `docs/steps/001-monorepo-scaffold.md`
- `docs/steps/002-database-migrations.md`
- `docs/steps/003-auth-jwt.md`

## Required file content

Each step doc must include:

1. **Title** — what this step is
2. **Phase** — which implementation phase it belongs to (e.g. Phase 1 — Foundation)
3. **Purpose** — why this step exists and what problem it solves
4. **What was built** — files created or modified, with a brief description of each
5. **Key decisions** — any architectural or design choices made during this step
6. **How it connects** — how this step relates to other parts of the system
7. **How to run / verify** — how a developer can confirm this step works
8. **What comes next** — what depends on this step

## When to create the doc

- Create the step doc at the same time as the implementation, not after.
- If a step spans multiple files, one doc covers the whole step.
- If a step is purely documentation or config with no logic, a brief doc is still required.

## Goal

These docs exist so any developer (or agent) can read `docs/steps/` in order and understand the full system — what was built, why, and how it fits together — without reading all the source code.

## Git commit rule

After every completed step, you MUST produce a git commit command for the user to run.

Format:
```bash
git add -A && git commit -m "<type>(<scope>): <short description>"
```

Commit message conventions:
- `feat(phase1): monorepo scaffold and docker setup`
- `feat(phase2): people, groups, tasks, constraints domain and API`
- `feat(phase3): solver payload normalization and CP-SAT constraints`
- Use `feat` for new functionality, `fix` for bug fixes, `chore` for config/tooling

The commit command must appear at the end of every step doc under a `## Git commit` section.
The user runs the command manually — you do not run git commands yourself.
