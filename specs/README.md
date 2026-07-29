# DairyDNA Feature Specs Index

**Program**: `specs/000-program/program-plan.md`  
**Clarifications**: `specs/000-program/clarifications.md`  
**Constitution**: `.specify/memory/constitution.md`

Use this index to review specs and activate a feature for plan → tasks → implement.

## Roadmap

| # | Directory | Status | Spec |
|---|-----------|--------|------|
| 000 | `000-thin-vertical-slice` | Implemented (MVP) | [spec.md](./000-thin-vertical-slice/spec.md) |
| 001 | `001-foundation-and-domain` | Implemented | [spec.md](./001-foundation-and-domain/spec.md) |
| 002 | `002-synthetic-data-generator` | Implemented | [spec.md](./002-synthetic-data-generator/spec.md) |
| 003 | `003-public-data-ingestion` | Draft | [spec.md](./003-public-data-ingestion/spec.md) |
| 004 | `004-operational-dashboard` | Draft | [spec.md](./004-operational-dashboard/spec.md) |
| 005 | `005-supply-forecasting` | Draft | [spec.md](./005-supply-forecasting/spec.md) |
| 006 | `006-demand-forecasting` | Draft | [spec.md](./006-demand-forecasting/spec.md) |
| 007 | `007-price-forecasting` | Draft | [spec.md](./007-price-forecasting/spec.md) |
| 008 | `008-transportation-costing` | Draft | [spec.md](./008-transportation-costing/spec.md) |
| 009 | `009-movement-optimization` | Draft | [spec.md](./009-movement-optimization/spec.md) |
| 010 | `010-scenario-simulation` | Draft | [spec.md](./010-scenario-simulation/spec.md) |
| 011 | `011-model-governance` | Draft | [spec.md](./011-model-governance/spec.md) |
| 012 | `012-historical-replay` | Draft | [spec.md](./012-historical-replay/spec.md) |
| 013 | `013-demo-hardening` | Draft | [spec.md](./013-demo-hardening/spec.md) |

## How to review

1. Read the feature `spec.md` and the program clarifications.
2. Check [`_visual-aids.md`](./_visual-aids.md) — UI features must include maps/charts where listed.
3. Optionally run `/speckit-clarify` on that feature to lock open questions.
4. Run `/speckit-checklist` if you want a review checklist before planning.

## How to run (Spec Kit loop)

Activate the feature, then continue the normal loop:

```powershell
# Point Spec Kit at the feature (example: 002)
# Edit .specify/feature.json → "feature_directory": "specs/002-synthetic-data-generator"
# Or use your Spec Kit / Cursor skill that sets the active feature.
```

Then for the active feature:

```text
/speckit-clarify
/speckit-plan
/speckit-tasks
/speckit-analyze
/speckit-implement
```

Do **not** jump to implement without plan + tasks for draft features.

## Milestone map

| Milestone | Features |
|-----------|----------|
| M0 Architecture proof | 000 |
| M1 Domain demo | 001, 002, 004 |
| M2 Forecasting | 003, 005, 006, 007, 011 |
| M3 Optimization | 008, 009, 010 |
| M4 Portfolio | 012, 013 |

**Visuals (all milestones):** network map; inventory age chart; forecast bands; margin/cost bars; scenario compare charts; optional flow arcs — see [`_visual-aids.md`](./_visual-aids.md).

