# Feature Specification: Thin Vertical Slice

**Feature Branch**: `000-thin-vertical-slice`  
**Created**: 2026-07-26  
**Status**: Implemented (thin-slice MVP; see tasks.md)  
**Product**: DairyDNA  
**Input**: Program plan section “First Implementation Iteration” / Feature 000

## Clarifications

### Session 2026-07-26

- Q: When a move would have negative expected contribution margin after transport, what should the optimizer do? → A: Hold unprofitable inventory (no forced loss-making moves)
- Q: What numeric tolerance must matching optimization runs satisfy for reproducibility? → A: Exact objective + quantities; costs may differ by ≤ 0.01
- Q: Does the thin-slice objective include contract shortfall penalties? → A: Explicit orders / spot demand only (no contract penalties in 000)
- Q: Should the thin-slice optimizer enforce safety stock? → A: Safety stock off for thin slice
- Q: How should access control work for generate/optimize in the local thin-slice demo? → A: Open local access — no authentication for 000
- Q: (Analyze remediation) Canonical name for allocation result aggregate? → A: OptimizationRun (UI may say Recommendations)
- Q: (Analyze remediation) Plant capacity in 000 optimizer? → A: Out of scope for 000; enforce delivery windows, inventory, truck capacity/compatibility, non-negative margin

## User Scenarios & Testing

### User Story 1 — Answer today’s allocation question (Priority: P1)

An operations planner (or interview reviewer) loads the thin-slice demo, sees
today’s available milk and cream, customer demand, static prices, truck
capacity, and transport costs, runs allocation, and receives a feasible
recommendation table with expected contribution margin.

**Why this priority**: Proves the full decision loop before scaling data or ML.

**Independent Test**: Generate thin-slice data, run optimize, verify at least
one feasible recommended movement and an explainable margin breakdown.

**Acceptance Scenarios**:

1. **Given** the thin-slice dataset, **When** the planner opens the demo home,
   **Then** inventory, demand, prices, and fleet summary are visible.
2. **Given** that dataset, **When** optimization is run for the demo day,
   **Then** the system returns a feasible plan or an explicit infeasible status.
3. **Given** a feasible plan, **When** viewing a recommended movement, **Then**
   revenue, transport cost, and expected contribution margin are shown.
4. **Given** insufficient truck capacity to move all inventory, **When**
   optimizing, **Then** the plan remains feasible and reports unused inventory
   and/or unserved demand rather than inventing capacity.

---

### User Story 2 — Reproduce the slice (Priority: P1b — after US1)

A developer regenerates the thin-slice dataset with the documented seed and
reruns optimization, obtaining the same logical recommendations: objective
value and recommended quantities MUST match exactly; itemized cost components
MAY differ by at most 0.01 currency units per compared field.

**Why this priority**: Interview demos must be deterministic; sequenced after the
allocation MVP so reproducibility hardens an existing path.

**Independent Test**: Run generate+optimize twice with the same seed; compare
recommendation summaries.

**Acceptance Scenarios**:

1. **Given** seed `104729` and thin-slice config, **When** generation runs
   twice, **Then** entity counts and key aggregates match.
2. **Given** the same generated dataset, **When** optimization runs twice,
   **Then** objective value and recommended quantities match exactly, and any
   compared cost breakdown fields differ by at most 0.01.

---

### Edge Cases

- No demand: optimizer reports no movements and full unused inventory.
- Expired inventory: excluded from eligible supply.
- Single customer with price too low after transport (negative expected
  contribution margin): leave inventory unmoved; do not force a loss-making
  shipment. Report unused inventory explicitly.

## Requirements

### Functional Requirements

- **FR-001**: System MUST support thin-slice scale: 5 farms, 2 facilities,
  5 customers, 3 trucks, raw milk + cream, 90 days history.
- **FR-002**: System MUST use explicit (non-ML) order demand and static market
  prices for this feature. Contract shortfall penalties MUST NOT be included in
  the thin-slice objective.
- **FR-003**: System MUST compute distance-based transportation cost with an
  explainable breakdown (at least distance and fuel/operating components).
- **FR-004**: System MUST run single-period feasible allocation maximizing
  expected contribution margin under these hard constraints for 000: available
  inventory (excluding expired lots), truck capacity, product–truck
  compatibility, order delivery windows, and non-negative contribution margin
  per movement. Partial order fulfillment down to
  `MinimumAcceptableQuantityPounds` and splitting a load across multiple trucks
  are allowed. **Plant/facility processing capacity is out of scope for 000**
  (deferred to later optimization features). Safety-stock constraints MUST NOT
  apply in this feature.
- **FR-004a**: System MUST NOT create a recommended movement whose expected
  contribution margin is negative; such inventory remains unused for the run.
- **FR-005**: System MUST never label an infeasible solution as recommended.
- **FR-006**: System MUST expose the flow via local orchestrated Minimal API + UI
  (Blazor + Semantic UI + Fluxor). HTTP endpoints MUST use ASP.NET Core Minimal
  APIs with no MVC controllers. Generate and optimize actions MUST be available
  without authentication in this feature (open local demo access).
- **FR-007**: Temporary naive optimizer is allowed; Feature 009 becomes system
  of record later (ADR required if both remain).
- **FR-008**: The local demo SHOULD bind for local development use; production
  internet exposure of unauthenticated generate/optimize is out of scope and
  MUST NOT be claimed as secure.

### Key Entities

Subset of program domain: Farm, Facility, Product, Inventory Lot, Customer,
Order, Truck, Market Price, **OptimizationRun** (API/resource name; UI may label
results as “Recommendations”), Recommended Movement.
Contract entities are out of scope for the thin-slice objective and are not
required for 000 acceptance.

## Success Criteria

- **SC-001**: The system answers the allocation question on the thin-slice demo
  in one documented path.
- **SC-002**: Same seed reproduces generation (exact entity counts and key
  aggregates) and optimization (exact objective and quantities; costs ≤ 0.01
  absolute difference per compared cost field).
- **SC-003**: Automated tests cover at least three known-answer optimization
  fixtures (one origin/one customer; higher price but worse net after transport;
  infeasible / insufficient capacity) plus expired-inventory exclusion and
  zero-demand (no movements, full unused inventory) cases.

## Assumptions

- Clarifications defaults apply (pounds, daily cadence, partial fill, split loads),
  except contract shortfall penalties and safety stock are deferred/off for this
  feature.
- No authentication in 000; admin auth deferred to later hardening.
- No public data, no ML.NET, no scenarios, no replay in this feature.

## Out of Scope

- Full three-year generator, forecasting, model governance, multi-period
  optimization, processing transforms, **plant/facility processing-capacity
  constraints**, contract shortfall penalties, safety stock,
  authentication/authorization, Aspire complexity beyond one API/UI/DB/
  worker as needed.
