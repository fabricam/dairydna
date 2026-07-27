# Feature Specification: Operational Dashboard

**Feature Branch**: `004-operational-dashboard`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program plan §6 / §10 / §17 Feature 004  
**Depends on**: 001–002 (network + generated ops data); may show 000 thin-slice panels as interim  
**Visual standard**: `specs/_visual-aids.md`

## User Scenarios & Testing

### User Story 1 — See today’s network posture (Priority: P1)

An operations planner opens the dashboard for a selected generation/dataset and
as-of date and sees a **network map**, inventory **age/risk chart**, open demand
summary, fleet availability, and price **sparklines/series** — all labeled
Synthetic or Public as appropriate. Tables support drill-down; they are not the
only primary view.

**Why this priority**: Planners cannot decide movements without a coherent ops
picture; interview reviewers need the story frame.

**Independent Test**: Load standard or thin-slice dataset; verify map + charts
populate within performance budget for the documented demo workload.

**Acceptance Scenarios**:

1. **Given** a completed generation, **When** opening the ops dashboard,
   **Then** a network map shows facilities and customers, and inventory, demand,
   fleet, and price summaries are visible for the as-of date.
2. **Given** aging cream lots, **When** viewing inventory visuals, **Then** an
   age or days-to-expiry chart indicates risk without relying on color alone.
3. **Given** synthetic data, **When** any panel or chart renders, **Then** a
   Synthetic (or Public) classification label is present.

---

### User Story 2 — Navigate network entities from the map (Priority: P2)

A planner selects a facility or customer on the map (or list) and drills into
detail using Semantic UI + Fluxor-backed views, retaining dataset/as-of context.

**Why this priority**: Supports M1 “domain demo” and ties to 001 browse stories.

**Independent Test**: Click a map marker → facility detail → return without
losing selected dataset/as-of context.

**Acceptance Scenarios**:

1. **Given** dashboard context (dataset + as-of), **When** selecting a facility
   on the map, **Then** detail shows capacities and current inventory for that
   as-of.
2. **Given** inactive entities, **When** using default views, **Then** they are
   hidden unless “include inactive” is enabled.

---

### User Story 3 — Honest empty and loading states (Priority: P2)

When data is missing or still generating, the UI shows explicit empty/loading/
error states — never invents numbers or empty charts that look like zero stock.

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
- Keyboard navigation reaches primary controls, map legend, and chart summaries.
- Map with missing coordinates: entity omitted from map with count warning, still
  listed in tables.

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide an operational dashboard for a selected
  dataset/generation and as-of date.
- **FR-002**: Dashboard MUST show inventory (with age/risk), demand, fleet, and
  prices at summary level.
- **FR-002a**: Dashboard MUST include a **network map** (farms optional;
  facilities and customers required) plotted from lat/lon.
- **FR-002b**: Dashboard MUST include an **inventory age/risk chart** (histogram
  or stacked bars by days-to-expiry or age band).
- **FR-002c**: Dashboard MUST include at least one **price time-series or
  sparkline** for active products on the as-of context.
- **FR-003**: UI MUST use Blazor + Semantic UI + Fluxor per pinned stack.
- **FR-004**: UI MUST label Synthetic vs Public vs Forecast vs Recommendation
  data on every visual.
- **FR-005**: Initial useful view SHOULD load within 2 seconds under the
  documented demo workload (constitution XII).
- **FR-006**: Dashboard MUST NOT require forecasting or OR-Tools to function.
- **FR-007**: Accessibility: keyboard operable primary flows; status not
  conveyed by color alone; charts MUST offer a text/table alternative.

### Key Entities (read models)

- **DashboardContext**: Dataset/generation id + as-of date.
- **InventorySummary / DemandSummary / FleetSummary / PriceSummary**: Panel DTOs.
- **NetworkMapPoint**: Entity id, type, lat, lon, label, risk/status attributes.
- **ChartSeries**: Labeled series for inventory age, prices, etc.

## Success Criteria

- **SC-001**: P1 acceptance scenarios pass manually and via UI/integration smoke
  including map + inventory chart visibility.
- **SC-002**: Documented demo workload meets ≤2s useful-content budget on the
  reference machine profile (recorded in feature notes).
- **SC-003**: Unknown dataset yields explicit error, not blank fake zeros.

## Assumptions

- 000 demo home may be evolved or replaced by this dashboard.
- Recommendation flow arcs and forecast bands appear when 005–009 land (placeholders OK until then).
- Schematic plot is sufficient; commercial map tiles optional.

## Out of Scope

- Full scenario compare UI (010), model registry UI (011), replay controls (012).
