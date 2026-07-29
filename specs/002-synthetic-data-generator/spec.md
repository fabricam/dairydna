# Feature Specification: Synthetic Data Generator

**Feature Branch**: `002-synthetic-data-generator`  
**Created**: 2026-07-26  
**Status**: Implemented  
**Product**: DairyDNA  
**Input**: Program plan §8 / §10 Feature 002  
**Depends on**: 001 (domain entities & invariants); may extend generator from 000

## User Scenarios & Testing

### User Story 1 — Generate a standard demo network (Priority: P1)

A demo administrator selects the standard demo profile (≈150 farms, 8 facilities,
75 customers, 30 trucks, 6 products, ~3 years daily history) with a documented
seed and receives a completed generation run plus validation report.

**Why this priority**: Forecasting and optimization need relationship-rich,
reproducible history at demo scale.

**Independent Test**: Run standard profile with seed `104729`; assert entity
counts within configured tolerances and status `Completed`.

**Acceptance Scenarios**:

1. **Given** standard profile + seed, **When** generation completes, **Then**
   the manifest records generator version, schema version, configuration hash,
   entity counts, date range, and seed.
2. **Given** that run, **When** opening the validation report, **Then**
   referential integrity and invariant checks are listed with pass/fail.
3. **Given** the same profile + seed, **When** generation runs twice into
   separate datasets, **Then** configuration hashes match and key aggregates
   match exactly.

---

### User Story 2 — Generate thin-slice and custom profiles (Priority: P1)

An administrator runs the thin-slice profile (5/2/5/3, milk+cream, 90 days) or
a custom count/date-range profile without code changes.

**Why this priority**: Interview demos and workstation constraints require
small profiles; experiments need knobs.

**Independent Test**: Run thin-slice and a custom smaller profile; both complete
with correct counts.

**Acceptance Scenarios**:

1. **Given** thin-slice profile, **When** generated, **Then** counts match the
   profile and products are Raw milk + Cream only.
2. **Given** invalid profile (zero farms), **When** submitted, **Then** the
   system rejects before writing data.

---

### User Story 3 — Encode learnable structure and controlled noise (Priority: P2)

Generated series include seasonality, weather/heat stress effects, butterfat →
cream coupling, demand seasonality, price autocorrelation/shocks, and spoilage
risk signals — with documented noise so models cannot overfit “perfect” data.

**Why this priority**: ML features fail if synthetic data is unrealistically clean.

**Independent Test**: Statistical smoke checks on a generated standard dataset
(seasonal peaks present; missingness rate within configured band).

**Acceptance Scenarios**:

1. **Given** standard generation, **When** inspecting farm production over a
   year, **Then** seasonal variation is detectable above noise.
2. **Given** configured missingness, **When** validating, **Then** the report
   records observed missingness within tolerance.

---

### Edge Cases

- Mid-run failure marks the generation `Failed` with message; partial data is
  quarantined or rolled back per plan (no silent “Completed”).
- Concurrent generation requests are isolated by generation/dataset id.
- Extremely large custom profiles MAY be rejected with a documented max bound.

## Requirements

### Functional Requirements

- **FR-001**: System MUST support named generation profiles including at least
  `thin-slice` and `standard-demo`.
- **FR-002**: System MUST accept a random seed and record it on the generation
  manifest.
- **FR-003**: System MUST produce farms, facilities, products, customers,
  contracts, orders, trucks, inventory lots, shipments (historical), market
  prices, and weather (or weather-linked factors) consistent with domain rules.
- **FR-004**: System MUST emit a validation report covering referential
  integrity and domain invariants.
- **FR-005**: System MUST mark all generated data as synthetic / non-PII.
- **FR-005a**: Generated farms, facilities, and customers MUST include usable
  lat/lon so network **maps** in 001/004/000 can render without manual geocoding.
- **FR-006**: System MUST version schema and generator implementation on each
  run.
- **FR-007**: Same seed + profile configuration MUST reproduce matching
  aggregates and configuration hash.
- **FR-008**: Generation MUST be restartable or safely re-runnable without
  corrupting unrelated datasets.

### Key Entities

- **GenerationProfile**: Named config (counts, date range, product set, noise).
- **GenerationManifest**: Run metadata (seed, versions, hashes, counts, status).
- **ValidationReport**: Pass/fail checks with counts and samples.

## Success Criteria

- **SC-001**: Thin-slice and standard-demo profiles complete successfully under
  documented machine profiles.
- **SC-002**: Duplicate runs with same seed+profile match configuration hash and
  key aggregates 100% of automated trials (≥3).
- **SC-003**: Validation report fails the run when critical integrity checks fail.
- **SC-004**: No real PII in generated names or identifiers.

## Assumptions

- Program clarifications apply (pounds; Raw milk + Cream for R1 movable set;
  soft contracts with penalties when contracts are generated).
- Feature 000 generator may be generalized rather than replaced.
- Full 8-product reference catalog may exist as reference data even when only
  6 products are active in standard demo.

## Out of Scope

- Public data ingestion, ML training, OR-Tools, scenario UI, auth hardening.
