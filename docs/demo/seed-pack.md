# DemoSeedPack

**Versioned demo dataset/scenario combination** for feature 013 (Demo
Hardening), so any two presenters — or CI — reach the same logical outcomes.
The source of truth is the `DemoSeedPack` static class at
[`src/DairyDNA.Application/Demo/DemoSeedPack.cs`](../../src/DairyDNA.Application/Demo/DemoSeedPack.cs);
this doc must stay in sync with it (`DemoHardeningTests` asserts the values below).

## Fixed inputs

| Field | Value | Notes |
|---|---|---|
| Profile | `thin-slice` | 5 farms / 2 facilities / 5 customers / 3 trucks, milk+cream, `2025-10-01`–`2025-12-29` (dense, ~90 days). |
| Random seed | `104729` | Default seed used by the Demo Home page, `ThinSliceGenerationRequest`, and `SyntheticGenerationRequest` when no seed is supplied. |
| Data classification | `Synthetic` | Stamped on demo summary / optimization run / replay responses. |

## Flagship scenario pack

Applied via `POST /api/scenarios/flagship-pack?generationId={id}` (spec 010),
in this order:

1. **`diesel-rise`** — diesel price rises to \$5.25/gal (`FuelPricePerGallon`).
2. **`distant-high-price`** — distant customers get a \$0.18/lb price bump (`DistantCustomerPriceBump`).
3. **`capacity-loss`** — available inventory reduced 25% (`CapacityScaleFactor = 0.75`).
4. **`demand-spike`** — open demand increases 30% (`DemandScaleFactor = 1.30`).

## Key URLs (Aspire `web` resource / API-only default ports)

| Page | Route |
|---|---|
| Demo home (generate, summary, optimize) | `/demo` (also served at `/`) |
| Dashboard (inventory age chart) | `/dashboard` |
| Network map (standalone) | `/network` |
| Recommendations (margin/cost chart, flow arcs) | `/recommendations` |
| Scenarios (flagship pack, compare) | `/scenarios` |
| Replay (regret report) | `/replay` |
| Model governance | `/models` |

Default standalone (non-Aspire) ports: API `http://localhost:5114`, Web
`http://localhost:5152`. Aspire assigns its own ports per run; use the URLs
printed by `dotnet run --project src/DairyDNA.AppHost` or shown in the Aspire
dashboard.

## One-shot bootstrap (optional, convenience only)

`POST /api/demo/bootstrap` (no body required) runs generate (`thin-slice`,
seed `104729`) → apply flagship pack → one optimization run, and returns the
generation id, optimization run id, objective value, and the flagship
scenario names. It exists for CI/dual-run determinism checks and rushed
demos; the presenter script above still walks the UI step by step because
that is what audiences should see. Running it twice must reach the same
objective and quantities (±\$0.01) — see
`tests/DairyDNA.IntegrationTests/DemoBootstrapApiTests.cs`. Note: with the
default `thin-slice`+`104729` combination, `objectiveValue`/movement count
are currently `0` (a known limitation, see `hardening-notes.md`) — the
determinism assertion (both runs equal) still holds either way.

## Honesty

Every artifact this seed pack produces is **synthetic**: generated farms,
facilities, customers, orders, prices, and inventory. Forecasts are
statistical estimates from ML.NET models trained on that synthetic history.
Recommendations are OR-Tools optimizer output — a *suggested* feasible
allocation, never an executed trade or truck dispatch. See
[`honesty-boundary.md`](./honesty-boundary.md) for the full boundary
statement.
