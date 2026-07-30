# Feature Specification: Demo Hardening

**Feature Branch**: `013-demo-hardening`  
**Created**: 2026-07-26  
**Status**: Implemented  
**Product**: DairyDNA  
**Input**: Program plan M4 / §14–§19 + Feature 013  
**Depends on**: Prior features used in the flagship demo path (000–012 as available)

## User Scenarios & Testing

### User Story 1 — One-command local demo (Priority: P1)

A developer clones the repo, runs a single documented command (Aspire AppHost
or script), and reaches a ready demo: data generated (or loaded), dashboard
open, optimize runnable — within documented time on the reference machine.

**Why this priority**: Interview and reviewer friction kills otherwise good demos.

**Independent Test**: Follow quickstart on a clean machine profile; measure
time-to-first-useful-screen.

**Acceptance Scenarios**:

1. **Given** prerequisites installed, **When** running the documented demo
   start command, **Then** API, Web, and database become healthy.
2. **Given** the started environment, **When** following the demo script,
   **Then** flagship story steps complete without manual DB edits.
3. **Given** README/quickstart, **When** reviewed, **Then** honesty boundary
   and limitations are stated upfront.

---

### User Story 2 — Stable demo script & seed pack (Priority: P1)

A fixed demo script (seed, profile, scenarios, expected talking points) is
versioned so two presenters get the same logical outcomes.

**Why this priority**: Constitution III reproducible demonstrations.

**Independent Test**: Run demo script twice; recommendation economics match
tolerances.

**Acceptance Scenarios**:

1. **Given** demo seed pack, **When** executed twice, **Then** objective and
   quantities match exactly (costs ≤0.01).
2. **Given** the script, **When** a step fails, **Then** troubleshooting notes
   exist for common failures (Docker, ports, locked files).

---

### User Story 3 — Observability and safety pass (Priority: P2)

Health checks, structured logs/traces, and basic security hygiene (no secrets in
repo, input validation on demo posts, clear local-dev auth posture) are verified.

**Why this priority**: Constitution X/XI; avoids embarrassing demo failures.

**Independent Test**: Checklist automation + manual sample of traces in Aspire
dashboard.

**Acceptance Scenarios**:

1. **Given** running demo, **When** checking Aspire/OTel, **Then** API and Web
   emit traces for optimize/generate.
2. **Given** repository scan, **When** checking for secrets, **Then** no
   committed credentials.
3. **Given** local-dev auth posture, **When** documented, **Then** it is
   explicit (open vs basic demo auth) and not claimed production-ready.

---

### Edge Cases

- Port conflicts: script documents alternate ports or fails with clear message.
- Partial feature set: demo script degrades gracefully (e.g., static prices if
  forecasts absent) with labels.
- Offline mode: fixture-only path when public ingestion is unavailable.

## Requirements

### Functional Requirements

- **FR-001**: System MUST provide a one-command (or one documented short
  sequence) local demo bring-up.
- **FR-002**: System MUST version a demo seed pack and presenter script.
- **FR-003**: Demo path MUST meet performance budgets for dashboard and
  optimize on the reference profile (record measurements).
- **FR-004**: Observability MUST cover generate, forecast (if enabled), and
  optimize jobs.
- **FR-005**: Documentation MUST state honesty boundary and R1 logistics
  simplifications.
- **FR-005a**: Demo script MUST exercise **network map**, at least one **chart**
  (inventory age, forecast band, or margin breakdown), and recommendations —
  not a tables-only walkthrough (`specs/_visual-aids.md`).
- **FR-006**: Accessibility smoke: keyboard reachability of primary demo
  controls including map/chart alternatives.
- **FR-007**: Failed jobs MUST surface actionable errors in UI/logs.

### Key Deliverables (not only entities)

- **DemoSeedPack**, **PresenterScript**, **ReferenceMachineProfile**
- **DemoHardeningNotes** with timings and known limitations

## Success Criteria

- **SC-001**: Clean-machine quickstart succeeds for a second engineer following
  docs only (peer test or recorded session).
- **SC-002**: Demo script reproducibility verified in CI where feasible.
- **SC-003**: Performance budgets recorded for dashboard ≤2s and optimize ≤30s.
- **SC-004**: Secret scan and health-check gates pass in CI.

## Assumptions

- Not all of 001–012 must be fully complete; hardening targets the current
  flagship path and documents gaps.
- Production deployment hardening is out of scope; local/demo focus only.

## Out of Scope

- Multi-tenant SaaS, production IdP mandate, autonomous dispatch, app store
  packaging.
