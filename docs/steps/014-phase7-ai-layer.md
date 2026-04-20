# Step 014 — Phase 7: AI Assistant Layer

## Phase
Phase 7 — Optional AI Layer

## Purpose
Add an optional AI assistant that helps admins parse natural language instructions into structured constraints, summarize schedule diffs, and explain infeasibility. The AI never makes scheduling decisions — it only assists with parsing and explanation. All AI output requires admin confirmation before being stored.

## What was built

### Application layer

| File | Description |
|---|---|
| `Application/AI/IAiAssistant.cs` | Interface with 3 methods: ParseConstraint, SummarizeDiff, ExplainInfeasibility. Also defines ParsedConstraintDto, DiffContextDto, InfeasibilityContextDto |
| `Application/AI/Commands/ParseConstraintCommand.cs` | MediatR command — calls AI, returns candidate constraint for admin review |
| `Application/AI/Commands/SummarizeDiffCommand.cs` | MediatR commands for diff summary and infeasibility explanation |

### Infrastructure

| File | Description |
|---|---|
| `Infrastructure/AI/OpenAiAssistant.cs` | GPT-4o implementation; low temperature (0.2) for deterministic output; graceful fallback on failure |
| `Infrastructure/AI/NoOpAiAssistant.cs` | No-op fallback when AI:ApiKey is not configured — app runs fine without AI |

### API

| File | Description |
|---|---|
| `Api/Controllers/AiController.cs` | `POST /spaces/{id}/ai/parse-constraint`, `/summarize-diff`, `/explain-infeasibility` — all require admin_mode permission |

### Frontend

| File | Description |
|---|---|
| `components/admin/AiConstraintParser.tsx` | Input box + parse button + candidate result card with confirm/discard actions |

## Key decisions

### AI is always optional
`Program.cs` checks for `AI:ApiKey`. If absent, `NoOpAiAssistant` is registered — the app starts and runs without any AI dependency. The AI endpoints return graceful fallback messages.

### Two-step confirm flow
The AI parse endpoint returns a `ParsedConstraintDto` with `parsed`, `ruleType`, `scopeHint`, `rulePayloadJson`, and `confidenceNote`. The admin reviews this in the UI and clicks "Confirm and save" — which then calls the existing `CreateConstraintCommand`. Nothing is auto-saved.

### Low temperature for structured output
OpenAI calls use `temperature: 0.2` to minimize hallucination and keep output deterministic. The system prompt instructs the model to return JSON only for constraint parsing.

### Locale-aware responses
All three AI methods accept a `locale` parameter and instruct the model to respond in Hebrew, English, or Russian accordingly.

## How to run / verify

```bash
# Set AI key in appsettings or environment
export AI__ApiKey="sk-..."

# Parse a constraint
curl -X POST "http://localhost:5000/spaces/$SPACE/ai/parse-constraint" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"input":"Ofek cannot do kitchen for 10 days"}'

# Without AI key — returns graceful fallback
# {"parsed":false,"confidenceNote":"AI assistant is not configured..."}
```

## What comes next
- CI/CD pipeline (GitHub Actions) — built in same commit
- AWS deployment configuration

## Git commit

```bash
git add -A && git commit -m "feat(phase7): AI assistant layer with constraint parser, diff summarizer, infeasibility explainer"
```
