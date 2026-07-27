# Tasks: Thin Vertical Slice

**Input**: Design documents from `/specs/000-thin-vertical-slice/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included — constitution IX + SC-002/SC-003 require domain, optimizer,
generator reproducibility, and contract tests.

**Organization**: Setup → Foundational → US1 (allocation MVP) → US2 (reproducibility) → Polish

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: `[US1]` / `[US2]` for user-story phases only

## Path Conventions

Paths follow `src/` and `tests/` at repository root per plan.md.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution scaffolding and pinned dependencies

- [X] T001 Create `DairyDNA.sln` and solution folders `src/`, `tests/`, `data/synthetic/` at repository root
- [X] T002 [P] Create class library `src/DairyDNA.Domain/DairyDNA.Domain.csproj` (.NET 10)
- [X] T003 [P] Create class library `src/DairyDNA.Application/DairyDNA.Application.csproj` (.NET 10)
- [X] T004 [P] Create class library `src/DairyDNA.Infrastructure/DairyDNA.Infrastructure.csproj` (.NET 10)
- [X] T005 [P] Create class library `src/DairyDNA.DataGenerator/DairyDNA.DataGenerator.csproj` (.NET 10)
- [X] T006 [P] Create class library `src/DairyDNA.Optimization/DairyDNA.Optimization.csproj` (.NET 10)
- [X] T007 [P] Create web project `src/DairyDNA.Api/DairyDNA.Api.csproj` (ASP.NET Core, Minimal APIs only — no controllers)
- [X] T008 [P] Create Blazor web project `src/DairyDNA.Web/DairyDNA.Web.csproj` (Interactive Server)
- [X] T009 [P] Create Aspire host `src/DairyDNA.AppHost/DairyDNA.AppHost.csproj` and `src/DairyDNA.ServiceDefaults/DairyDNA.ServiceDefaults.csproj`
- [X] T010 [P] Create test projects `tests/DairyDNA.UnitTests/`, `tests/DairyDNA.IntegrationTests/`, `tests/DairyDNA.ContractTests/`, `tests/DairyDNA.Optimization.Tests/`
- [X] T011 Add project references per plan boundaries (Domain←Application←Infrastructure/Api/Web/Generator/Optimization; Api/Web→Application; AppHost→Api/Web/SQL Server)
- [X] T012 Add NuGet packages: EF Core SQL Server, Fluxor.Blazor.Web, xUnit, FluentAssertions, Aspire.Hosting.SqlServer (or current Aspire SQL Server package)
- [X] T013 [P] Add `.editorconfig` and ensure `dotnet build DairyDNA.sln` succeeds empty
- [X] T014 [P] Write ADR `docs/architecture/adr-0001-naive-optimizer.md` documenting temporary naive optimizer vs Feature 009 OR-Tools

**Checkpoint**: Empty solution builds

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared domain, persistence, Minimal API host, Blazor/Fluxor shell, Aspire wiring — blocks US1/US2

**⚠️ CRITICAL**: No user story work until this phase completes

- [X] T015 [P] Implement domain entities and value objects in `src/DairyDNA.Domain/` per `data-model.md` (Farm, Facility, Product, InventoryLot, Customer, Order, Truck, MarketPrice, GenerationManifest, OptimizationRun, RecommendedMovement)
- [X] T016 [P] Implement domain invariants/validators in `src/DairyDNA.Domain/Rules/` (positive quantities, ExpiresAt > ProducedAt, truck compatibility helpers)
- [X] T017 Create application ports in `src/DairyDNA.Application/Abstractions/` (`IDairyDnaDbContext`, `IThinSliceGenerator`, `IAllocationOptimizer`, `ITransportCostCalculator`)
- [X] T018 Implement EF Core `DairyDnaDbContext` and configurations in `src/DairyDNA.Infrastructure/Persistence/`
- [X] T019 Add EF Core SQL Server registration + initial migration in `src/DairyDNA.Infrastructure/Persistence/Migrations/`
- [X] T020 Implement problem-details + validation pipeline helpers in `src/DairyDNA.Api/Infrastructure/`
- [X] T021 Configure `src/DairyDNA.Api/Program.cs` as Minimal APIs only (no `MapControllers`); map `/health`; register DI; OpenAPI
- [X] T022 Create endpoint group stubs in `src/DairyDNA.Api/Endpoints/` (`GenerationEndpoints.cs`, `DemoEndpoints.cs`, `OptimizationEndpoints.cs`) matching `contracts/openapi.yaml`
- [X] T023 Wire Aspire SQL Server + Api + Web in `src/DairyDNA.AppHost/Program.cs` and ServiceDefaults OTel/health; configure local-dev binding only (do not present as internet-facing production)
- [X] T024 Add Semantic UI assets under `src/DairyDNA.Web/wwwroot/` (or documented CDN) and base layout classes
- [X] T025 Register Fluxor in `src/DairyDNA.Web/Program.cs` and add Store folder scaffold `src/DairyDNA.Web/Store/`
- [X] T026 Create Blazor shell pages `src/DairyDNA.Web/Components/Pages/Home.razor` and navigation using Semantic UI
- [X] T027 [P] Add unit tests for domain invariants in `tests/DairyDNA.UnitTests/Domain/`
- [X] T028 [P] Add WebApplicationFactory smoke for `/health` in `tests/DairyDNA.IntegrationTests/HealthTests.cs`

**Checkpoint**: Aspire brings up API+Web+SQL Server; `/health` returns Healthy; domain tests pass

---

## Phase 3: User Story 1 — Answer today’s allocation question (Priority: P1) 🎯 MVP

**Goal**: Generate thin-slice data, view demo summary, run optimize, see feasible recommendation table with margin breakdown

**Independent Test**: Generate → demo summary → optimize → inspect movements (quickstart Scenario A)

### Tests for User Story 1

- [X] T029 [P] [US1] Add transport-cost unit tests in `tests/DairyDNA.UnitTests/Transport/TransportCostCalculatorTests.cs`
- [X] T030 [P] [US1] Add known-answer optimizer fixtures in `tests/DairyDNA.Optimization.Tests/` (one origin/one customer; higher price worse after transport; insufficient capacity; negative-margin hold; expired inventory excluded; zero demand → no movements + full unused inventory)
- [X] T031 [P] [US1] Add contract tests for generation/demo/optimization paths in `tests/DairyDNA.ContractTests/` from `contracts/openapi.yaml`

### Implementation for User Story 1

- [X] T032 [P] [US1] Implement `TransportCostCalculator` in `src/DairyDNA.Application/Transport/TransportCostCalculator.cs` (distance, fuel, operating; money quantized to 2 decimals)
- [X] T033 [US1] Implement `NaiveContributionMarginOptimizer` (`naive-cm-v1`) in `src/DairyDNA.Optimization/NaiveContributionMarginOptimizer.cs` with deterministic sort, independent feasibility validation, delivery-window checks, expired-lot exclusion, partial fills down to `MinimumAcceptableQuantityPounds`, and split loads across trucks; include per-movement explanation text covering margin factors and binding constraints
- [X] T034 [US1] Implement thin-slice generator in `src/DairyDNA.DataGenerator/ThinSliceGenerator.cs` (5/2/5/3, RAW_MILK+CREAM, 90 days, seed default `104729`, `IsSynthetic=true`)
- [X] T035 [US1] Implement `CreateGenerationRun` / `GetGenerationRun` use cases in `src/DairyDNA.Application/Generation/`
- [X] T036 [US1] Map Minimal API generation endpoints in `src/DairyDNA.Api/Endpoints/GenerationEndpoints.cs`
- [X] T037 [US1] Implement `GetDemoSummary` use case in `src/DairyDNA.Application/Demo/GetDemoSummary.cs`
- [X] T038 [US1] Map Minimal API demo endpoint in `src/DairyDNA.Api/Endpoints/DemoEndpoints.cs`
- [X] T039 [US1] Implement `CreateOptimizationRun` / `GetOptimizationRun` use cases in `src/DairyDNA.Application/Optimization/` (no contract penalties; no safety stock; no plant processing-capacity constraints; reject negative-margin moves; enforce delivery windows; support partial fill + multi-truck splits)
- [X] T040 [US1] Map Minimal API optimization endpoints in `src/DairyDNA.Api/Endpoints/OptimizationEndpoints.cs`
- [X] T041 [P] [US1] Add Fluxor generation store (state/actions/effects/reducers) in `src/DairyDNA.Web/Store/Generation/`
- [X] T042 [P] [US1] Add Fluxor demo summary store in `src/DairyDNA.Web/Store/Demo/`
- [X] T043 [P] [US1] Add Fluxor optimization store in `src/DairyDNA.Web/Store/Optimization/`
- [X] T044 [US1] Build Semantic UI demo home in `src/DairyDNA.Web/Components/Pages/DemoHome.razor` showing inventory, demand, prices, fleet (label data as Synthetic)
- [X] T045 [US1] Build Semantic UI recommendations view in `src/DairyDNA.Web/Components/Pages/Recommendations.razor` backed by `OptimizationRun` (show revenue, transport cost, margin, unused inventory, unserved demand, status, explanation with binding constraints/assumptions)
- [X] T046 [US1] Wire generate → summarize → optimize button flow on demo pages using Fluxor effects calling Minimal APIs
- [X] T047 [US1] Add structured logging/metrics for generation and optimize duration in Api/Application
- [X] T048 [US1] Add integration test `tests/DairyDNA.IntegrationTests/ThinSliceHappyPathTests.cs` covering generate → summary → optimize

**Checkpoint**: US1 demo path works locally; SC-001/SC-003 covered; infeasible never labeled recommended

---

## Phase 4: User Story 2 — Reproduce the slice (Priority: P1b)

**Goal**: Same seed/config yields identical generation aggregates and exact objective/quantities (costs ≤ 0.01)

**Independent Test**: quickstart Scenario B + automated reproducibility tests

### Tests for User Story 2

- [X] T049 [P] [US2] Generator seed reproducibility tests in `tests/DairyDNA.UnitTests/Generation/ThinSliceGeneratorReproTests.cs`
- [X] T050 [P] [US2] Optimization reproducibility tests (exact objective/quantities; cost abs diff ≤ 0.01) in `tests/DairyDNA.Optimization.Tests/ReproducibilityTests.cs`

### Implementation for User Story 2

- [X] T051 [US2] Ensure generator records `GenerationManifest` fields (seed, schemaVersion, configurationHash, entity counts) in `src/DairyDNA.DataGenerator/` and persistence
- [X] T052 [US2] Enforce deterministic optimizer tie-break ordering documented in `src/DairyDNA.Optimization/NaiveContributionMarginOptimizer.cs`
- [X] T053 [US2] Quantize all money fields to 2 decimals at cost and objective boundaries in Application/Optimization layers
- [X] T054 [US2] Add UI/API display of seed, schema version, optimizer version on demo/results pages for interview transparency
- [X] T055 [US2] Add integration repro test running generate+optimize twice in `tests/DairyDNA.IntegrationTests/ReproducibilityIntegrationTests.cs`

**Checkpoint**: SC-002 satisfied automatically and via quickstart Scenario B

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Demo readiness and documentation alignment

- [X] T056 [P] Update `README.md` with Aspire run instructions for thin-slice demo; state clearly that 000 is local-dev only, unauthenticated, and not production-secure
- [X] T057 [P] Validate and adjust `specs/000-thin-vertical-slice/quickstart.md` against actual ports/paths
- [X] T058 [P] Ensure OpenAPI document emitted by Api matches `contracts/openapi.yaml` (or document intentional deltas)
- [X] T059 Confirm no MVC controllers exist under `src/DairyDNA.Api/` (Minimal APIs only)
- [X] T060 Run full `dotnet test DairyDNA.sln` and fix failures
- [X] T061 Manual pass of quickstart Scenarios A–D; record results and wall-clock timings for demo home load and optimize vs plan budgets (≤2s useful content, ≤30s optimize) in `docs/architecture/demo-000-notes.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: start immediately
- **Phase 2 Foundational**: after Setup — **blocks** US1/US2
- **Phase 3 US1**: after Foundational — MVP
- **Phase 4 US2**: after US1 generator/optimizer exist (builds on US1)
- **Phase 5 Polish**: after desired stories complete

