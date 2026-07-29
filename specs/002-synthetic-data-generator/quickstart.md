# Quickstart: Synthetic Data Generator (002)

Generate named profiles (`thin-slice`, `standard-demo`) or custom overrides,
inspect the validation report, then continue into demo summary / optimization.

## Prerequisites

- .NET 10 SDK
- Docker (Aspire SQL Server) **or** in-memory API for local checks
- Solution builds: `dotnet build DairyDNA.sln`

## Profiles

| Name | Farms | Facilities | Customers | Trucks | Products | History |
|------|------:|-----------:|----------:|-------:|----------|---------|
| `thin-slice` | 5 | 2 | 5 | 3 | milk+cream | ~90 days |
| `standard-demo` | 150 | 8 | 75 | 30 | 6 products | ~3 years (dense last 90d) |
| `custom` | overrides | … | … | … | optional | date range + counts |

Max bounds: farms 500, facilities 50, customers 500, trucks 200, span 1200 days.
Generator version: `synthetic-gen-v2`. Schema: `dairydna.synthetic.v2`.

## Run API (in-memory)

```powershell
cd c:\repos\dairydna
$env:UseInMemoryDatabase='true'
dotnet run --project src/DairyDNA.Api --urls http://localhost:5114
```

Or use Aspire: `dotnet run --project src/DairyDNA.AppHost`.

## Scenario A — Thin-slice + validation

1. List profiles:

```http
GET /api/generation-profiles
```

2. Generate:

```http
POST /api/generation-runs
Content-Type: application/json

{
  "profileName": "thin-slice",
  "randomSeed": 104729
}
```

Expect `202 Accepted` with `status: Completed`, `profileName`, `generatorVersion`.

3. Validation report:

```http
GET /api/generation-runs/{id}/validation-report
```

Expect `passed: true` and critical referential/invariant checks listed.

4. Demo summary / optimize as in feature 000:

```http
GET /api/demo/summary?generationId={id}
POST /api/optimization-runs
{ "generationId": "{id}" }
```

## Scenario B — Custom small profile

```http
POST /api/generation-runs
Content-Type: application/json

{
  "profileName": "custom",
  "randomSeed": 42,
  "farmCount": 3,
  "facilityCount": 2,
  "customerCount": 3,
  "truckCount": 2,
  "productSet": "standard-six",
  "startDate": "2025-12-01",
  "endDate": "2025-12-29"
}
```

Zero farms must return `400` before any entities are written.

## Scenario C — UI

1. Open Web demo home (`/`).
2. Choose profile (`thin-slice` / `standard-demo` / `custom`), set seed, Generate.
3. Optionally **Load validation report**, then Load demo summary / Run optimization.

> Prefer `thin-slice` for interview demos. `standard-demo` is slower (large history).

## Tests

```powershell
dotnet test DairyDNA.sln
```
