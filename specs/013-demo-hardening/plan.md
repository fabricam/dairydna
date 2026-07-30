# Implementation Plan: Demo Hardening

**Branch**: `013-demo-hardening` | **Date**: 2026-07-29 | **Spec**: [spec.md](./spec.md)

## Summary

Feature 013 is deliberately documentation- and glue-heavy, not a new
subsystem. The flagship path (generate → forecast/models → optimize →
scenarios → replay → visuals) already works end to end (000–012); this
feature versions it (a fixed `DemoSeedPack`), documents it (presenter
script, hardening notes, honesty boundary), scripts it (one-command
bring-up + smoke check), adds a thin optional bootstrap convenience
endpoint, adds minimal observability spans, and adds lightweight CI-ish
checks. No new domain entities, no changes to the optimizer/generator
business logic, and no change to `DairyDNA.AppHost.csproj`.

## Approach

1. **`DemoSeedPack` (Application/Demo)**: a static class holding the fixed
   profile (`thin-slice`), seed (`104729` — confirmed as the app's existing
   default in `GenerationEndpoints`/`SyntheticGenerationRequest`), the
   flagship scenario names (from `ScenarioService.ApplyFlagshipPack`, 010),
   the web routes used in the demo, and the doc paths that describe it. This
   is the single source of truth `docs/demo/*.md` and the tests reference.
2. **`DemoBootstrapHandler` + `POST /api/demo/bootstrap`**: thin composition
   of three existing handlers (`CreateGenerationRunHandler` →
   `IScenarioService.ApplyFlagshipPack` → `CreateOptimizationRunHandler`) so
   a single call reaches a ready demo state. No new persistence, no new
   business rules. Convenience only — the presenter script still walks the
   UI step by step.
3. **Observability**: a shared `ActivitySource` (`DairyDnaTelemetry`, named
   `DairyDNA.Api` to match `IHostEnvironment.ApplicationName`, which
   `ServiceDefaults` already registers via `tracing.AddSource(...)`) backs
   two new named spans — `DairyDNA.Generate` (wraps
   `CreateGenerationRunHandler.HandleAsync`) and `DairyDNA.Optimize` (wraps
   `CreateOptimizationRunHandler.HandleAsync`) — plus
   `DairyDNA.Demo.Bootstrap`. `DairyDNA.ServiceDefaults` itself is
   unchanged.
4. **Docs** (`docs/demo/`): `presenter-script.md` (versioned, labeled
   Synthetic/Forecast/Recommendation walkthrough exercising the network map,
   at least one chart, and recommendations per FR-005a, plus
   troubleshooting), `seed-pack.md` (the `DemoSeedPack` values plus key
   URLs), `hardening-notes.md` (reference machine profile, freshly measured
   performance budgets, known limitations including a documented
   `thin-slice`+`104729` zero-movement case, R1 logistics simplifications,
   Aspire/OTel notes), `honesty-boundary.md` (what this is/is not).
5. **Scripts**: `scripts/demo-start.ps1` (restore/build + `dotnet run
   --project src/DairyDNA.AppHost`, with an `-InMemory` fallback and
   troubleshooting text) and `scripts/demo-smoke.ps1` (hits `/health` and
   optionally `/api/demo/bootstrap` against an already-running API).
   `scripts/ci-checks.ps1` runs `dotnet test DairyDNA.sln`, a lightweight
   regex-based secret scan over `git ls-files`, and confirms a health-check
   test exists. `.github/workflows/ci.yml` runs the same build/test plus the
   secret-scan-only mode of `ci-checks.ps1` in a second job.
6. **README**: honesty boundary moved up front, one-command demo section,
   links to the new docs, active-feature pointer updated to 013, stack line
   corrected (ML.NET/OR-Tools are in, not "later"; OR-Tools is the default
   optimizer, not the temporary `naive-cm-v1`).
7. **Tests**: `DemoHardeningTests` (unit) asserts the `DemoSeedPack`
   constants and that the four `docs/demo/*.md` files exist and the
   presenter script mentions the network map, a chart, recommendations, and
   the seed. `DemoBootstrapApiTests` (integration) asserts two independent
   bootstrap calls reach the same objective/quantities (±$0.01), matching
   the existing `ReproducibilityIntegrationTests` pattern.

## Non-goals

- Fixing the pre-existing `thin-slice`+`104729` zero-recommended-movement
  behavior on the OR-Tools optimizer (documented as a known limitation with
  a `standard-demo` workaround, not touched — would mean changing 008/009
  allocation logic, out of scope for a hardening pass).
- Any authentication/authorization system, multi-tenant isolation, or other
  production-readiness work (explicitly out of scope per spec 013).
- Rebuilding or materially changing the OpenTelemetry/Aspire wiring in
  `DairyDNA.ServiceDefaults` (only two new named spans were added, reusing
  the existing `AddSource(ApplicationName)` registration).
- Touching `DairyDNA.AppHost.csproj` (Aspire SDK version) — explicitly
  excluded from this change.
