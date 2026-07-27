# Feature Specification: Supply Forecasting

**Feature Branch**: `005-supply-forecasting`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program clarifications (Forecasting) + program plan Feature 005  
**Depends on**: 002 (history); 003 optional (weather features); 011 for full governance UX (minimal versioning required here)

## User Scenarios & Testing

### User Story 1 — Produce facility/region supply forecasts (Priority: P1)

A data scientist (or demo operator) trains/runs the supply forecast pipeline for
horizons 1/7/14/28 days and publishes versioned forecasts with point estimate
and lower/upper bounds for Raw milk (and cream yield-linked supply where
modeled).

**Why this priority**: Optimization and planners need expected available supply.

**Independent Test**: Train on synthetic history with time-ordered split; score
7-day WAPE vs seasonal-naive baseline on held-out period.

**Acceptance Scenarios**:

1. **Given** a versioned dataset, **When** supply forecasting runs, **Then**
   forecasts exist for active facilities (coverage ≥ 99% target) at 7-day
   horizon.
2. **Given** published forecasts, **When** queried as-of a timestamp, **Then**
   only features available at that as-of are implied by the feature snapshot
   (no leakage).
3. **Given** evaluation results, **When** compared to seasonal-naive, **Then**
   7-day WAPE improves by ≥ 10% on the default synthetic scenario (or the run
   is marked below-bar with explicit metrics — never silent).

---

### User Story 2 — Inspect forecast bands on the dashboard (Priority: P1)

A planner views supply forecast bands for a facility/region and can distinguish
actuals vs forecasts.

**Why this priority**: Constitution VII honesty in the UI.

**Independent Test**: Open forecast panel; verify labels and interval display.

**Acceptance Scenarios**:

1. **Given** published forecasts, **When** viewing a facility, **Then** point
   and bounds are shown and labeled as forecasts.
2. **Given** actual production for past dates, **When** overlapping the chart,
   **Then** actuals and forecasts are visually distinct.

---

### User Story 3 — Record experiment metadata (Priority: P2)

Each training/inference run records dataset version, feature schema, algorithm,
hyperparameters, seed, metrics, and model version.

**Why this priority**: Constitution III / XIII reproducibility.

**Independent Test**: After a run, experiment/model records are queryable.

**Acceptance Scenarios**:

1. **Given** a completed training job, **When** inspecting the model card/
   experiment, **Then** required metadata fields are present.
2. **Given** the same seed and dataset version, **When** retraining with the
   same config, **Then** metrics match within documented tolerance.

---

### Edge Cases

- Cold facilities with sparse history: mark low-confidence / cold-start; still
  emit a forecast via hierarchy (farm→facility→region) per clarifications.
- Missing weather features: model uses documented fallback; flagged in run log.
- Attempted use of future actuals in features fails automated leakage tests.

## Requirements

### Functional Requirements

- **FR-001**: System MUST forecast supply at facility and region aggregation
  levels for horizons 1, 7, 14, 28 days.
- **FR-002**: Forecasts MUST include point estimate and lower/upper bounds.
- **FR-003**: Train/validation/test splits MUST be time-ordered with as-of
  feature timestamps (no leakage).
- **FR-004**: Primary acceptance metric MUST be WAPE; also report MAE, RMSE,
  bias, interval coverage.
- **FR-005**: Default synthetic scenario MUST target ≥10% WAPE improvement vs
  seasonal-naive on 7-day horizon and aggregate bias within ±5%.
- **FR-006**: System MUST version models and bind forecasts to model + dataset
  versions.
- **FR-007**: UI MUST not present forecasts as guaranteed volumes.
- **FR-008**: Forecasting module MUST sit behind interfaces (ML.NET default).

### Key Entities

- **FeatureSnapshot**: As-of-timestamped feature rows.
- **Experiment / ModelVersion**: Training metadata and artifacts.
- **SupplyForecast**: Horizon forecasts with intervals and provenance.

## Success Criteria

- **SC-001**: Leakage tests pass in CI.
- **SC-002**: Baseline comparison is produced on every evaluation run.
- **SC-003**: 7-day facility coverage ≥ 99% on default synthetic scenario when
  data is complete.
- **SC-004**: Forecast read API P95 ≤ 500 ms for precomputed results (demo
  workload).

## Assumptions

- Clarifications forecasting table is authoritative.
- Full model-governance UI may arrive in 011; minimum metadata is required now.

## Out of Scope

- Demand/price models (006/007), OR-Tools consumption (009), auto-retraining in
  production.
