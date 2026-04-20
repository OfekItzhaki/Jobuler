# AWS Infrastructure

## Architecture

```
Internet
  │
  ▼
ALB (Application Load Balancer)
  ├── /api/*  → ECS Service: jobuler-api  (Fargate, port 8080)
  ├── /*      → ECS Service: jobuler-web  (Fargate, port 3000)
  └── internal only → ECS Service: jobuler-solver (Fargate, port 8000)

ECS Cluster: jobuler-cluster (Fargate)
  ├── jobuler-api     — ASP.NET Core API
  ├── jobuler-solver  — Python OR-Tools solver
  └── jobuler-web     — Next.js frontend

Managed Services:
  ├── RDS PostgreSQL 16  — jobuler-db (Multi-AZ in production)
  ├── ElastiCache Redis  — jobuler-cache (cluster mode)
  └── S3 Bucket          — jobuler-exports (CSV/PDF exports)

Secrets: AWS Secrets Manager
  ├── jobuler/db-connection
  ├── jobuler/redis-connection
  ├── jobuler/jwt-secret
  └── jobuler/openai-key (optional)
```

## Required GitHub Secrets

Set these in GitHub → Settings → Secrets and variables → Actions:

| Secret | Description |
|---|---|
| `AWS_ACCOUNT_ID` | Your 12-digit AWS account ID |
| `AWS_ACCESS_KEY_ID` | IAM user access key (deploy role) |
| `AWS_SECRET_ACCESS_KEY` | IAM user secret key |

## Required IAM Permissions for Deploy Role

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Action": ["ecr:*"], "Resource": "*" },
    { "Effect": "Allow", "Action": ["ecs:UpdateService", "ecs:DescribeServices"], "Resource": "*" },
    { "Effect": "Allow", "Action": ["iam:PassRole"], "Resource": "arn:aws:iam::*:role/ecsTaskExecutionRole" }
  ]
}
```

## First-time Setup

1. Create ECR repositories:
```bash
aws ecr create-repository --repository-name jobuler-api
aws ecr create-repository --repository-name jobuler-solver
aws ecr create-repository --repository-name jobuler-web
```

2. Create ECS cluster:
```bash
aws ecs create-cluster --cluster-name jobuler-cluster --capacity-providers FARGATE
```

3. Create RDS PostgreSQL instance (replace values):
```bash
aws rds create-db-instance \
  --db-instance-identifier jobuler-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 16 \
  --master-username jobuler \
  --master-user-password <password> \
  --allocated-storage 20
```

4. Store secrets in Secrets Manager:
```bash
aws secretsmanager create-secret --name jobuler/jwt-secret --secret-string "<your-jwt-secret>"
aws secretsmanager create-secret --name jobuler/db-connection --secret-string "Host=...;Database=jobuler;..."
```

5. Replace `ACCOUNT_ID` in `ecs-task-*.json` with your actual account ID.

6. Register task definitions:
```bash
aws ecs register-task-definition --cli-input-json file://infra/aws/ecs-task-api.json
aws ecs register-task-definition --cli-input-json file://infra/aws/ecs-task-solver.json
```

7. Push to `main` — GitHub Actions will build, push to ECR, and deploy to ECS automatically.
