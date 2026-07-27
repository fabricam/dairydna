# Feature Specification: Transportation Costing

**Feature Branch**: `008-transportation-costing`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program clarifications (Optimization transport) + Feature 008  
**Depends on**: 001 domain; may replace/extend 000 distance costing; fuel from 003

## User Scenarios & Testing

### User Story 1 — Estimate cost for an origin–destination move (Priority: P1)

A logistics coordinator (or optimizer) requests estimated transportation cost
for moving a quantity of product from a facility to a customer/facility using
distance, fuel, operating rates, empty-return approximation, and load/unload
time — money quantized to 2 decimals.

**Why this priority**: Contribution margin is revenue minus transport (and later
other costs); costing must be explicit and testable.

**Independent Test**: Known lat/lon pairs and rates produce golden cost
breakdowns within 0.01.

**Acceptance Scenarios**:

1. **Given** origin, destination, truck rates, and quantity, **When** costing
   runs, **Then** distance, fuel, operating, and total estimated cost are
   returned.
2. **Given** empty-return included policy, **When** costing, **Then** billed
   miles reflect the documented empty-return rule.
3. **Given** the same inputs twice, **When** costing, **Then** totals match
   exactly (2-decimal quantization).

---

### User Story 2 — Fuel-price sensitivity (Priority: P2)

When fuel prices change (imported or scenario override), estimated fuel
components update for new cost calculations without rewriting history.

**Why this priority**: Flagship demo includes diesel rise.

**Independent Test**: Recompute with higher fuel price; fuel component increases
monotonically.

**Acceptance Scenarios**:

1. **Given** a baseline fuel price and a higher scenario fuel price, **When**
   costing the same lane, **Then** fuel cost is higher in the scenario.
2. **Given** historical cost snapshots on past optimization runs, **When** fuel
   changes later, **Then** past run economics remain unchanged.

---

### User Story 3 — Incompatible or impossible lanes (Priority: P2)

Requests with incompatible truck/product or missing coordinates fail validation
rather than returning zero-cost fantasies.

**Why this priority**: Feasibility-before-profitability starts at costing inputs.

**Independent Test**: Invalid requests return problem details.

**Acceptance Scenarios**:

1. **Given** missing destination coordinates, **When** costing, **Then** the
   API rejects with a clear error.
2. **Given** incompatible product–truck pairing, **When** costing is invoked in
   a context that includes compatibility checks, **Then** the call is rejected
   or flagged ineligible.

---

### Edge Cases

- Zero distance (same point): still applies load/unload time costs per policy.
- Extreme distances: may warn or cap per documented max lane length.
- Negative rates rejected.

## Requirements

### Functional Requirements

- **FR-001**: System MUST compute distance-based transport cost breakdowns
  (distance, fuel, operating, total) for single-leg O→D moves.
- **FR-002**: Empty-return cost MUST be included per program clarifications.
- **FR-003**: Money fields MUST quantize to 2 decimal places.
- **FR-004**: Costing MUST be deterministic for identical inputs.
- **FR-005**: Costing MUST be exposed behind an application port usable by
  000/009 optimizers.
- **FR-006**: Fuel price input MUST be parameterizable (observed or scenario).
- **FR-007**: Assumptions (avg speed, empty-return rule, load/unload hours)
  MUST be documented and echoed in explanations when used by optimization.

### Key Entities

- **TransportLane**: Origin/destination references + distance cache optional.
- **TransportCostEstimate**: Breakdown DTO with versioned costing model id.
- **FuelPriceObservation**: From 003 or scenario override.

## Success Criteria

- **SC-001**: Golden-path unit tests cover ≥5 known lanes within 0.01.
- **SC-002**: Determinism tests pass 100% over ≥10 repeated calls.
- **SC-003**: Invalid coordinate cases rejected in contract tests.

## Assumptions

- R1 is single-leg only; multi-stop routing is out of scope.
- 000 naive costing may be upgraded in place under this feature’s version id.

## Out of Scope

- Turn-by-turn navigation, telematics, driver payroll, multi-stop VRP.
