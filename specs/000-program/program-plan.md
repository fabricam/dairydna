# DairyDNA — Spec Kit Program Plan (Improved)

**Product**: DairyDNA  
**Formerly drafted as**: DairyFlow  
**Date**: 2026-07-26  
**Status**: Approved program baseline for Spec Kit execution  
**Repo**: `dairydna` (existing; do not recreate as `dairy-flow`)

---

## 1. Verdict on the Original Plan

The original Spec Kit plan is strong: clear vision, domain model, phased
features, ML discipline, optimization feasibility rules, and an honest demo
boundary. It is already closer to a production Spec Kit program than a typical
brainstorm dump.

It needed hardening in six places:

1. **Product rename and repo alignment** — DairyDNA / `dairydna`, not DairyFlow.
2. **Artifact shape** — one mega-document mixed vision, constitution prompts,
   feature specs, and tech plans. Spec Kit needs constitution + per-feature
   `spec.md` / `plan.md` / `tasks.md`.
3. **Unresolved product decisions** — many `/speckit.clarify` questions were
   left open; demos stall without defaults.
4. **Internal inconsistencies** — product count (6 vs 8), Spec Kit paths
   (`memory/` vs `.specify/memory/`), and feature 001 including recommendation
   entities before optimization exists.
5. **Missing thin vertical slice** — section 38 was correct advice but not a
   first-class Spec Kit feature ahead of full-scale foundation work.
6. **Pinned stack choices** — database and .NET version were left ambiguous in
   the draft; this plan pins **SQL Server** and **.NET 10**.

This program plan incorporates those fixes. Feature work still follows the
normal Spec Kit loop per numbered feature.

---

## 2. Product Vision

DairyDNA helps dairy organizations decide:

- How much milk and cream will be available.
- Where milk and cream are currently located.
- Where inventory should be moved.
- Which customers or markets should receive available product.
- Whether product should be sold, stored, separated, or processed.
- Which movement plan produces the greatest expected contribution margin.
- How changing market conditions affect operational decisions.

The system combines synthetic operational data, public market-data ingestion,
supply/demand/price forecasting, transportation costing, constraint-based
shipment optimization, scenario simulation, and explainable recommendations.

**Release 1 honesty boundary**: demonstration platform only. No real trades,
truck dispatch, or production-grade market advice.

### Contribution Margin Model

```text
Expected contribution margin
=
Expected sales revenue
- transportation cost
- handling cost
- processing cost
- storage cost
- spoilage risk cost
- contract penalty cost
```

Feasibility always beats predicted revenue.

---

## 3. Repository & Spec Kit Bootstrap

```powershell
cd c:\repos\dairydna
uv tool install specify-cli
specify init . --integration copilot --ai-skills
```

If Spec Kit files already exist, do not re-init blindly; merge carefully.

### Target Source Layout

```text
dairydna/
├── .specify/
│   └── memory/
│       └── constitution.md
├── specs/
│   ├── 000-program/                 # this program plan + clarifications
│   ├── 001-foundation-and-domain/
│   ├── 002-synthetic-data-generator/
│   └── ...
├── docs/
│   ├── domain/
│   ├── architecture/
│   ├── data/
│   └── ml/
├── src/
│   ├── DairyDNA.AppHost/
│   ├── DairyDNA.ServiceDefaults/
│   ├── DairyDNA.Web/
│   ├── DairyDNA.Api/
│   ├── DairyDNA.Domain/
│   ├── DairyDNA.Application/
│   ├── DairyDNA.Infrastructure/
│   ├── DairyDNA.DataGenerator/
│   ├── DairyDNA.DataIngestion/
│   ├── DairyDNA.Forecasting/
│   ├── DairyDNA.Optimization/
│   └── DairyDNA.Worker/
├── tests/
│   ├── DairyDNA.UnitTests/
│   ├── DairyDNA.IntegrationTests/
│   ├── DairyDNA.ContractTests/
│   ├── DairyDNA.Forecasting.Tests/
│   └── DairyDNA.Optimization.Tests/
├── data/
│   ├── raw/
│   ├── synthetic/
│   ├── processed/
│   └── models/
└── DairyDNA.sln
```

