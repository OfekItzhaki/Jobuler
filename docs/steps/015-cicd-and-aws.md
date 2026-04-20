# Step 015 — CI/CD and AWS Deployment

## Phase
Phase 6 — Hardening (infrastructure)

## Purpose
Set up GitHub Actions CI for all three services and a deployment pipeline that builds Docker images, pushes to AWS ECR, and deploys to ECS Fargate on every push to main.

## What was built

| File | Description |
|---|---|
| `.github/workflows/ci.yml` | CI: builds API (.NET), lints/tests solver (Python), type-checks and builds frontend (Next.js) |
| `.github/workflows/deploy.yml` | CD: builds and pushes Docker images to ECR, deploys to ECS on push to main |
| `infra/aws/ecs-task-api.json` | ECS Fargate task definition for the API (512 CPU, 1024 MB, secrets from Secrets Manager) |
| `infra/aws/ecs-task-solver.json` | ECS Fargate task definition for the solver (1024 CPU, 2048 MB — more memory for OR-Tools) |
| `infra/aws/README.md` | Step-by-step AWS setup guide: ECR repos, ECS cluster, RDS, ElastiCache, Secrets Manager |

## Key decisions

### Secrets from AWS Secrets Manager
All sensitive config (DB connection, JWT secret, Redis, OpenAI key) is stored in Secrets Manager and injected into ECS containers as environment variables at runtime. Never in the image or source code.

### Solver gets more memory
OR-Tools CP-SAT can be memory-intensive for large scheduling problems. The solver task definition uses 2048 MB vs 1024 MB for the API.

### Deploy only on main
CI runs on all branches and PRs. Deployment only triggers on push to `main` or manual `workflow_dispatch`. This prevents accidental deploys from feature branches.

### ECS wait for stability
The deploy job waits for `ecs wait services-stable` after deploying the API. This ensures the deployment is healthy before the workflow completes.

## Required GitHub Secrets

```
AWS_ACCOUNT_ID
AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY
```

## AWS Architecture

```
ALB → jobuler-api (ECS Fargate)
    → jobuler-web (ECS Fargate)
    → jobuler-solver (internal only, ECS Fargate)

RDS PostgreSQL 16 (Multi-AZ)
ElastiCache Redis
S3 (exports)
Secrets Manager (all secrets)
ECR (Docker images)
CloudWatch Logs (/ecs/jobuler-*)
```

## How to verify

Push to `main` → GitHub Actions tab → CI job should pass all three service builds → Deploy job should push images and update ECS services.

## Git commit

```bash
git add -A && git commit -m "feat(infra): GitHub Actions CI/CD and AWS ECS deployment config"
```
