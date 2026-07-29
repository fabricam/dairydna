# Tasks: Public Data Ingestion

## Phase 1 — Domain & persistence

- [X] T001 Import entities + ImportRunStatus enum
- [X] T002 DbContext / IDairyDnaDbContext wiring
- [X] T003 Seed ImportSource catalog (fixture dairy/weather/fuel)

## Phase 2 — Ingestion engine

- [X] T004 DairyDNA.DataIngestion project + fixture files
- [X] T005 Schema validation + quarantine path
- [X] T006 Idempotent import by checksum + schema version
- [X] T007 Unit tests: three sources, quarantine, idempotency

## Phase 3 — API/UI/docs

- [X] T008 Import API endpoints
- [X] T009 Web Imports page + nav
- [X] T010 Integration tests
- [X] T011 quickstart + mark Implemented; `dotnet test` green
