# DairyDNA presenter script

**Version**: 1.0 (feature 013) · **Seed pack**: [`docs/demo/seed-pack.md`](./seed-pack.md) ·
**Profile**: `thin-slice` · **Seed**: `104729`

This is the versioned walkthrough two presenters should follow to get the same
logical outcomes (spec 013 User Story 2). Every screen and number below is
labeled by its data classification:

- **Synthetic** — generated data (farms, facilities, customers, orders, prices, inventory).
- **Forecast** — ML.NET model output (supply/demand/price bands); a statistical estimate, not a guarantee.
- **Recommendation** — OR-Tools optimizer output; a *suggested* feasible allocation, not an executed trade or dispatch.

No real trades, no real truck dispatch, no production market advice. See
[`honesty-boundary.md`](./honesty-boundary.md). For a learner-oriented tour of
every page and dairy concept, see [`learning-guide.md`](./learning-guide.md).

## 0. Bring up the demo

One command (see [`scripts/demo-start.ps1`](../../scripts/demo-start.ps1) for details):

```powershell
dotnet run --project src/DairyDNA.AppHost
```

Open the Aspire dashboard link printed in the console, then open the **web**
resource endpoint. If Docker isn't running, see **Troubleshooting** below for
the in-memory fallback.

## 1. Demo home (`/demo`) — generate the seed pack **[Synthetic]**

1. Leave **Profile** = `thin-slice`, **Seed** = `104729` (the defaults already
   match the seed pack).
2. Click **Generate synthetic data**. Expect: a generation id, status
   `Completed`, and a planning date near the end of the `2025-10-01`–
   `2025-12-29` window — sub-second on the reference machine (§ performance
   budgets in `hardening-notes.md`).
3. Click **Load demo summary**. Expect: inventory/demand/fleet counts and the
   **network map** — an interactive Google map of farms, facilities, and
   customers (labeled **Synthetic**). This is the map required by FR-005a;
   call out that markers use generated coordinates and recommended lanes are
   planning flows, not literal GPS routes.
4. *Talking point*: "Every number on this screen came from a seeded random
   generator — there is no live market data yet at this stage."

Alternative one-shot path for CI or a rushed demo: `POST /api/demo/bootstrap`
runs generate → flagship scenario pack → one optimize in a single call (see
seed-pack.md). The step-by-step UI walkthrough above is still what you should
present live.

## 2. Dashboard (`/dashboard`) — a chart, not just tables **[Synthetic]**

1. Open **Dashboard** from the nav and paste/pick the generation id.
2. Point at the **Inventory age / risk** chart (`InventoryAgeChart`) — a
   bucketed bar chart of lot age bands with risk coloring. This is the
   "chart" required by FR-005a.
3. *Talking point*: "Aging inventory close to expiry is what the optimizer is
   racing against — that's the pressure driving the recommendations you'll
   see next."
4. If forecasts were trained (optional, see `/forecasts/*`), point out the
   **forecast band chart** (`ForecastBandChart`, labeled **Forecast**) as a
   second valid chart choice.

## 3. Optimize and recommendations (`/recommendations`) — **[Recommendation]**

1. Back on `/demo`, click **Run optimization**. Expect: a `Feasible` (or
   `Infeasible`, rarer) status within the ≤30s budget — typically under a
   second.
2. Open **View recommendations**. Point at:
   - The **recommended flow arcs** overlaid on the same network map.
   - The **margin and cost breakdown** chart (`MarginCostChart`) — grouped
     bars of revenue / transport cost / contribution margin per movement.
   - The movement table underneath (accessible text alternative for the
     charts is present for screen readers).
3. *Talking point*: "Every row here is a **recommendation** — a suggested
   truck-and-product movement the optimizer believes is feasible and
   profitable. Nothing has been dispatched."
