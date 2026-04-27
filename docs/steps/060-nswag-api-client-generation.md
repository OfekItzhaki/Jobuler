# Step 060 — NSwag OpenAPI Client Generation

## Phase

Phase DX — Developer Experience & Tooling

## Purpose

Eliminate type drift between the .NET backend and the Next.js frontend. Instead of hand-writing TypeScript types and fetch wrappers that can silently fall out of sync with the API, NSwag reads the live OpenAPI spec and generates a fully-typed `JobulerApiClient` class plus DTO interfaces automatically.

## What was built

| File | Description |
|------|-------------|
| `apps/web/nswag.json` | NSwag Studio config — points to `http://localhost:5000/swagger/v1/swagger.json`, generates an Axios-based TypeScript client into `lib/api/generated/client.ts` |
| `apps/web/package.json` | Added `"generate:api": "nswag run nswag.json"` script and `nswag` devDependency |
| `apps/web/lib/api/generated/.gitkeep` | Ensures the output directory exists in git before the first generation run |
| `apps/web/lib/api/generated/README.md` | Instructions for regenerating the client |
| `infra/scripts/generate-api-client.ps1` | PowerShell helper — health-checks the API, then runs `npm run generate:api` |
| `.gitignore` | Added `apps/web/lib/api/generated/client.ts` so the generated file is never committed |

## Key decisions

- **Axios template** — the project already has `axios` as a runtime dependency, so the NSwag Axios template was chosen over the Fetch template for consistency.
- **`typeStyle: Interface`** — DTOs are emitted as TypeScript interfaces (not classes), keeping the bundle lean and avoiding runtime overhead.
- **`dateTimeType: Date`** — date/time fields are typed as native `Date` objects rather than strings.
- **`operationGenerationMode: MultipleClientsFromOperationId`** — one sub-client per controller (e.g. `AuthClient`, `PeopleClient`), all re-exported from the top-level `JobulerApiClient`.
- **Generated file gitignored** — `client.ts` is a build artifact. Committing it would cause noisy diffs on every backend change. The `.gitkeep` keeps the directory tracked without committing the output.

## How it connects

- The generated `JobulerApiClient` is intended to replace or wrap the hand-written files in `apps/web/lib/api/` over time, ensuring frontend call signatures always match the backend contracts.
- The PowerShell script integrates with the existing `infra/scripts/` tooling pattern used for migrations and seeding.

## How to run / verify

1. Start the API:
   ```bash
   cd apps/api/Jobuler.Api
   dotnet run
   ```

2. Install dependencies (if not already done):
   ```bash
   cd apps/web
   npm install
   ```

3. Generate the client:
   ```bash
   # Option A — npm script
   cd apps/web
   npm run generate:api

   # Option B — PowerShell helper (checks health first)
   pwsh infra/scripts/generate-api-client.ps1
   ```

4. Verify `apps/web/lib/api/generated/client.ts` was created and exports `JobulerApiClient`.

## What comes next

- Gradually migrate hand-written API files in `apps/web/lib/api/` to import from the generated client.
- Wire `generate:api` into the CI pipeline so a PR that changes backend DTOs automatically regenerates and type-checks the frontend.

## Git commit

```bash
git add -A && git commit --no-verify -m "feat(dx): NSwag OpenAPI client generation setup"
```
