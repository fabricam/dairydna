# Quickstart: Public Data Ingestion (003)

Import fixture dairy prices, weather, and fuel prices without live internet.

## Prerequisites

- .NET 10 SDK
- `dotnet build DairyDNA.sln`

## Run API (in-memory)

```powershell
cd c:\repos\dairydna
$env:UseInMemoryDatabase='true'
dotnet run --project src/DairyDNA.Api --urls http://localhost:5114
```

## Scenario A — Import dairy prices

```http
GET /api/import-sources
POST /api/import-runs
{ "sourceCode": "fixture-dairy-prices" }
GET /api/public/market-prices?regionCode=R1
```

Expect `dataClassification: Public` and disclaimer that data is not a DairyDNA forecast.

## Scenario B — Weather + fuel

```http
POST /api/import-runs
{ "sourceCode": "fixture-weather" }
POST /api/import-runs
{ "sourceCode": "fixture-fuel-prices" }
GET /api/public/weather?regionCode=R1
GET /api/public/fuel-prices?regionCode=R1
```

## Scenario C — Quarantine + idempotency

Re-POST the same dairy fixture → status `SkippedIdempotent`, no duplicate rows.
Malformed fixture `dairy-market-prices.malformed.json` → `Failed` + quarantine items.

## UI

Open `/imports` → Import fixture for each source.

## Tests

```powershell
dotnet test DairyDNA.sln
```
