# Quickstart: Operational Dashboard (004)

View network map, inventory age/risk, prices, demand, and fleet for a generation.

## Prerequisites

Generate a thin-slice dataset (002) first.

## Scenario A — API

```http
POST /api/generation-runs
{ "profileName": "thin-slice", "randomSeed": 104729 }

GET /api/dashboard?generationId={id}
GET /api/dashboard/facilities/{facilityId}?generationId={id}&asOfDate={planningDate}
```

Expect `dataClassification: Synthetic`, `network`, `inventoryAgeRisk`, `priceSeries`.

Unknown id → `404` with explicit error (not blank zeros).

## Scenario B — UI

1. Open `/dashboard`.
2. Select a recent generation (or paste id) → Load.
3. Confirm map + age chart + price sparkline with Synthetic labels.
4. Click a facility → detail retains generation/as-of; Back returns to dashboard.

## Performance note

Thin-slice useful content loads via aggregated summaries (target ≤2s on reference demo machine).

## Tests

```powershell
dotnet test DairyDNA.sln --filter FullyQualifiedName~Dashboard
```
