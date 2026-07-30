# Demo hardening notes (feature 013)

Refresh of `docs/architecture/demo-000-notes.md` against the current codebase
(features 000–012 implemented; OR-Tools is now the default optimizer).
Measurements below were taken with `dotnet run --project src/DairyDNA.Api
--no-build` (`UseInMemoryDatabase=true`) on the reference machine.

## Reference machine profile

| Item | Value |
|---|---|
| OS | Windows 10/11 (win32 10.0.26200) |
| .NET SDK | 10.0.202 (repo also has 8.0.423 / 9.0.316 / 10.0.110 installed side by side) |
| Docker Desktop | Required for Aspire's SQL Server resource; not required for the API-only `UseInMemoryDatabase=true` path |
| Shell | PowerShell |
| Ports (standalone, non-Aspire) | API `http://localhost:5114` (`https://localhost:7122`); Web `http://localhost:5152` (`https://localhost:7032`) |
| Ports (Aspire AppHost) | Dashboard `http://localhost:15110` / `https://localhost:17266`; `api`/`web` resource endpoints are assigned per run — use the URLs Aspire prints |

## Performance budgets (SC-003)

Measured 2026-07-29 against an in-memory database (no SQL Server round-trip),
which is representative of the demo path since Aspire also seeds a fresh
container each run. All figures are single-request wall-clock time
(PowerShell `Measure-Command`), first request after process start ("cold")
vs. a subsequent request ("warm", JIT/query plans cached):

| Step | Budget | `thin-slice` (cold / warm) | `standard-demo` (cold / warm) |
|---|---|---|---|
| `GET /health` | n/a | 251 ms / — | — |
| `POST /api/generation-runs` (generate) | dashboard-adjacent, ≤2s | 309 ms / 70 ms | 197 ms |
| `GET /api/demo/summary` (dashboard content) | **≤2s** | 127 ms / — | 32 ms |
| `POST /api/optimization-runs` (optimize) | **≤30s** | 201 ms / 3 ms | 35 ms |
| `POST /api/demo/bootstrap` (generate + flagship pack + optimize) | ≤30s | 95 ms | — |

Both budgets have wide headroom (roughly two orders of magnitude under
budget) even on the larger `standard-demo` profile (150 farms / 8 facilities
/ 75 customers / 30 trucks, ~3 years of history). Re-measure with `dotnet
test tests/DairyDNA.IntegrationTests` (the reproducibility/happy-path tests
exercise the same code paths) or `scripts/demo-smoke.ps1` if hardware differs
materially from the above.

## Known limitations

- **`thin-slice` + seed `104729` can return zero recommended movements.**
  The OR-Tools optimizer (`ortools-cm-v1`, default since 009) returns
  `Feasible` with `movements: []` for the default `thin-slice` profile and
  seed across the entire `2025-10-01`–`2025-12-29` window, and for all four
  flagship scenario overrides. This is a pre-existing characteristic of the
  small thin-slice network (5 farms / 2 facilities / 5 customers / 3 trucks)
  colliding with truck/time-window feasibility — not something introduced by
  feature 013, and it is not a crash (`unusedInventory`/`unservedDemand` are
  populated normally; `tests/DairyDNA.IntegrationTests/ApiTests.cs`
  `ThinSliceHappyPathTests` already tolerates empty movements with
  `if (opt.movements is { Count: > 0 })`). **If recommendations look empty
  while presenting live, generate a `standard-demo` profile dataset instead**
  — the same seed (`104729`) reliably produces 5 movements and a ~$2,308.65
  objective on `standard-demo`, with generate/optimize still well under
  budget (see table above). This is not fixed here per feature 013's "avoid
  overbuilding" scope (it would mean changing 008/009 allocation logic); it
  is called out so presenters aren't surprised.
