# Research: Thin Vertical Slice (000)

**Date**: 2026-07-26  
**Feature**: `000-thin-vertical-slice`

## Decision 1 — Solution shape

**Decision**: Aspire-hosted modular monolith with `DairyDNA.Api` (ASP.NET Core
**Minimal APIs only — no controllers**), `DairyDNA.Web`, `DairyDNA.Domain`,
`DairyDNA.Application`, `DairyDNA.Infrastructure`, `DairyDNA.DataGenerator`,
`DairyDNA.Optimization`.

**Rationale**: Matches program plan; keeps boundaries testable; avoids
microservices for a demo slice. Minimal APIs keep the HTTP surface thin and
aligned with endpoint-group organization.

**Alternatives considered**: Single ASP.NET project (faster start, weaker
boundaries); separate microservices (overkill for 000); MVC controllers
(rejected — project standard is Minimal APIs only).

## Decision 2 — Database

**Decision**: SQL Server via Aspire + EF Core SQL Server provider.

**Rationale**: Pinned in program plan; common for .NET enterprise/portfolio demos;
Aspire can provision SQL Server containers locally.

**Alternatives considered**: PostgreSQL (portable open-source option; superseded by
project preference for SQL Server); SQLite (simpler but weaker Aspire story and
later scale path).

## Decision 3 — UI stack

**Decision**: Blazor Interactive Server + Semantic UI CSS (and minimal JS only
if required) + **Fluxor** for Blazor state management (stores, actions, effects
for demo summary, generation runs, and optimization results). Prefer
class-based Semantic UI patterns in Razor components.

**Rationale**: Spec requires Blazor + Semantic UI; Fluxor keeps interactive demo
state predictable as generate/optimize flows grow. Server interactivity is
enough for local demo without WASM complexity.

**Alternatives considered**: Blazor WASM (heavier publish); MudBlazor/Fluent
(conflicts with Semantic UI choice); Fomantic-UI fork (acceptable drop-in if
upstream Semantic UI assets are problematic — document in ADR if switched);
component-local `StateHasChanged` only (rejected once multi-step demo flows
need shared UI state).

## Decision 4 — Optimizer for 000

**Decision**: `IAllocationOptimizer` with `NaiveContributionMarginOptimizer`:
enumerate feasible single-leg candidate moves, score by expected contribution
margin, assign greedily in deterministic sort order (margin desc, then origin
id, destination id, product id, truck id). Reject negative-margin candidates.
Validate feasibility independently after solve.

**Rationale**: Spec allows temporary naive optimizer; tiny instance sizes make
exact enumeration practical; deterministic ordering supports SC-002.

**Alternatives considered**: Google OR-Tools now (deferred to 009); random
search (non-reproducible); pure SQL heuristics (harder to unit test).

## Decision 5 — Job execution model

**Decision**: Synchronous optimize for thin slice (expect sub-second).
Generation may be synchronous for thin scale or a simple in-process background
task with polled status; no message bus.

**Rationale**: Constitution XV — no speculative messaging. Thin data volume.

**Alternatives considered**: Hangfire/queues (premature); always-sync generate
for 90 days is acceptable on a workstation.

## Decision 6 — Transport costing

**Decision**: Explicit formula using Haversine (or precomputed distance matrix
from lat/long) × fuel/operating rates + fixed load/unload components. Persist
breakdown fields: distance miles, fuel cost, operating cost, total estimated
cost. Quantize money to 2 decimal places.

**Rationale**: Spec requires explainable distance-based cost; quantization
supports 0.01 tolerance.

**Alternatives considered**: External routing APIs (out of scope / nondeterministic);
ML residual cost (deferred).

## Decision 7 — Demo “today”

**Decision**: Generation config includes `planningDate` (default = last date in
generated range). Optimization `asOfDate` selects inventory/orders/prices for
that calendar day.

**Rationale**: Makes acceptance scenarios deterministic without wall-clock
coupling.

**Alternatives considered**: Always DateTime.UtcNow (breaks reproducibility).

## Decision 8 — Auth

**Decision**: No authentication for 000; document localhost/dev-only.

**Rationale**: Clarify session answer A; FR-006/FR-008.

**Alternatives considered**: Basic auth / demo user (deferred to hardening).

## Decision 9 — Contracts / safety stock

**Decision**: Omit contract entities and safety-stock constraints from 000
optimizer inputs.

**Rationale**: Clarify session answers.

## Decision 10 — Units & products

**Decision**: Pounds; products `RAW_MILK` and `CREAM` only.

**Rationale**: Program clarifications + thin-slice FR-001.

## Open items deferred to `/speckit.tasks` / implement

- Exact Semantic UI asset packaging (CDN vs vendored wwwroot)
- Fluxor feature folder layout under `DairyDNA.Web` (Store/Actions/Effects/Reducers)
- Minimal API endpoint group organization under `DairyDNA.Api` (e.g. Generation,
  Demo, Optimization map groups)
- Whether Worker project is created in 000 or deferred until jobs need it
- Currency code display (`USD` label only; no FX)
