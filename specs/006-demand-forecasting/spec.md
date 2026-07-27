# Feature Specification: Demand Forecasting

**Feature Branch**: `006-demand-forecasting`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program clarifications (Forecasting) + program plan Feature 006  
**Depends on**: 002; 005 patterns for pipelines/versioning; 003 optional

## User Scenarios & Testing

### User Story 1 — Forecast customer/segment demand (Priority: P1)

A data scientist runs demand forecasting for horizons 1/7/14/28 days for Raw
milk and Cream orders at customer and segment/region levels, with intervals and
cold-start handling for sparse customers.

**Why this priority**: Allocation needs expected demand, not only open orders.

**Independent Test**: Time-ordered evaluation vs seasonal-naive or
same-day-previous-week baseline (documented per model card).

**Acceptance Scenarios**:

1. **Given** synthetic order history, **When** demand forecasting completes,
   **Then** forecasts exist for active customers or their segments at 7-day
   horizon.
2. **Given** a new/sparse customer, **When** forecasting, **Then** the forecast
   is marked cold-start and uses segment/region allocation weights.
3. **Given** evaluation, **When** compared to the documented baseline, **Then**
   metrics are recorded; below-bar runs are explicitly flagged.

---

### User Story 2 — Planner views demand bands (Priority: P1)

A planner sees demand forecast bands alongside open orders and can tell
forecast vs committed demand apart.

**Why this priority**: Honest uncertainty for sales/ops decisions.

**Independent Test**: UI shows open orders and forecast series with distinct
labels.

**Acceptance Scenarios**:

1. **Given** published demand forecasts, **When** opening a customer view,
   **Then** point/bounds display as forecasts.
2. **Given** open orders for the as-of date, **When** viewing the same screen,
   **Then** orders are not labeled as forecasts.

---

### User Story 3 — Contract-aware features without leakage (Priority: P2)

Known contract minimums and calendar effects entered before as-of time MAY be
features; future unknown orders MUST NOT leak.

**Why this priority**: Clarifications allow future-known values only when known
before as-of.

**Independent Test**: Feature audit test rejects post-as-of order quantities.

**Acceptance Scenarios**:

1. **Given** a contract minimum known before as-of, **When** features build,
   **Then** it may appear in the snapshot.
2. **Given** an order created after as-of, **When** features build for that
   as-of, **Then** it is excluded.

---

### Edge Cases

- Zero-history segment: emit hierarchical fallback; mark cold-start.
- Demand spike scenarios (ice-cream heat event) remain in synthetic data for
  later scenario demos — model need not perfectly predict shocks to ship.
- Negative forecasts clamped/rejected per model policy with logging.

## Requirements

### Functional Requirements

- **FR-001**: System MUST forecast demand for Raw milk and Cream at customer
  and segment/region levels for horizons 1/7/14/28.
- **FR-002**: Forecasts MUST include point + lower/upper bounds.
- **FR-003**: Sparse/new customers MUST use segment + region allocation and
  cold-start marking.
- **FR-004**: Time-ordered splits and as-of features are mandatory (no leakage).
- **FR-005**: Primary metric WAPE; also MAE, RMSE, bias, interval coverage.
- **FR-006**: Baseline MUST be documented (seasonal-naive or same-day-previous-
  week) and reported every evaluation.
- **FR-007**: Version model + dataset on every published forecast.
- **FR-008**: UI MUST distinguish forecasts from orders/contracts.

### Key Entities

- **DemandFeatureSnapshot**, **DemandModelVersion**, **DemandForecast**
- **CustomerSegmentProfile**: Weights for cold-start allocation

## Success Criteria

- **SC-001**: Leakage tests pass in CI.
- **SC-002**: Cold-start path covered by automated tests.
- **SC-003**: Precomputed forecast reads meet ≤500 ms P95 demo budget.
- **SC-004**: Every evaluation emits baseline comparison artifacts.

## Assumptions

- Soft contracts and partial fills remain optimization concerns (009); this
  feature predicts demand quantities, not penalties.
- 011 deepens governance UX.

## Out of Scope

- Price forecasting (007), optimization binding (009), auto-dispatch.