- **Local-dev auth posture: open, unauthenticated.** There is no login, no
  API key, no tenant isolation. Every endpoint is reachable by anyone who can
  reach the process. This is acceptable for a local/interview demo only —
  **do not deploy this as-is to any shared or public environment.**
- **R1 logistics simplifications** (carried from 008/009, restated here for
  visibility):
  - Trucks are treated as a shared capacity/time-window pool, not
    individually dispatched or routed (no multi-stop routing, no driver
    hours-of-service modeling).
  - Transportation cost is a straight-line-distance + fuel/operating-cost
    model (`TransportCostCalculator`), not a real carrier rate card or road
    network.
  - Safety stock is a flat 5% haircut on available inventory
    (`SafetyStockEnabled`), not a per-product/per-facility policy.
  - Replay "regret" baselines (012) ignore truck capacity/time-window
    feasibility entirely — they are proxy economics for comparison, not
    alternate feasible optimizers.
- **Public data ingestion (003) is optional.** The demo does not depend on
  live/public sources being reachable; `thin-slice`/`standard-demo` synthetic
  profiles work fully offline.
- **Forecast-dependent price modes degrade to Spot.** If no price model has
  been trained yet for a generation, `PriceMode=ForecastPoint/Lower/Upper`
  silently falls back to the orders' existing offered prices (see
  `CreateOptimizationRunHandler.ApplyForecastPricesAsync`) rather than
  failing — label any forecast-priced screen accordingly.

## Aspire / OpenTelemetry

`DairyDNA.ServiceDefaults` (`src/DairyDNA.ServiceDefaults/Extensions.cs`,
unchanged by feature 013) already wires OpenTelemetry tracing + metrics +
health checks for both `api` and `web` via `AddServiceDefaults()`:
ASP.NET Core, HttpClient, and runtime instrumentation, plus an OTLP exporter
when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (Aspire sets this automatically for
projects it hosts). `/health` and `/alive` are excluded from tracing noise.

Feature 013 adds two thin, explicitly named spans (no new telemetry stack) so
generate/optimize show up distinctly in the Aspire dashboard's trace view:

- `DairyDNA.Generate` — wraps `CreateGenerationRunHandler.HandleAsync`
  (tags: profile, seed, resulting generation id/status).
- `DairyDNA.Optimize` — wraps `CreateOptimizationRunHandler.HandleAsync`
  (tags: generation id, price mode, optimizer version, status, objective
  value, movement count).
- `DairyDNA.Demo.Bootstrap` — wraps the optional one-shot bootstrap endpoint.

Both use a shared `ActivitySource` named `DairyDNA.Api`
(`DairyDNA.Application.Diagnostics.DairyDnaTelemetry`), which matches the API
host's `IHostEnvironment.ApplicationName` — the same name
`ConfigureOpenTelemetry` already passes to `tracing.AddSource(...)`, so no
`ServiceDefaults` changes were needed to pick these spans up.

**To see traces**: run via Aspire (`dotnet run --project src/DairyDNA.AppHost`),
open the dashboard link printed in the console, select the `api` resource,
open **Traces**, then exercise Generate/Optimize from the Web UI (or hit the
endpoints directly) and look for `DairyDNA.Generate` / `DairyDNA.Optimize` /
`DairyDNA.Demo.Bootstrap` spans with the tags above.

## Security / hygiene

- No secrets are committed (`.gitignore` already excludes `*.env`,
  `*.pfx`, `*.publishsettings`, `*.user`, publish profiles, etc.);
  `scripts/ci-checks.ps1` adds a lightweight scan over tracked files for
  common secret patterns.
- `/health` (and Aspire's `/alive`) require no auth by design — they are
  liveness/readiness signals, not sensitive data.
- Demo POST bodies are validated by existing handlers (profile name/limits in
  `GenerationProfileCatalog`, scenario override range checks in
  `CreateOptimizationRunHandler.ValidateOverrides`); invalid input returns
  `400`/`ValidationProblem`, not a crash.
