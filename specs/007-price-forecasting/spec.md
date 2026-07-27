# Feature Specification: Price Forecasting

**Feature Branch**: `007-price-forecasting`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program clarifications (Forecasting + Optimization price inputs) + Feature 007  
**Depends on**: 002/003 price histories; 005/006 pipeline patterns

## User Scenarios & Testing

### User Story 1 — Forecast product prices with intervals (Priority: P1)

A data scientist produces price forecasts for Raw milk and Cream (and other
active demo products when present) at the geographic grain used by the demo
(region and/or market), for horizons 1/7/14/28 days, with point and bounds.

**Why this priority**: Optimization selects among forecast point / conservative /
optimistic / user scenario prices.

**Independent Test**: Time-ordered evaluation vs a documented naive baseline
(e.g., last price / seasonal-naive); emit WAPE and bias.

**Acceptance Scenarios**:

1. **Given** historical market prices (synthetic and/or public), **When** price
   forecasting runs, **Then** versioned forecasts with intervals are published.
2. **Given** published forecasts, **When** an optimization-ready price bundle is
   requested, **Then** point, lower, and upper series are available for the
   horizon.
3. **Given** evaluation, **When** complete, **Then** baseline comparison is
   stored on the experiment.

---

### User Story 2 — Honest presentation (Priority: P1)

Planners see price bands labeled as forecasts — never as executable trade
quotes or guaranteed clearing prices.

**Why this priority**: Product honesty boundary and constitution VII.

**Independent Test**: UI copy and API field names include forecast semantics.

**Acceptance Scenarios**:

1. **Given** forecast prices on a chart, **When** rendered, **Then** labels
   distinguish actual observations vs forecasts.
2. **Given** API consumers, **When** reading forecast DTOs, **Then** schema
   documents non-advisory demo nature.

---

### User Story 3 — Shock robustness smoke (Priority: P2)

Synthetic price shocks in generated data do not crash the pipeline; coverage
and metrics remain reportable.

**Why this priority**: Demo narrative includes diesel/price shocks.

**Independent Test**: Run evaluation window containing a configured shock;
pipeline completes with metrics.

**Acceptance Scenarios**:

1. **Given** a dataset with a price shock, **When** forecasting, **Then** the
   job completes and flags elevated error in the shock window without failing
   the entire publish (unless quality gate configured otherwise).

---

### Edge Cases

- Missing public series: fall back to synthetic-only with explicit source mix
  on the model card.
- Negative price predictions rejected/clamped with logging.
- As-of feature build excludes future prints.

## Requirements

### Functional Requirements

- **FR-001**: System MUST forecast prices for R1 movable products (Raw milk,
  Cream) at documented geography for horizons 1/7/14/28.
- **FR-002**: Forecasts MUST include point + lower/upper bounds suitable for
  optimization price-mode selection.
- **FR-003**: No leakage: time-ordered splits and as-of features.
- **FR-004**: Report WAPE (primary), MAE, RMSE, bias, interval coverage.
- **FR-005**: Version models, datasets, and published forecast sets.
- **FR-006**: UI/API MUST NOT present forecasts as trade execution prices.
- **FR-007**: Support mixing synthetic and public observations with provenance.

### Key Entities

- **PriceFeatureSnapshot**, **PriceModelVersion**, **PriceForecast**
- **OptimizationPriceBundle**: Point/lower/upper (and optional user override hook)

## Success Criteria

- **SC-001**: Leakage tests pass in CI.
- **SC-002**: OptimizationPriceBundle can be retrieved for a demo as-of date in
  <500 ms P95 when precomputed.
- **SC-003**: Baseline comparison artifact emitted every evaluation.
- **SC-004**: Honesty labeling verified in UI smoke test.

## Assumptions

- Clarifications: optimization may select point / conservative / optimistic /
  user scenario — this feature supplies the first three; user scenario is 010.
- 011 owns richer model registry UX.

## Out of Scope

- Live trading, brokerage connectivity, advisory disclaimers beyond demo labels.
