# Feature Specification: Historical Replay

**Feature Branch**: `012-historical-replay`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program plan M4 + Feature 012  
**Depends on**: 002 datasets, 005–007 forecasts (optional modes), 009 optimize, 010 scenarios

## User Scenarios & Testing

### User Story 1 — Replay a planning day (Priority: P1)

A reviewer selects a historical as-of date in a dataset and replays the planning
loop (snapshot inputs → optional forecasts as-of → optimize) producing an
`OptimizationRun` comparable to what would have been recommended that day.

**Why this priority**: Proves decisions are time-consistent and demoable on past
days without leakage.

**Independent Test**: Replay day D using only data available as-of D; assert no
post-D features in the replay feature set.

**Acceptance Scenarios**:

1. **Given** a multi-day synthetic dataset, **When** replaying date D,
   **Then** a ReplayRun completes with linked OptimizationRun.
2. **Given** that replay, **When** auditing features/prices used, **Then** none
   have effective timestamps after D.
3. **Given** the same replay request twice, **When** completed, **Then**
   objective and quantities match (cost ≤0.01 tolerance).

---

### User Story 2 — Regret vs baselines (Priority: P1)

For a replay window, DairyDNA compares optimizer recommendations to simple
policies (e.g., nearest customer, highest price first) on realized or
proxy outcomes (margin estimate, spoilage, unmet demand).

**Why this priority**: Portfolio metric — recommendations beat naive policies
on at least one documented scenario.

**Independent Test**: Replay window report shows optimizer vs baselines.

**Acceptance Scenarios**:

1. **Given** a documented evaluation window, **When** regret report runs,
   **Then** metrics for optimizer and ≥2 baselines are present.
2. **Given** the flagship-compatible window, **When** reported, **Then**
   optimizer wins on at least one primary metric or the report explicitly
   records failure to meet the bar.

---

### User Story 3 — Step through days with timeline visuals (Priority: P2)

A reviewer steps as-of date forward/back via a **timeline or date scrubber** and
sees dashboard map/charts + recommendation outputs update for replay context.

**Why this priority**: Interview narrative control.

**Independent Test**: Change as-of; map/charts refresh to that date’s replay or
prompt to run replay.

**Acceptance Scenarios**:

1. **Given** replay results for dates D and D+1, **When** stepping the control,
   **Then** the correct run’s summary and charts are shown.
2. **Given** a date without replay, **When** selected, **Then** UI offers run
   replay rather than showing another day’s plan silently.
3. **Given** a regret report, **When** opened, **Then** optimizer vs baseline
   metrics appear as a **comparison chart**, not metrics-only prose.

---

### Edge Cases

- First day of dataset: limited history forecasts may be cold-start; labeled.
- Missing forecast models: replay can use actuals/static prices with mode flag.
- Cross-dataset replay forbidden.

## Requirements

### Functional Requirements

- **FR-001**: System MUST support ReplayRun for a dataset + as-of date.
- **FR-002**: Replay MUST enforce as-of data availability (no leakage).
- **FR-003**: System MUST compute regret/baseline comparison reports for a
  date window.
- **FR-004**: Replays MUST record model/optimizer/costing versions used.
- **FR-005**: UI MUST allow selecting as-of date and viewing replay outputs.
- **FR-005a**: Replay UI MUST include a **date timeline/scrubber** and reuse
  dashboard **map/chart** visuals for the selected as-of; regret reports MUST
  include a **comparison chart** (`specs/_visual-aids.md`).
- **FR-006**: Reproducibility rules match optimization (exact objective/
  quantities; costs ≤0.01).

### Key Entities

- **ReplayRun**, **ReplayWindowReport**, **BaselinePolicyResult**

## Success Criteria

- **SC-001**: Leakage audit passes for replay feature construction.
- **SC-002**: At least one checked-in regret report artifact for the demo
  window.
- **SC-003**: Dual replay determinism test passes.

## Assumptions

- Realized “actual outcomes” may be proxy economics from the synthetic world,
  clearly labeled.
- Not a claim of historical market performance.

## Out of Scope

- Live production backtesting against real P&L systems.
