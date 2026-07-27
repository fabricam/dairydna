# DairyDNA — Resolved Clarifications

**Status**: Program defaults for demo v1  
**Date**: 2026-07-26  
**Rule**: Features may tighten these defaults but MUST NOT silently contradict
them. Changes require an amendment note in the feature `spec.md`.

These close the open `/speckit.clarify` questions from the original plan so
implementation can proceed without blocking debates.

---

## Product

| Question | Decision |
|----------|----------|
| Default planning horizon | **24 hours** |
| Decision cadence | **Daily** (one planning run per demo “day”) |
| Independently movable products (R1) | **Raw milk** and **Cream** only |
| Quantity units | **Pounds** (single unit of measure in v1) |
| Contract commitments | **Soft**: shortfall allowed with explicit penalty cost in objective; **hard** maximum acceptance |
| Partial order fulfillment | **Yes**, down to `MinimumAcceptableQuantity` |
| Split loads across trucks | **Yes** |
| Product blending | **No** in R1–R2 |
| Processing transforms inventory | **No** until Optimization Release 3 |
| Processing loss | Deferred with transforms; when added, use explicit yield coefficients + loss factor |

---

## Forecasting

| Question | Decision |
|----------|----------|
| Geographic aggregation | Farm → facility → region rollups; serve facility and region first |
| Primary horizons | **1, 7, 14, 28** days; seven-day is acceptance gate |
| Model acceptance metric | **WAPE** primary; also MAE, RMSE, bias, interval coverage |
| Sparse / new customers | Segment + region forecast, allocated by profile weights; mark **cold-start** |
| Future-known values allowed | Calendar, known contract minimums, announced outages entered before as-of time |
| Uncertainty representation | Point estimate + lower/upper bounds; optimization can select point / conservative / optimistic / user scenario |
| Baseline to beat | Seasonal-naive for supply; for demand use seasonal-naive or same-day-previous-week as documented per feature |

### Supply success target (default synthetic scenario)

- Beat seasonal-naive WAPE by ≥ **10%** on 7-day horizon
- Aggregate bias within **±5%**
- Forecast coverage ≥ **99%** of active facilities

Refine after baseline experiments; do not invent success after the fact.

---

## Optimization

| Question | Decision |
|----------|----------|
| Primary objective | **Maximize expected contribution margin** |
| Customer service vs profit | Contract shortfalls penalized; no separate hard “serve everyone” rule in R1 |
| Safety stock | Optional flag on optimization run; default **on** for demo profile |
| Unserved contract demand | Allowed with penalty (see product table) |
| Truck routes | **Single-leg** origin→destination in R1 |
| Empty-return cost | **Included** in transport cost |
| Processing in first optimizer | **No** |
| Planning model | **Single-day / single-period** in R1; multi-period in R2 |

### Price inputs for optimization

Supports: forecast point, lower bound, upper bound, user scenario price.

---

## Demonstration

| Question | Decision |
|----------|----------|
| Flagship scenario | Cream excess + distant high price vs nearby contract penalty + heat + demand spike + diesel + plant capacity loss + expiring cream |
| Reproducible interview result | Same seed/config → same recommendation economics within documented tolerance |
| Workstation scale | Thin slice always; standard 3-year demo documented with machine profile |
| Required visuals | **Network map** (farms/facilities/customers); **inventory age/risk chart**; **forecast band time-series**; **recommendation cost/margin bars**; **scenario compare charts**; optional flow arcs on map — built with **Blazor + Semantic UI + Fluxor** (see `specs/_visual-aids.md`) |
| Intentional limitations to show | No auto-dispatch; forecasts ≠ guaranteed prices; R1 logistics simplifications labeled |
| Map fidelity | Schematic regional plot from lat/lon is sufficient; commercial map API not required for demo v1 |

---

## Amendment Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-07-26 | Initial defaults | Close original clarify backlog for DairyDNA |
| 2026-07-26 | Visual aids: maps + charts required | Prefer spatial/temporal visuals over tables-only UI |
