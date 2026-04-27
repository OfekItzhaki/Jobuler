# Step 059 — Husky + lint-staged Pre-Commit Hooks

## Phase
Phase DX — Developer Experience & Tooling

## Purpose
Enforce code quality at commit time without relying on CI alone. Prevents broken TypeScript/ESLint code and failing .NET builds from entering the repository.

## What was built

| File | Description |
|------|-------------|
| `package.json` (root) | Minimal root package.json with `"prepare": "husky"` and husky v9 as a devDependency |
| `apps/web/package.json` | Added `husky` and `lint-staged` devDependencies; added `lint-staged` config to auto-fix ESLint on staged `.ts`/`.tsx` files |
| `.husky/pre-commit` | Pre-commit hook script: runs lint-staged for frontend files, then conditionally runs `dotnet build` if any `.cs` files are staged |

## Key decisions

- **Husky v9 syntax** — uses `prepare` script instead of the deprecated `husky install`. Running `npm install` at the root will automatically set up hooks.
- **lint-staged scoped to `apps/web`** — config lives in `apps/web/package.json` and is referenced explicitly via `--config` to avoid ambiguity in the monorepo.
- **Conditional dotnet build** — `dotnet build` only runs when `.cs` files are actually staged, keeping commits fast when only frontend or Python files change.
- **`--no-restore -v quiet`** — skips NuGet restore (assumed already done) and suppresses verbose output so the hook is non-intrusive.
- **No `npm install` required to configure** — all config files are plain JSON/shell; hooks activate after the next `npm install` at the root.

## How it connects

- The root `package.json` is the monorepo's npm workspace entry point for tooling only — it does not manage `apps/web` dependencies directly.
- `apps/web/package.json` already owns the Next.js project; lint-staged is co-located there so `next lint` runs with the correct working directory context.
- The `.husky/pre-commit` script is the single enforcement point for both the JS and C# layers before any commit lands.

## How to run / verify

1. Install dependencies at the root (first time only):
   ```bash
   npm install
   ```
2. Stage a `.ts` or `.tsx` file with a lint error and run `git commit` — ESLint should auto-fix or block the commit.
3. Stage a `.cs` file and run `git commit` — you should see the dotnet build output.
4. Verify the hook file is executable:
   ```bash
   ls -la .husky/pre-commit
   ```

## What comes next

- Add a `pre-push` hook to run the full test suite (`playwright test --run`, `dotnet test`) before pushes to `main`.
- Consider adding `black --check` / `ruff` for the Python solver in the same pre-commit hook.

## Git commit

```bash
git add -A && git commit -m "chore(dx): add husky pre-commit hooks and lint-staged"
```
