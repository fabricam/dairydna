# Implementation Plan: Thin Vertical Slice

**Branch**: `000-thin-vertical-slice` | **Date**: 2026-07-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/000-thin-vertical-slice/spec.md`

## Summary

Deliver the smallest end-to-end DairyDNA path that answers: given today’s milk
and cream, explicit demand, static prices, trucks, and transport costs, where
should product move to maximize expected contribution margin?

Technical approach: .NET 10 modular monolith (Minimal APIs + Blazor/Semantic UI/Fluxor) under
Aspire, SQL Server via EF Core, seeded thin-slice generator (seed `104729`),
distance-based transport costing, and a deterministic naive allocation
optimizer behind `IAllocationOptimizer` (OR-Tools deferred to Feature 009 with
ADR). Open local access; no auth, no contracts/penalties, no safety stock, no ML.

## Technical Context

**Language/Version**: .NET 10 (C#)

**Primary Dependencies**: ASP.NET Core Minimal APIs (no MVC controllers), Blazor
(Interactive Server), Semantic UI (CSS/JS via wwwroot or CDN), Fluxor (Blazor
state), .NET Aspire, EF Core, Microsoft.Data.SqlClient / EF Core SQL Server
provider, xUnit / FluentAssertions

**Storage**: SQL Server (Aspire-managed for local demo); optional file export of
generation manifests under `data/synthetic/`

**Testing**: xUnit unit + integration; known-answer optimizer fixtures;
generator seed reproducibility tests; WebApplicationFactory for API contracts

**Target Platform**: Local developer workstation (Windows/macOS/Linux) via Aspire

**Project Type**: Web application (Minimal API + Blazor UI) with domain/application
libraries and optional worker for long jobs

**Performance Goals**: Demo home useful content ≤ 2s; thin-slice optimize ≤ 30s
(expected ≪ 1s at this scale)

**Constraints**: Pounds only; single-period 24h; no negative-margin moves;
delivery windows enforced; plant processing capacity deferred; partial fill +
split loads allowed; repro: exact objective/quantities, costs ≤ 0.01 abs diff;
open local access only

**Scale/Scope**: 5 farms, 2 facilities, 5 customers, 3 trucks, 2 products
(raw milk, cream), 90 days history; 1 demo planning day

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I Specs as source of truth | Pass | Spec + this plan; tasks next |
| II Outcomes before tech | Pass | Spec is outcome-first |
| III Deterministic demos | Pass | Seed + optimizer determinism required |
| IV No data leakage | N/A | No forecasting in 000 |
| V Feasibility before profit | Pass | Constraints + never label infeasible as recommended |
| VI Explainable recommendations | Pass | Revenue, transport, margin on each movement |
| VII Honest uncertainty | Pass | Static prices labeled; no forecast claims |
| VIII Modular boundaries | Pass | Domain free of EF/web/solver packages |
| IX Test-first domain/opt | Pass | Known-answer + invariant tests required |
| X Observable behavior | Pass | Aspire + OTel health/logs/traces |
| XI Secure-by-default | **Exception** | Open local access per clarify; see Complexity Tracking |
| XII Performance budgets | Pass | Thin workload within budgets |
| XIII Versioned contracts | Pass | API + dataset schema versions |
| XIV Small increments | Pass | This feature is the thin slice |
| XV Simplicity | Pass | Modular monolith; naive optimizer |

### Post–Phase 1 Re-check

Gates remain Pass with the documented XI exception. Design introduces no new
violations.

## Project Structure

### Documentation (this feature)

```text
specs/000-thin-vertical-slice/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/           # Phase 1
└── tasks.md             # Created by /speckit.tasks (not this command)
```

### Source Code (repository root)

```text
src/
├── DairyDNA.AppHost/
├── DairyDNA.ServiceDefaults/
├── DairyDNA.Web/                 # Blazor + Semantic UI + Fluxor
├── DairyDNA.Api/                 # Minimal APIs only (no controllers)
├── DairyDNA.Domain/
├── DairyDNA.Application/
├── DairyDNA.Infrastructure/
├── DairyDNA.DataGenerator/
├── DairyDNA.Optimization/        # Naive optimizer + interfaces
└── DairyDNA.Worker/              # Optional for generate/optimize jobs

tests/
├── DairyDNA.UnitTests/
├── DairyDNA.IntegrationTests/
├── DairyDNA.ContractTests/
└── DairyDNA.Optimization.Tests/

data/
├── synthetic/
└── ...
DairyDNA.sln
```

**Structure Decision**: Program layout (Aspire web app + modular libraries).
Forecasting/Ingestion projects are **not** required for 000 and SHOULD be
omitted until later features.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Constitution XI — no auth on generate/optimize | Clarify session locked open local demo access for interview speed (FR-006/FR-008) | Basic auth would slow first E2E proof; auth deferred to demo hardening with explicit non-prod binding |
| Temporary naive optimizer instead of OR-Tools (FR-007) | Prove allocation loop with deterministic known-answer tests at tiny scale | OR-Tools now adds package/solver complexity before architecture is proven; Feature 009 becomes system of record via ADR |

## Phase Outputs

- [research.md](./research.md) — resolved technical decisions
- [data-model.md](./data-model.md) — entities and validation
- [contracts/openapi.yaml](./contracts/openapi.yaml) — HTTP API
- [quickstart.md](./quickstart.md) — local validation path

## Application Workflows

1. **Generate**: `POST /api/generation-runs` with thin-slice config → persist
   entities + `GenerationManifest` → status Complete.
2. **Demo home**: `GET` summary endpoints → Blazor dashboard (inventory, demand,
   prices, fleet).
3. **Optimize**: `POST /api/optimization-runs` with `asOfDate` → build inputs →
   cost matrix → naive optimizer → persist run + movements → UI table.
4. **Inspect**: `GET /api/optimization-runs/{id}` with movements, unused
   inventory, unserved demand, objective, status.

## Failure Handling

- Validation errors → 400 with problem details
- Generation/optimize failures → run status Failed + diagnostic message
- Infeasible (no legal assignment exists when required by fixture) → status
  `Infeasible` (not Recommended)
- Empty demand → `Feasible` with zero movements and full unused inventory

## Observability

- Health checks: API + SQL Server
- Structured logs: generation seed, run ids, optimize duration, objective
- Traces across Web → API → optimizer
- Metrics: generation duration, optimize duration, feasibility status

## Security (000)

- No authentication
- Input validation on all writes
- Local/dev binding only; do not document as production-safe
- Synthetic names only; no real PII

## Testing Strategy

- Domain invariant unit tests
- Transport cost formula tests
- Generator seed reproducibility
- Optimizer known-answer fixtures (SC-003): one origin/one customer; higher
  price worse after transport; insufficient capacity; negative-margin hold;
  expired inventory excluded; zero demand
- API contract tests against OpenAPI
- Optional Aspire integration smoke for generate → optimize → get
- Manual timing check vs ≤2s demo / ≤30s optimize budgets (T061)

## Risks

| Risk | Mitigation |
|------|------------|
| Naive optimizer diverges from future OR-Tools | ADR + interface; Feature 009 replaces implementation |
| Semantic UI JS interactivity with Blazor | Prefer CSS components; limit JS behaviors |
| Cascading component state sprawl | Use Fluxor stores/actions/effects for demo summary, generation, and optimization UI state |
| Floating cost diffs | Quantize currency to 2 decimals; tolerance 0.01 |
| Terminology drift (Recommendations vs OptimizationRun) | API uses OptimizationRun; UI may label Recommendations |
