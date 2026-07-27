# Quickstart: Thin Vertical Slice (000)

Validate that DairyDNA can generate thin-slice data, show a demo summary, and
produce a feasible (or explicitly infeasible) allocation recommendation.

## Prerequisites

- .NET 10 SDK
- Docker (for Aspire SQL Server) **or** local SQL Server / LocalDB
- Repository at `c:\repos\dairydna` with solution built

## Setup

```powershell
cd c:\repos\dairydna
dotnet restore DairyDNA.sln
dotnet build DairyDNA.sln
```

Start with Aspire AppHost (exact project path after scaffolding):

```powershell
dotnet run --project src/DairyDNA.AppHost
```

Note the API and Web endpoints from the Aspire dashboard.

Without Aspire (in-memory API):

```powershell
dotnet run --project src/DairyDNA.Api
# http://localhost:5114 — GET /health, POST /api/generation-runs
```

## Scenario A — Happy path allocation

1. **Generate** thin-slice data:

```http
POST /api/generation-runs
Content-Type: application/json

{
  "scenarioName": "thin-vertical-slice",
  "schemaVersion": "dairydna.thin-slice.v1",
  "randomSeed": 104729,
  "startDate": "2025-10-01",
  "endDate": "2025-12-29",
  "farmCount": 5,
  "facilityCount": 2,
  "customerCount": 5,
  "truckCount": 3
}
```

2. Wait until status is `Completed`. Record `id` and `planningDate`.

3. **Open demo home** (Web UI) or call:

```http
GET /api/demo/summary?generationId={id}
```

**Expected**: Inventory, demand, static prices, and fleet rows; data marked
Synthetic.

4. **Optimize**:

```http
POST /api/optimization-runs
Content-Type: application/json

{
  "generationId": "{id}",
  "optimizerVersion": "naive-cm-v1"
}
```

**Expected**: `status` is `Feasible` or `Infeasible`. If `Feasible` with
movements, each movement shows revenue, transportation cost, and
`expectedContributionMargin >= 0`. UI recommendation table matches API.

## Scenario B — Reproducibility

1. Run generation twice with the same body (seed `104729`).
2. Compare entity counts and key aggregates — must match exactly.
3. Run optimization twice on the same generation (or two equivalent
   generations).
4. **Expected**: Identical objective and recommended quantities; any compared
   cost fields differ by at most `0.01`.

## Scenario C — Known-answer tests (automated)

```powershell
dotnet test tests/DairyDNA.Optimization.Tests
```

**Expected** fixtures pass:

1. One origin / one customer → ships available quantity up to demand if margin > 0
2. Higher distant price loses to nearer lower price after transport
3. Insufficient truck capacity → feasible plan with unused inventory / unserved demand
4. Negative margin only option → zero movements, unused inventory reported
5. Expired inventory excluded from supply
6. Zero demand → no movements, full unused inventory

## Scenario D — Health

```http
GET /health
```

**Expected**: `Healthy` when API and database are up.

## Contracts & model references

- API: [contracts/openapi.yaml](./contracts/openapi.yaml)
- Entities: [data-model.md](./data-model.md)
- Decisions: [research.md](./research.md)

## Non-goals for this quickstart

- Authentication
- Public data import
- ML forecasting
- OR-Tools / Feature 009 optimizer
- Contract penalties / safety stock
