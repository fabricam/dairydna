# Feature Specification: Scenario Simulation

**Feature Branch**: `010-scenario-simulation`  
**Created**: 2026-07-26  
**Status**: Implemented  
**Product**: DairyDNA  
**Input**: Program plan §17 flagship narrative + Feature 010  
**Depends on**: 009 (optimize); 007/008 for price/fuel overrides; 004 for compare UI

## User Scenarios & Testing

### User Story 1 — Define and run a what-if scenario (Priority: P1)

A commodity/sales manager or planner creates a scenario that overrides one or
more inputs (price mode/user prices, fuel price, plant capacity, demand spike,
heat marker) and runs optimization, then compares objective and service metrics
to the base run.

**Why this priority**: Demo value is “change conditions → see different feasible
plan,” not a single static recommendation.

**Independent Test**: Base run + scenario with higher distant price; comparison
API returns both runs’ objectives and movement diffs.

**Acceptance Scenarios**:

1. **Given** a base OptimizationRun, **When** a scenario overrides fuel price
   upward and re-optimizes, **Then** a new run is linked to the scenario with
   distinct economics.
2. **Given** base and scenario runs, **When** opening compare view, **Then**
   objective, unserved demand, unused inventory, and key movements are shown
   side-by-side with **compare charts** (e.g., grouped bars for objective and
   service metrics) and optional **map overlay** of differing flows.
3. **Given** scenario inputs, **When** saved, **Then** they are versioned and
   re-runnable with the same results under reproducibility rules.

---

### User Story 2 — Flagship narrative pack (Priority: P1)

A demo operator loads the packaged flagship scenario set (cream excess, distant
high price, nearby contract pressure, heat, demand spike, diesel rise, plant
capacity loss, expiring cream) without hand-editing raw tables.

**Why this priority**: Interview reproducibility and story clarity.

**Independent Test**: One-click (or single API) load of flagship pack on a
standard/thin dataset; optimize base vs stressed scenario.

**Acceptance Scenarios**:

1. **Given** a compatible dataset, **When** applying the flagship pack,
   **Then** scenario definitions are created with documented overrides.
2. **Given** those scenarios, **When** compared, **Then** the UI narrative can
   show that highest price is not always the best move.

---

### User Story 3 — Prevent mislabeling partial sims (Priority: P2)

Failed or timed-out scenario solves are not shown as complete recommendations.

**Why this priority**: Constitution V/VII.

**Independent Test**: Force timeout/failure; UI shows Failed.

**Acceptance Scenarios**:

1. **Given** a failed scenario optimize, **When** viewing compare, **Then**
   status Failed is visible and movements are not presented as recommended.

---

### Edge Cases

- Scenario overrides that violate invariants are rejected at save/run time.
- Comparing runs from different dataset versions is blocked or hard-warned.
- User scenario prices outside sanity bounds require confirmation.

## Requirements

### Functional Requirements

- **FR-001**: System MUST support named scenarios with override sets (prices,
  fuel, capacity, demand adjustments, flags).
- **FR-002**: System MUST run optimization under a scenario and link the
  OptimizationRun to the scenario.
- **FR-003**: System MUST provide base-vs-scenario comparison read models.
- **FR-004**: System MUST ship a flagship scenario pack for demos.
- **FR-005**: Scenario definitions MUST be versioned and reproducible.
- **FR-006**: UI MUST use Semantic UI + Fluxor compare views with honesty labels.
- **FR-006a**: Scenario compare MUST include **charts** for objective and key
  service metrics (base vs scenario); SHOULD overlay differing recommended
  flows on a **network map** (`specs/_visual-aids.md`).
- **FR-007**: Failed runs MUST NOT be labeled as successful recommendations.

### Key Entities

- **ScenarioDefinition**, **ScenarioOverride**, **ScenarioRun**
- **ScenarioComparison**: Pairwise metrics and movement diff summary

## Success Criteria

- **SC-001**: Flagship pack applies and compares successfully on documented
  dataset profile.
- **SC-002**: Re-running the same scenario yields exact objective/quantities
  (cost ≤0.01 tolerance).
- **SC-003**: Compare view usable within dashboard performance expectations.

## Assumptions

- Optimization engine is 009 OR-Tools (or explicitly versioned solver).
- Not all narrative stressors need separate ML forecasts — overrides suffice.

## Out of Scope

- Full Monte Carlo risk engine, multi-user scenario collaboration, auto-dispatch.
