# Feature Specification: Operational Dashboard

**Feature Branch**: `004-operational-dashboard`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program plan §6 / §10 / §17 Feature 004  
**Depends on**: 001–002 (network + generated ops data); may show 000 thin-slice panels as interim

## User Scenarios & Testing

### User Story 1 — See today’s network posture (Priority: P1)

An operations planner opens the dashboard for a selected generation/dataset and
as-of date and sees inventory by facility/product (with age/risk cues), open
demand, fleet availability, and market/static prices — all labeled Synthetic
or Public as appropriate.

**Why this priority**: Planners cannot decide movements without a coherent ops
picture; interview reviewers need the story frame.

**Independent Test**: Load standard or thin-slice dataset; verify panels populate
within performance budget for the documented demo workload.

**Acceptance Scenarios**:

1. **Given** a completed generation, **When** opening the ops dashboard,
   **Then** inventory, demand, fleet, and price summaries are visible for the
   as-of date.
2. **Given** aging cream lots, **When** viewing inventory, **Then** expiry/age
   risk is visually indicated without relying on color alone.
3. **Given** synthetic data, **When** any panel renders, **Then** a Synthetic
   (or Public) classification label is present.

---

### User Story 2 — Navigate network entities (Priority: P2)

A planner drills from summary tiles into farms, facilities, customers, and
trucks using Semantic UI + Fluxor-backed views.

**Why this priority**: Supports M1 “domain demo” and ties to 001 browse stories.

**Independent Test**: From dashboard, open facility detail and return without
losing selected dataset/as-of context.

**Acceptance Scenarios**:

1. **Given** dashboard context (dataset + as-of), **When** opening a facility,
   **Then** detail shows capacities and current inventory for that as-of.
2. **Given** inactive entities, **When** using default views, **Then** they are
   hidden unless “include inactive” is enabled.

---

### User Story 3 — Honest empty and loading states (Priority: P2)

When data is missing or still generating, the UI shows explicit empty/loading/
error states — never invents numbers.

**Why this priority**: Constitution VII honesty and demo trust.

**Independent Test**: Open dashboard with unknown generation id; see clear error.

**Acceptance Scenarios**:

1. **Given** no dataset selected, **When** opening dashboard, **Then** user is
   prompted to generate or select a dataset.
2. **Given** API failure, **When** loading a panel, **Then** an error message
   appears and other panels may still load independently.

---

### Edge Cases

- As-of date outside dataset range → clear validation message.
- Extremely large standard dataset: initial useful view still meets ≤2s budget
  via aggregation/paging (plan may use summaries).
- Keyboard navigation reaches primary controls and tables.

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide an operational dashboard for a selected
  dataset/generation and as-of date.
- **FR-002**: Dashboard MUST show inventory (with age/risk), demand, fleet, and
  prices at summary level.
- **FR-003**: UI MUST use Blazor + Semantic UI + Fluxor per pinned stack.
- **FR-004**: UI MUST label Synthetic vs Public vs Forecast vs Recommendation
  data.
- **FR-005**: Initial useful view SHOULD load within 2 seconds under the
  documented demo workload (constitution XII).
- **FR-006**: Dashboard MUST NOT require forecasting or OR-Tools to function.
- **FR-007**: Accessibility: keyboard operable primary flows; status not
  conveyed by color alone.

### Key Entities (read models)

- **DashboardContext**: Dataset/generation id + as-of date.
- **InventorySummary / DemandSummary / FleetSummary / PriceSummary**: Panel DTOs.

## Success Criteria

- **SC-001**: P1 acceptance scenarios pass manually and via UI/integration smoke.
- **SC-002**: Documented demo workload meets ≤2s useful-content budget on the
  reference machine profile (recorded in feature notes).
- **SC-003**: Unknown dataset yields explicit error, not blank fake zeros.

## Assumptions

- 000 demo home may be evolved or replaced by this dashboard.
- Recommendation and forecast panels appear as placeholders or links until
  005–009 land.

## Out of Scope

- Full scenario compare UI (010), model registry UI (011), replay controls (012).
