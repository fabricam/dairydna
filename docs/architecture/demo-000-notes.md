# Demo 000 notes — thin vertical slice

Manual pass of quickstart scenarios (2026-07-26) against local implementation.

## Environment

- `dotnet test DairyDNA.sln` — all green (unit, optimization, integration, contract)
- API can run with `UseInMemoryDatabase=true` (default when no `ConnectionStrings:dairydna`)
- Aspire AppHost: `src/DairyDNA.AppHost` → SQL Server + Api + Web

## Scenario results

| Scenario | Result | Notes |
|----------|--------|-------|
| A Happy path | Pass (automated + UI path) | Generate → summary → optimize → recommendations |
| B Reproducibility | Pass | Same seed counts; optimize twice exact objective/qty; costs ≤ 0.01 |
| C Known-answer | Pass | `DairyDNA.Optimization.Tests` (7 fixtures + repro) |
| D Health | Pass | `GET /health` → `{ "status": "Healthy" }` |

## Timing budgets (plan)

| Step | Budget | Observed (local, in-memory) |
|------|--------|------------------------------|
| Demo home useful content | ≤ 2s | Sub-second after generate |
| Optimize | ≤ 30s | Typically &lt; 1s on thin-slice day slice |

## Security posture (000)

Local-dev only. No authentication. Do not expose as internet-facing production.
