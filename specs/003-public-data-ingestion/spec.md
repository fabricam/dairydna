# Feature Specification: Public Data Ingestion

**Feature Branch**: `003-public-data-ingestion`  
**Created**: 2026-07-26  
**Status**: Draft — ready for `/speckit-clarify` → `/speckit-plan`  
**Product**: DairyDNA  
**Input**: Program plan §5 / §10 Feature 003  
**Depends on**: 001 (canonical observation stores); benefits from 002 for merge demos

## User Scenarios & Testing

### User Story 1 — Import selected public dairy/market series (Priority: P1)

A data steward configures a small set of public dairy price / market series,
runs an import, and sees versioned raw files plus canonical market-price
observations with provenance.

**Why this priority**: Forecasting and demos need real-world price context
alongside synthetic ops data — without claiming production market advice.

**Independent Test**: Import a fixture file representing a public series;
verify raw checksum, import-run status, and canonical rows.

**Acceptance Scenarios**:

1. **Given** a configured source and fixture payload, **When** import runs,
   **Then** an ImportRun records source, timestamps, row counts, and checksum.
2. **Given** successful import, **When** querying market prices for that
   product/region/date, **Then** observations are available with source label
   and MAY render on dashboard **price charts**.
3. **Given** the honesty boundary, **When** viewing imported data in UI/API,
   **Then** data is labeled as public/external — not DairyDNA forecasts.

---

### User Story 2 — Ingest weather and fuel-price inputs (Priority: P1)

A steward imports weather and fuel-price series used later for transport cost
and supply/demand features.

**Why this priority**: Flagship demo narrative depends on heat and diesel.

**Independent Test**: Import weather + fuel fixtures; confirm join keys
(region/date) are usable by later features.

**Acceptance Scenarios**:

1. **Given** weather fixture for a region/date range, **When** imported,
   **Then** daily weather factors are queryable by region and date.
2. **Given** fuel-price fixture, **When** imported, **Then** weekly (or
   documented cadence) fuel prices are queryable.

---

### User Story 3 — Quarantine bad payloads (Priority: P2)

Invalid, incomplete, or schema-mismatched files are quarantined; import run
fails or completes with explicit quarantine counts — never silently corrupts
canonical data.

**Why this priority**: Constitution XI/XIII — validated, versioned contracts.

**Independent Test**: Feed malformed fixture; assert quarantine + failed/partial
status without canonical pollution.

**Acceptance Scenarios**:

1. **Given** a file failing schema validation, **When** imported, **Then** it
   is quarantined and canonical tables are unchanged for that batch.
2. **Given** a re-import of the same checksummed file, **When** idempotent
   import is enabled, **Then** the system does not duplicate observations.

---

### Edge Cases

- Source unavailable: ImportRun `Failed` with retry guidance; no partial
  “success” labeling.
- Timezone/date boundary mismatches are documented and normalized to the
  planning calendar (UTC date or region-local per plan).
- Empty file → failed validation, not empty success.

## Requirements

### Functional Requirements

- **FR-001**: System MUST support import runs for at least: dairy market
  prices, weather, and fuel prices (fixture-driven acceptable for demo).
- **FR-002**: System MUST store versioned raw payloads with checksums.
- **FR-003**: System MUST validate against a versioned schema before promoting
  to canonical observations.
- **FR-004**: System MUST quarantine invalid batches and record reasons.
- **FR-005**: Imports MUST be idempotent for identical checksum + schema
  version when re-run.
- **FR-006**: Every canonical observation MUST carry source, as-of/effective
  date, and import-run id.
- **FR-007**: UI/API MUST label public/external data distinctly from synthetic
  and forecast data.
- **FR-008**: System MUST NOT present ingested public prices as DairyDNA
  trading recommendations.

### Key Entities

- **ImportSource**: Configured public/fixture source definition.
- **ImportRun**: Execution metadata and status.
- **RawPayload**: Checksummed stored bytes/file reference.
- **CanonicalObservation**: Normalized market price / weather / fuel row.
- **QuarantineItem**: Failed record/file with reason.

## Success Criteria

- **SC-001**: Fixture-based imports for the three source types succeed in CI.
- **SC-002**: Malformed fixture is quarantined 100% of automated trials.
- **SC-003**: Re-import of identical payload does not duplicate canonical rows.
- **SC-004**: Quickstart documents how to run a demo import without live
  internet (fixtures).

## Assumptions

- Live connector credentials are optional; fixtures satisfy acceptance for
  demo v1.
- Geographic keys map to DairyDNA regions from 001/002.
- No scraping of ToS-prohibited sources; only approved/fixture sources.

## Out of Scope

- Full enterprise MDM, real-time streaming, brokerage APIs, PII customer feeds.
