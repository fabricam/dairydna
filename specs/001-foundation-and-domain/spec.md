# Feature Specification: Foundation and Domain

**Feature Branch**: `001-foundation-and-domain`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program plan `specs/000-program/program-plan.md` + original DairyFlow foundation intent, renamed and scoped  
**Depends on**: 000 (shared domain types may already exist; expand coverage here)

## User Scenarios & Testing

### User Story 1 — Browse the dairy network (Priority: P1)

A demo administrator opens DairyDNA and views farms, facilities, customers,
products, trucks, and contracts so they can understand the operating network
before generating or optimizing data. Spatial entities MUST be visible on a
**simple network map** (regional lat/lon plot), not only in lists.

**Why this priority**: Without a shared domain language and browsable reference
data, later forecasting and optimization features have nothing coherent to
attach to.

**Independent Test**: Seed a small set of reference entities and verify list,
detail, and map views for farms, facilities, and customers.

**Acceptance Scenarios**:

1. **Given** seeded reference data, **When** an administrator opens the farms
   list, **Then** each farm shows region, herd size, and active status.
2. **Given** seeded facilities, **When** filtering by facility type, **Then**
   only matching facilities appear.
3. **Given** seeded farms, facilities, and customers with coordinates, **When**
   opening the network map, **Then** each active entity appears at its lat/lon
   with a legend distinguishing entity types.
4. **Given** the application is running, **When** a health endpoint is queried,
   **Then** the system reports a healthy ready state for core dependencies.

---

### User Story 2 — Enforce domain invariants (Priority: P1)

A demo administrator attempts to create or update reference data. Invalid
states are rejected with clear errors; valid states persist.

**Why this priority**: Feasibility-before-profitability and later optimization
depend on refusing impossible domain states early.

**Independent Test**: Attempt invalid creates/updates via API and confirm
rejection without persistence.

**Acceptance Scenarios**:

1. **Given** a facility create request with negative storage capacity, **When**
   submitted, **Then** the system rejects it.
2. **Given** an inventory lot whose expiration is before production, **When**
   submitted, **Then** the system rejects it.
3. **Given** a truck incompatible with a product type, **When** assigning that
   product to a shipment or capacity rule, **Then** the system rejects it.
4. **Given** a contract with end date before start date, **When** submitted,
   **Then** the system rejects it.
5. **Given** an order with non-positive requested quantity, **When** submitted,
   **Then** the system rejects it.

---

### User Story 3 — Soft-deactivate without destroying history (Priority: P2)

An administrator deactivates a farm, facility, customer, truck, or product.
Historical records remain queryable; inactive entities are excluded from
default “active network” views.

**Why this priority**: Demo and replay workflows need stable historical
identity without hard deletes.

**Independent Test**: Deactivate an entity, confirm it is hidden from default
active lists but still retrievable by id including historical flag.

**Acceptance Scenarios**:

1. **Given** an active customer, **When** deactivated, **Then** default
   customer lists omit it and detail view shows inactive status.
2. **Given** historical orders for that customer, **When** queried, **Then**
   those orders remain available.

---

### Edge Cases

- Concurrent updates to the same entity return a conflict rather than silent
  overwrite when concurrency tokens are present.
- Deactivating a facility that still has active inventory is allowed but MUST
  surface a warning in the API/UI response metadata.
- Unknown facility types or product codes are rejected.
- Empty names / whitespace-only names are rejected.

## Requirements

### Functional Requirements

- **FR-001**: System MUST represent farms, facilities, products, inventory lots,
  customers, contracts, orders, trucks, shipments, and market prices with
  documented business meaning.
- **FR-002**: System MUST validate positive quantities, non-negative capacities,
  delivery window ordering, contract date ordering, expiration after production,
  and product–truck compatibility.
- **FR-003**: Users MUST be able to list and view detail for farms, facilities,
  customers, products, trucks, and contracts.
- **FR-003a**: Users MUST be able to view farms, facilities, and customers on a
  **network map** (schematic lat/lon plot with legend). See `specs/_visual-aids.md`.
- **FR-004**: System MUST allow soft-deactivation of reference entities without
  deleting historical records.
- **FR-005**: System MUST expose health information for the running application.
- **FR-006**: System MUST start locally as one orchestrated environment for
  API, web UI, and database.
- **FR-007**: System MUST NOT require forecasting, optimization, or public-data
  ingestion to satisfy this feature’s acceptance scenarios.
- **FR-008**: Quantities and capacities for this feature MUST use pounds as the
  unit of measure (see program clarifications).

### Key Entities

- **Farm**: Milk supply source with region, herd, component baselines.
- **Facility**: Receiving, separation, processing, or storage site with
  capacities and costs.
- **Product**: Typed dairy product with shelf-life and component constraints.
- **Inventory Lot**: Quantity of a product at a facility with quality and age.
- **Customer**: Demand destination with preferences and credit category.
- **Contract**: Committed min/max quantities and prices with shortfall penalty.
- **Order**: Contract, spot, or internal-transfer request with delivery window.
- **Truck**: Capacity, compatibility, cost rates, availability window.
- **Shipment**: Historical or planned movement record (create/read in foundation;
  optimizer-owned creation comes later).
- **Market Price**: Dated price observation by product and region.

Forecast and recommendation entities are **out of scope for persistence
workflows in 001**; they arrive with forecasting/optimization features.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All P1 acceptance scenarios pass via automated tests.
- **SC-002**: Invalid domain examples in this spec are rejected 100% of the time
  in automated tests.
- **SC-003**: A developer can start the orchestrated local environment and open
  the reference-data UI within the documented quickstart steps.
- **SC-004**: Health check reports ready when the database is reachable.

## Assumptions

- Program clarifications in `specs/000-program/clarifications.md` apply.
- Feature **000** (thin slice), if implemented first, may share domain types;
  this feature expands reference-data management and invariant coverage.
- Auth for demo admin may be a simple configured demo identity in early
  milestones; full identity provider is not required for 001.
- No real PII; all names are synthetic.

## Out of Scope

- ML forecasting, OR-Tools optimization, scenario simulation, public ingestion,
  model registry, historical replay automation beyond basic entity CRUD/read.
