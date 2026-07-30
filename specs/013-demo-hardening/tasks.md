# Tasks: Demo Hardening

- [X] T001 Add `DemoSeedPack` static class (`Application/Demo`) with the
      versioned profile/seed/flagship-scenario/route/doc-path constants.
- [X] T002 Add `DairyDnaTelemetry` shared `ActivitySource` and thin
      `DairyDNA.Generate` / `DairyDNA.Optimize` spans in
      `CreateGenerationRunHandler` / `CreateOptimizationRunHandler`.
- [X] T003 Add `DemoBootstrapHandler` (generate → flagship pack → optimize)
      and `POST /api/demo/bootstrap`, registered in `Program.cs`.
- [X] T004 Write `docs/demo/presenter-script.md` (labeled, versioned,
      exercises network map + a chart + recommendations; troubleshooting).
- [X] T005 Write `docs/demo/seed-pack.md` (DemoSeedPack values, flagship pack,
      key URLs, honesty note).
- [X] T006 Write `docs/demo/hardening-notes.md` (reference machine profile,
      freshly measured performance budgets, known limitations, R1 logistics
      simplifications, Aspire/OTel notes, security/hygiene).
- [X] T007 Write `docs/demo/honesty-boundary.md`.
- [X] T008 Write `scripts/demo-start.ps1` (one-command bring-up + `-InMemory`
      fallback + troubleshooting) and `scripts/demo-smoke.ps1` (`/health` +
      optional bootstrap check).
- [X] T009 Write `scripts/ci-checks.ps1` (build+test, secret scan, health-test
      presence check) and `.github/workflows/ci.yml`.
- [X] T010 Update root `README.md` (honesty boundary up front, one-command
      demo section, doc links, active-feature pointer, stack/optimizer fix).
- [X] T011 Add `DemoHardeningTests` (unit: seed pack constants + doc files
      exist + presenter script content) and `DemoBootstrapApiTests`
      (integration: dual-bootstrap determinism).
- [X] T012 Update `specs/013-demo-hardening/{plan,tasks,quickstart}.md`,
      `specs/README.md` status row, and `.specify/feature.json`.
