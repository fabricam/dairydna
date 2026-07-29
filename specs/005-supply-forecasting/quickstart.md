# Quickstart: Supply Forecasting (005)

Train ML.NET supply forecasts on a thin-slice generation and inspect bands.

## Prerequisites

- Feature 002 dataset (thin-slice)
- Optional: 003 weather fixtures for heat-stress features

## Scenario A — Train & query

```http
POST /api/generation-runs
{ "profileName": "thin-slice", "randomSeed": 104729 }

POST /api/forecasts/supply/runs
{ "generationId": "{id}", "randomSeed": 104729 }

GET /api/forecasts/supply/models/latest?generationId={id}
GET /api/forecasts/supply?generationId={id}
GET /api/forecasts/supply?generationId={id}&facilityId={facilityId}
```

Expect `dataClassification: Forecast`, horizons 1/7/14/28, point + lower/upper.
Metrics include WAPE vs seasonal-naive; below-bar runs are labeled explicitly.

## Scenario B — UI

Open `/forecasts/supply` → select generation → Train & publish → pick facility on map → view band chart (Actual vs Forecast).

## Tests

```powershell
dotnet test DairyDNA.sln --filter FullyQualifiedName~Forecast
```