---

## 4. Pinned Technical Baseline

| Concern | Decision | Rationale |
|--------|----------|-----------|
| Runtime | .NET 10 | Current supported stack as of mid-2026 |
| UI | Blazor (interactive server for demo) + Semantic UI + Fluxor | Fast demo UX with Semantic UI styling and Fluxor for client state |
| API | ASP.NET Core Minimal APIs (no controllers) | OpenAPI contracts, validation; endpoint groups instead of MVC controllers |
| Orchestration | .NET Aspire | Local composition + OpenTelemetry |
| OLTP DB | SQL Server | Aligns with typical .NET enterprise demos; Aspire-friendly locally |
| ORM | EF Core | Standard .NET persistence |
| Forecasting | ML.NET behind interfaces | Replaceable algorithms |
| Optimization | Google OR-Tools behind interfaces | LP/MIP for allocation |
| Architecture | Modular monolith + workers | Avoid premature microservices |
| Units | Pounds | Single unit for v1; conversion later |
| Planning cadence | Daily decisions, 24h default horizon | Matches demo story |

Database and .NET version may be revised only via a plan amendment and ADR.

---

## 5. Scope

### In Scope (Demo Release)

- Synthetic farms, plants, customers, products, trucks, contracts, orders,
  inventory, prices, weather, shipments, disruptions.
- Selected public dairy, weather, and fuel-price ingestion.
- Daily/weekly supply, demand, and price forecasts.
- Transportation cost estimation.
- Shipment/allocation optimization.
- Scenario comparison and historical replay.
- Dashboard visualization and recommendation explanations.
- Model/optimizer evaluation and reproducible demos.

### Out of Scope (Initial Release)

- Autonomous dispatch, real trading, billing/payments.
- Real farm/customer identities, telematics, driver payroll.
- Turn-by-turn navigation, regulatory certification.
- Auto-execution of recommendations, production auto-retraining.
- General-purpose LLM assistant, multi-tenant SaaS billing.

---

## 6. Personas

| Persona | Primary need |
|--------|----------------|
| Operations Planner | Supply visibility and movement decisions |
| Commodity / Sales Manager | Net-value customer/market comparison |
| Plant Manager | Incoming inventory, capacity, disruptions |
| Logistics Coordinator | Feasible tanker moves, timing, cost |
| Data Scientist | Reproducible experiments, metrics, versions |
| Interview Reviewer | Clear problem, architecture, and value story |

---

## 7. Domain Model (Program Baseline)

Retain the original entities with DairyDNA naming. Key corrections:

| Topic | Decision |
|------|----------|
| Full catalog products | 8: Raw milk, Cream, Skim milk, Class I fluid milk, Class II cream product, Butter, Cheese milk, Dry whey input |
| Default demo catalog | Start with **Raw milk + Cream** in the thin slice; expand to 6 movable products for standard demo; keep full 8 in reference data |
| Recommendation entities | Introduced in **009**, not required in **001** persistence beyond placeholder types if needed for forward references |
| Destination on shipment | `DestinationType` + `DestinationId` (facility or customer) |

Primary entities: Farm, Facility, Product, Inventory Lot, Customer, Contract,
Order, Truck, Shipment, Market Price, Forecast, Recommendation Run,
Recommended Movement. Full field lists live in each feature `data-model.md`.

---

## 8. Synthetic Data Strategy

### Standard Demo Scale

- 150 farms, 8 facilities, 75 customers, 30 trucks, 3 regions
- 6 active products for the standard demo scenario
- 3 years daily history (~160k farm-production, ~80k orders, ~25k shipments)
- Daily inventory, market prices, weather; weekly fuel prices

### Thin-Slice Scale (Feature 000 / first E2E)

- 5 farms, 2 facilities, 5 customers, 3 trucks
- Raw milk + cream only
- 90 days history
- Static prices initially; distance-based transport cost
- One-day allocation optimization

Generation MUST encode learnable seasonality, weather/heat stress, butterfat →
cream yield coupling, demand seasonality, price autocorrelation/shocks, and
spoilage risk. Every run records:

```text
GeneratorVersion, ScenarioName, RandomSeed, GeneratedAt,
DateRange, EntityCounts, ConfigurationHash, SchemaVersion
```