### User Story Dependencies

- **US1**: after Foundational only
- **US2**: after US1 implementation of generator + optimizer (repro strengthens US1)

### Parallel Opportunities

- T002–T010 scaffolding in parallel
- T015–T016 domain vs later infra once ports exist
- T029–T031 US1 tests in parallel before/with implementation
- T041–T043 Fluxor stores in parallel
- T049–T050 US2 tests in parallel
- T056–T058 polish docs in parallel

---

## Parallel Example: User Story 1

```text
# Tests in parallel:
T029 Transport cost unit tests
T030 Optimizer known-answer fixtures
T031 Contract tests

# Fluxor stores in parallel after APIs exist:
T041 Generation store
T042 Demo store
T043 Optimization store
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 Setup
2. Phase 2 Foundational
3. Phase 3 US1
4. **STOP** — validate quickstart Scenario A
5. Then US2 reproducibility + polish

### Suggested MVP scope

T001–T048 (through US1 integration test). US2 (T049–T055) required before calling the feature demo-interview ready per SC-002.

---

## Notes

- No authentication tasks (clarify / FR-006)
- No OR-Tools in 000 (ADR T014)
- No contract entities / safety stock / plant processing-capacity constraints in optimizer inputs
- Worker project optional — omit unless sync generate proves too slow
- Canonical API entity: `OptimizationRun`; UI “Recommendations” is a display label only
- Analyze remediation 2026-07-26: I1/U1–U6 addressed in spec + tasks
