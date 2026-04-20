# Jobuler — Forces Scheduler SaaS

A secure, multilingual, multi-tenant scheduling SaaS for force/platoon/shift-based organizations.

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 14 + TypeScript |
| Backend API | ASP.NET Core 8 |
| Solver | Python 3.11 + OR-Tools CP-SAT |
| Database | PostgreSQL 16 |
| Cache / Queue | Redis 7 |
| Storage | S3-compatible (MinIO locally) |

## Monorepo Structure

```
jobuler/
  apps/
    web/          # Next.js frontend
    api/          # ASP.NET Core API
    solver/       # Python OR-Tools solver service
  packages/
    contracts/    # Shared API contracts / DTOs (TypeScript)
    i18n/         # Translation dictionaries (he, en, ru)
  infra/
    docker/       # Dockerfiles per service
    compose/      # Docker Compose files
    migrations/   # PostgreSQL migration SQL files
    scripts/      # Seed data and utility scripts
  docs/
    architecture/ # Architecture decision records
    api/          # API documentation
    product/      # Product specs
```

## Quick Start (Local Dev)

### Prerequisites
- Docker Desktop
- Node.js 20+
- .NET 8 SDK
- Python 3.11+

### Run everything

```bash
# Copy environment template
cp infra/compose/.env.example infra/compose/.env

# Start all services
docker compose -f infra/compose/docker-compose.yml up -d

# Run DB migrations
./infra/scripts/migrate.sh

# Seed demo data
./infra/scripts/seed.sh
```

### Individual services

```bash
# Frontend (dev)
cd apps/web && npm install && npm run dev

# API (dev)
cd apps/api && dotnet run

# Solver (dev)
cd apps/solver && pip install -r requirements.txt && uvicorn main:app --reload
```

## Documentation

- [Technical Specification](docs/kiro-forces-scheduler-tech-spec.md)
- [Agent Instructions](docs/agents.md)
- [Architecture Overview](docs/architecture/overview.md)

## Languages

Hebrew (primary, RTL), English, Russian.

## License

Private — all rights reserved.