---

## 9. Feature Roadmap (Improved)

```text
000-thin-vertical-slice          # NEW: prove E2E architecture first
001-foundation-and-domain
002-synthetic-data-generator
003-public-data-ingestion
004-operational-dashboard
005-supply-forecasting
006-demand-forecasting
007-price-forecasting
008-transportation-costing
009-movement-optimization
010-scenario-simulation
011-model-governance
012-historical-replay
013-demo-hardening
```

### Why Feature 000 Exists

The original plan correctly recommended a tiny E2E slice at the end (section
38) but sequenced full foundation + full generator first. That risks building
broad platform surface before proving the decision loop.

**000** delivers the interview question early:

> Given today’s available milk and cream, demand, prices, truck capacity, and
> transport costs, where should product move to maximize expected contribution
> margin?

Then **001–013** scale fidelity, forecasting, governance, and polish.

### Milestone Mapping

| Milestone | Features | Demonstrates |
|-----------|----------|--------------|
| M0 Architecture Proof | 000 | End-to-end allocation answer on tiny data |
| M1 Domain Demo | 001, 002, 004 | Network, inventory, seasonal ops |
| M2 Forecasting | 003, 005, 006, 007, 011 | ML.NET, leakage-safe eval, versions |
| M3 Optimization | 008, 009, 010 | OR-Tools, explainable what-if |
| M4 Portfolio | 012, 013 | Regret vs baselines, one-command demo |

Note: **008/009** may absorb lessons from **000**’s naive optimizer and replace
it; do not maintain two permanent optimizers without an ADR.

---

## 10. Feature Intent Summaries

Use these as `/speckit.specify` seeds (DairyDNA naming). Full prompts remain in
companion docs; only deltas from the original plan are called out here.

### 000 — Thin Vertical Slice

Minimal Aspire-hosted API + Blazor/Semantic UI/Fluxor + domain + tiny generator + explicit demand +
static prices + distance costing + single-period feasible allocation +
recommendation table. No public ingestion, no ML, no scenarios.

### 001 — Foundation and Domain

Reference data management and invariants. **Do not** require full forecast /
recommendation workflows yet. Health + local Aspire startup + domain tests.

### 002 — Synthetic Data Generator

Configurable, seeded, relationship-rich generator with validation report.
Supports both thin-slice and standard-three-year configs.

### 003–013

Keep original intent: ingestion, dashboard, supply/demand/price forecasts,
transport costing, OR-Tools optimization, scenarios, model governance, replay,
demo hardening — renamed to DairyDNA and bound to resolved clarifications.

---

## 11. Optimization Delivery Strategy

| Release | Capability |
|---------|------------|
| R1 (000/009) | Single period; milk+cream; direct O→D; one truck/move; no multi-stop; no processing transform |
| R2 | Multi-period; carryover; shelf-life decay; storage cost; truck availability over time |
| R3 | Processing transforms (separation yields, co-products) |
| R4 | Risk-adjusted / uncertainty-aware planning if justified |

---

## 12. Architecture

```text
Blazor Web UI (Semantic UI + Fluxor)
   │
ASP.NET Core Minimal APIs (workflows + authz; no controllers)
   ├── Domain & Rules
   ├── Forecasting Module (interface → ML.NET)
   └── Optimization Module (interface → OR-Tools)
           │
Infrastructure (EF Core, files, model registry)
           │
Workers (generate, import, train, replay, solve)
```

### Boundaries

- **Domain**: entities, invariants, no framework deps
- **Application**: use cases, ports, validation, transactions
- **Infrastructure**: EF Core, files, adapters
- **Forecasting / Optimization**: algorithms behind interfaces

### Data Pipeline

Public sources + synthetic generator → versioned raw → validation/quarantine →
canonical observations → feature snapshots → supply/demand/price models →
versioned forecasts → transport/constraints → optimizer → recommendations.

---

## 13. API Surface (Initial)

```text
/api/farms
/api/facilities
/api/products
/api/customers
/api/contracts
/api/orders
/api/trucks
/api/inventory
/api/market-prices
/api/datasets
/api/generation-runs
/api/import-runs
/api/experiments
/api/models
/api/forecasts
/api/scenarios
/api/optimization-runs
/api/recommendations
/api/replays
```

