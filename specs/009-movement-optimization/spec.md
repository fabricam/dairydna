# Feature Specification: Movement Optimization

**Feature Branch**: `009-movement-optimization`  
**Created**: 2026-07-26  
**Status**: Implemented  
**Product**: DairyDNA  
**Input**: Program clarifications (Optimization) + ADR naive→OR-Tools + Feature 009  
**Depends on**: 001–002, 008; forecasts 005–007 preferred for price/supply/demand modes; replaces 000 naive as system of record

## Clarifications (inherited defaults)

- Maximize expected contribution margin
- Soft contract shortfalls with penalty; hard max acceptance
- Partial fills and split loads allowed
- Safety stock optional (default on for demo profile)
- Single-leg routes; no processing transforms in R1
- Single-period planning in R1
- Hold unprofitable inventory (no forced negative-margin moves) unless a later
  amendment explicitly changes this

## User Scenarios & Testing

### User Story 1 — Produce a feasible allocation plan (Priority: P1)

An operations planner runs optimization for an as-of date and receives an
`OptimizationRun` with feasible recommended movements (or explicit infeasible/
failed status), each with revenue, costs, margin, binding constraints, and
assumptions.

**Why this priority**: Core product question — where should product move?

**Independent Test**: Known-answer fixtures (capacity, expiry, negative margin,
windows) plus independent feasibility validator.

**Acceptance Scenarios**:

1. **Given** inventory, demand/orders, trucks, and prices, **When** optimize
   runs, **Then** status is Feasible or explicit Infeasible/Failed — never a
   silent invalid plan labeled recommended.
2. **Given** a feasible run with movements, **When** inspecting recommendations,
   **Then** explanation includes margin factors and binding constraints, a
   **margin/cost breakdown chart** is shown, and recommended O→D moves appear
   as **arcs or highlighted pairs on the network map**.
3. **Given** insufficient capacity, **When** optimize runs, **Then** plan stays
   feasible and reports unserved demand and/or unused inventory.
4. **Given** only negative-margin options, **When** optimize runs, **Then**
   inventory is held (no loss-making forced moves) unless amendment says
   otherwise.

---

### User Story 2 — Beat simple baselines on the flagship scenario (Priority: P1)

On the documented cream-excess / distant high-price / nearby contract / heat /
diesel / capacity-loss narrative, the optimizer’s expected margin and/or
spoilage/unmet-demand metrics beat a simple greedy or highest-price-first
baseline.

**Why this priority**: Interview punchline — highest price ≠ best move.

**Independent Test**: Scenario fixture + baseline policy comparison report.

**Acceptance Scenarios**:

1. **Given** the flagship synthetic scenario, **When** OR-Tools optimize and
   baseline policy run, **Then** a comparison report shows optimizer advantage
   on at least one primary metric (margin, spoilage, or unmet contract demand).
2. **Given** that report, **When** reviewed, **Then** assumptions and costing
   version are cited.

---

### User Story 3 — Become system of record vs naive 000 solver (Priority: P1)

OR-Tools-backed optimizer is the default for new runs; `naive-cm-v1` remains
available only behind an explicit version flag for regression comparison, with
ADR documenting temporary status.

**Why this priority**: Avoid two permanent optimizers drifting.

**Independent Test**: Default optimize path records OR-Tools (or successor)
version string; ADR referenced from plan.

**Acceptance Scenarios**:

1. **Given** default optimize request, **When** completed, **Then**
   `optimizerVersion` identifies the OR-Tools implementation.
2. **Given** an explicit request for `naive-cm-v1`, **When** allowed in demo
   config, **Then** that solver may still run for comparison.

---

### Edge Cases

- Delivery window impossibility → movement omitted; demand may remain unserved.
- Expired lots excluded from supply.
- Safety stock on: retain configured minimums; document binding when they limit
  shipments.
- Solver timeout → Failed with message; no partial recommendations labeled
  complete.

## Requirements

### Functional Requirements

- **FR-001**: System MUST maximize expected contribution margin subject to
  inventory, truck capacity/compatibility, delivery windows, demand bounds,
  shelf-life/expiry, and optional safety stock.
- **FR-002**: System MUST apply soft contract shortfall penalties and hard
  maximum acceptance when contracts are in scope for the run.
- **FR-003**: System MUST allow partial fills down to minimum acceptable
  quantity and split loads across trucks.
- **FR-004**: System MUST reject or omit negative expected-margin moves (hold
  inventory) per thin-slice clarification unless amended.
- **FR-005**: System MUST run independent feasibility validation after solve.
- **FR-006**: System MUST provide known-answer tests and flagship baseline
  comparison.
- **FR-007**: Every movement MUST be explainable (factors, costs, constraints,
  assumptions).
- **FR-007a**: Recommendations UI MUST include (1) a **network map** with
  recommended flow arcs or highlighted O→D pairs and (2) a **chart** of margin
  and cost components across movements. Tables alone are not sufficient for the
  primary recommendations view (`specs/_visual-aids.md`).
- **FR-008**: Price inputs MUST support forecast point/lower/upper and user
  scenario override.
- **FR-009**: Canonical API entity remains `OptimizationRun`; UI may say
  Recommendations.
- **FR-010**: Default solver MUST be OR-Tools behind `IAllocationOptimizer`;
  naive solver is non-default.

### Key Entities

- **OptimizationRun**, **RecommendedMovement**
- **OptimizationRequest**: as-of, price mode, safety stock flag, solver version
- **FeasibilityReport**: validator output

## Success Criteria

- **SC-001**: All known-answer fixtures pass.
- **SC-002**: Independent validator never accepts an infeasible plan as
  Feasible.
- **SC-003**: Documented demo optimize completes ≤ 30s.
- **SC-004**: Flagship baseline comparison produced in CI or documented manual
  gate with checked-in artifacts.
- **SC-005**: Reproducibility: exact objective + quantities; costs ≤ 0.01
  tolerance per compared field.

## Assumptions

- R1: milk+cream, single period, single-leg, no processing transforms.
- 000 remains a historical/demo path until cutover; ADR-0001 governs.

## Out of Scope

- Multi-period R2, processing R3, uncertainty-aware R4, auto-dispatch.