4. **Known limitation — check before presenting live**: the default
   `thin-slice` profile with seed `104729` can return zero movements
   (`Feasible`, but nothing to move) on this small 5-farm/2-facility/3-truck
   network — see `hardening-notes.md`. If recommendations look empty,
   generate a `standard-demo` profile dataset instead (same seed; still well
   under the performance budget) — it reliably produces movements — or be
   upfront that this particular combination has no profitable moves today.

## 4. Scenarios (`/scenarios`) — what-if comparisons **[Recommendation]**

1. Apply the flagship pack (`diesel-rise`, `distant-high-price`,
   `capacity-loss`, `demand-spike`) if not already applied.
2. Run `diesel-rise` and compare against the base run. Expect: a different
   objective and at least one changed movement; the compare view calls out
   whether the scenario run is still "recommended" (feasible) or not.
3. *Talking point*: "This is the core interview story: change one real-world
   condition, get a different feasible plan in seconds — not a re-run of a
   multi-hour batch job."

## 5. Replay (`/replay`) — regret vs. simple baselines **[Recommendation]**

1. Open `/replay`, load the generation id, and run a replay for a day inside
   the dataset window.
2. Build a regret report for a short window (e.g., the first 3–7 days).
   Expect the **grouped-bar regret chart** (optimizer vs.
   `NearestCustomerGreedy` vs. `HighestPriceFirst`) plus a per-day
   win/lose table.
3. *Talking point*: "This is an honesty check on the optimizer itself —
   we compare it to two dumb-but-plausible baselines using the same
   synthetic inputs, not a claim about real historical outcomes."

## 6. Models (`/models`) — governance **[Forecast / Recommendation]**

Optional, if time allows: show model versions, published/retired status, and
that the optimizer/costing versions are stamped on every run
(reproducibility + governance, spec 011).

## Expected outcomes (qualitative)

- Generate completes in well under the 2s dashboard budget (typically
  <500ms measured locally; see `hardening-notes.md`).
- Optimize returns `Feasible` well under the 30s budget. The `thin-slice`
  profile + seed `104729` currently returns zero movements (a known
  limitation, see step 3 above); `standard-demo` reliably returns non-zero
  movements and a positive objective.
- Running generate → optimize twice with the same seed produces the same
  objective and movement quantities (±$0.01) — this is asserted by
  automated tests (`ReproducibilityIntegrationTests`,
  `DemoBootstrapApiTests`), not just eyeballed.
- Scenario runs change the objective in the expected direction (e.g.,
  `diesel-rise` increases transportation cost, lowering margin).

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Aspire fails to start SQL Server | Docker Desktop not running | Start Docker Desktop, or run the API alone with `UseInMemoryDatabase=true` (see `scripts/demo-start.ps1`). |
| Port already in use (5114/5152/7122/7032/15110/17266) | Another DairyDNA instance or unrelated app | Stop the other process, or edit the relevant `Properties/launchSettings.json` `applicationUrl` to a free port. |
| Build fails with a locked `.dll`/`.exe` | A previous `dotnet run`/IDE debug session is still holding the file | Stop all running `dotnet`/IIS Express processes (Task Manager or `taskkill`), then rebuild. |
| `/health` doesn't return healthy | API still starting, or DB migration/seed step failed | Wait a few seconds and retry; check the Aspire dashboard logs for the `api` resource. |
| Generate/optimize looks "stuck" | Usually just a cold JIT/first-request hit | Should still complete well inside the 30s optimize budget; if not, check Aspire traces (`hardening-notes.md`). |
| No public data available (offline) | Public ingestion source unreachable | Demo does not depend on live ingestion — use `thin-slice`/`standard-demo` synthetic profiles, which work fully offline. |

## Accessibility notes

- Primary demo buttons (Generate, Load demo summary, Run optimization) are
  reachable by keyboard (standard `<button>`/`<a>` elements, tab order
  follows the page).
- Every chart/map component ships a text-alternative summary
  (`AriaLabel`/`TextAlternative`) so the same information is available
  without relying on the visual rendering.
