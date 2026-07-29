# Tasks: Synthetic Data Generator

## Phase 1 — Profiles & domain

- [X] T001 Define `GenerationProfile` catalog (`thin-slice`, `standard-demo`) + max bounds
- [X] T002 Extend `GenerationManifest` (GeneratorVersion, ProfileName, ValidationReportJson)
- [X] T003 Add `WeatherObservation` entity + DbSet
- [X] T004 Validate profile request (reject zero farms / over max) before writes

## Phase 2 — Generator

- [X] T005 Implement `SyntheticDataGenerator` supporting profiles + custom overrides
- [X] T006 Multi-product catalog for standard-demo (6 active); thin-slice milk+cream
- [X] T007 Seasonality, cream coupling, price autocorrelation, configurable missingness
- [X] T008 Generate weather observations + historical shipments
- [X] T009 Emit validation report (referential + invariants); fail run on critical fails
- [X] T010 [P] Unit tests: repro hash/counts; invalid profile; seasonal smoke on thin data

## Phase 3 — API/UI

- [X] T011 `GET /api/generation-profiles`; extend POST generation-runs for profileName
- [X] T012 `GET /api/generation-runs/{id}/validation-report`
- [X] T013 Demo UI profile selector (thin-slice / standard-demo / custom knobs)
- [X] T014 Integration tests for profiles + validation endpoint
- [X] T015 quickstart + mark spec Implemented; `dotnet test` green
