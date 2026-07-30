# Quickstart: Movement Optimization (009)

## Default OR-Tools optimize

```http
POST /api/generation-runs
{ "profileName": "thin-slice", "randomSeed": 104729 }

POST /api/optimization-runs
{
  "generationId": "{id}",
  "priceMode": "Spot",
  "safetyStockEnabled": true
}
```

Expect `optimizerVersion: ortools-cm-v1`.

## Compare with naive

```http
POST /api/optimization-runs
{ "generationId": "{id}", "optimizerVersion": "naive-cm-v1" }
```

## UI

Generate → Run optimization → `/recommendations` shows map arcs + margin chart.

## Tests

```powershell
dotnet test DairyDNA.sln --filter FullyQualifiedName~OrTools
```