Long jobs return queued run IDs; clients poll status. Partial results MUST NOT
be labeled complete recommendations.

---

## 14. Nonfunctional Requirements

Unchanged in intent from the original plan:

- Dashboard useful content ≤ 2s (demo workload)
- Forecast query ≤ 500 ms P95 (precomputed)
- Optimization ≤ 30s (documented demo)
- Restartable generation/import; idempotent imports
- Checksummed model artifacts
- Accessible UI (keyboard, labels, non-color-only status)
- Full observability across UI/API/worker

---

## 15. Testing & Definition of Done

Keep original unit, property, integration, model, optimization, and E2E
strategies. Definition of Done remains constitution-aligned:

- Spec scenarios satisfied; clarifications incorporated
- Constitution gates pass; plan matches behavior; tasks traceable
- Tests green; contracts documented; observability present
- Demo scenario reproducible; `/speckit.analyze` clean
- ML: time-ordered eval, baselines, model card, no leakage
- Optimization: feasibility validation, known-answer tests, infeasible handling

---

## 16. Spec Kit Execution Loop (Per Feature)

```text
1.  /speckit.specify
2.  Review spec.md
3.  /speckit.clarify   (use specs/000-program/clarifications.md defaults)
4.  Approve clarifications
5.  /speckit.checklist
6.  Resolve checklist gaps
7.  /speckit.plan
8.  Review plan.md, research.md, data-model.md, contracts/
9.  /speckit.tasks
10. Review sequencing
11. /speckit.analyze
12. Resolve inconsistencies
13. /speckit.implement
14. Tests + acceptance
15. Docs / model or optimizer reports
16. /speckit.converge (if available) until converged
17. Merge only when DoD is met
```

Do **not** jump to implement after the first specify pass.

### Planning Prompt Baseline

Use the original technical planning checklist (technical context, constitution
check, research, data model, contracts, workflows, failure handling,
observability, security, testing, migration, performance, local/dev, risks),
with DairyDNA naming and the pinned stack above (ASP.NET Core Minimal APIs with
no controllers; Blazor + Semantic UI + Fluxor for the demonstration dashboard).

### Task Generation Rules

Independently testable increments; requirement traceability; parallel markers;
required tests; working app after each major phase. Forecasting and
optimization task families remain as in the original plan.

---

## 17. Default Demonstration Story

Keep the cream-excess / distant high-price customer / nearby contract penalty /
heat event / ice-cream demand spike / diesel rise / plant capacity loss /
expiring cream narrative. The punchline remains: highest price ≠ best move.

---

## 18. Principal Risks (Retained + One Add)

| Risk | Mitigation |
|------|------------|
| Synthetic data too clean | Noise, shocks, missingness, hidden validations, baselines |
| Forecast leakage | As-of features, time-ordered tests, feature audits |
| Optimizing forecast error | Uncertainty UI, conservative scenarios, regret metrics |
| Unrealistic logistics | Labeled assumptions, distance matrices, windows/cleaning/empty miles |
| Overengineering | Modular monolith first |
| Unclear business metric | Margin, service, spoilage, miles as primary measures |
| **Two optimizers drift (000 vs 009)** | ADR: 000 naive solver is temporary; 009 becomes system of record |

---

## 19. Success Measures

- Repeatable synthetic dairy network
- Traceable public-data ingestion
- Forecasts that beat documented baselines without leakage
- Feasible, explainable recommendations that beat simple policies on margin
  and/or spoilage/unmet demand in at least one documented scenario
- Versioned data, models, scenarios, optimizer runs
- One-command local developer experience
- Interview-ready business and technical demonstration

---

## 20. Immediate Next Actions

1. Review and accept `.specify/memory/constitution.md`.
2. Accept `specs/000-program/clarifications.md` defaults (or amend).
3. Run Spec Kit on **000-thin-vertical-slice** or **001-foundation-and-domain**
   (recommended: specify **000** first if the goal is fastest E2E proof).
4. Only then generate `plan.md` / `tasks.md` for that feature.
