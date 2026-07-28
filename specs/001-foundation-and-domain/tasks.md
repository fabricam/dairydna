# Tasks: Foundation and Domain

**Input**: spec.md, plan.md, data-model.md  
**Organization**: Setup → Domain → API → UI → Polish

## Phase 1: Domain & persistence

- [X] T001 Add `Contract` and `Shipment` entities + enums in `src/DairyDNA.Domain/`
- [X] T002 Add `Active` to Product and Truck; expand OrderType / FacilityType as needed
- [X] T003 Expand `DomainInvariants` (contract dates, names, truck–product assignment)
- [X] T004 Register DbSets + configurations in `DairyDnaDbContext`
- [X] T005 [P] Unit tests for new/expanded invariants in `tests/DairyDNA.UnitTests/`

## Phase 2: Reference-data API

- [X] T006 Application handlers for list/get/create/deactivate (farms, facilities, customers, products, trucks, contracts)
- [X] T007 Map Minimal API endpoints in `ReferenceEndpoints.cs` (generationId required; activeOnly default true)
- [X] T008 Network points endpoint including farms (`GET /api/network?generationId=`)
- [X] T009 Shipment list/get (read-only) endpoints
- [X] T010 [P] Integration tests: invalid create 400; deactivate hides from default list; network includes farms

## Phase 3: Web UI

- [X] T011 Reference hub page `/reference` with entity type navigation (Fluxor or simple HttpClient)
- [X] T012 List + detail pages/sections for farms, facilities, customers, products, trucks, contracts
- [X] T013 Network page `/network` with map (farms + facilities + customers) + legend
- [X] T014 Soft-deactivate actions in UI with Synthetic labeling
- [X] T015 Nav links from MainLayout / demo home to Network and Reference

## Phase 4: Polish

- [X] T016 Update OpenAPI contract under `specs/001-foundation-and-domain/contracts/`
- [X] T017 quickstart.md validated; `dotnet test DairyDNA.sln` green
- [X] T018 Mark feature status Implemented in spec.md when done

## Dependencies

Phase 1 → Phase 2 → Phase 3 → Phase 4. T005/T010 parallel within phases.
