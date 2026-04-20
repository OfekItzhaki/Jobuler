# Step 001 — Monorepo Scaffold

## Phase
Phase 1 — Foundation

## Purpose
Establish the project directory structure, root configuration files, Docker setup, and Dockerfiles so every subsequent step has a consistent home and all services can run together locally.

## What was built

| File | Description |
|---|---|
| `README.md` | Project overview, tech stack table, monorepo structure, quick-start instructions |
| `.gitignore` | Ignores for Node, .NET, Python, Docker env files, OS artifacts |
| `infra/compose/.env.example` | Template for all environment variables (DB, Redis, JWT, ports, MinIO) |
| `infra/compose/docker-compose.yml` | Orchestrates all services: postgres, redis, minio, api, solver, web |
| `infra/docker/api.Dockerfile` | Multi-stage build for ASP.NET Core API |
| `infra/docker/solver.Dockerfile` | Python solver service container |
| `infra/docker/web.Dockerfile` | Multi-stage Next.js production build |

## Key decisions

- **Monorepo layout**: `apps/` for runnable services, `packages/` for shared code, `infra/` for all infrastructure concerns. Keeps service boundaries explicit without a complex build tool like Turborepo or Nx at this stage.
- **MinIO for local S3**: Avoids AWS dependency in local dev while keeping the production path to real S3 trivial (same SDK, different endpoint).
- **Docker Compose health checks**: All dependent services (postgres, redis) have health checks so the API container waits for them to be ready before starting.
- **Multi-stage Dockerfiles**: Keeps production images small; build tools are not included in the runtime image.

## How it connects
Every other service (API, solver, frontend, migrations) depends on this scaffold. The `docker-compose.yml` is the single command that brings the full local environment up.

## How to run / verify

```bash
cp infra/compose/.env.example infra/compose/.env
# Edit .env with your local secrets if needed
docker compose -f infra/compose/docker-compose.yml up -d
# All containers should reach healthy/running state
docker compose -f infra/compose/docker-compose.yml ps
```

## What comes next
- Step 002: Database migrations (requires postgres container running)
- Step 003: ASP.NET Core API project structure (uses api.Dockerfile)
- Step 005: Python solver stub (uses solver.Dockerfile)
- Step 006: Next.js frontend shell (uses web.Dockerfile)

## Git commit

```bash
git add -A && git commit -m "feat(phase1): monorepo scaffold, docker compose, and dockerfiles"
```
