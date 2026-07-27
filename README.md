# DairyDNA

Decision-support and logistics optimization demo for dairy milk and cream
allocation across a synthetic U.S. dairy network.

DairyDNA forecasts supply, demand, and prices; estimates transportation cost;
and recommends the highest-value **feasible** product movements. The first
releases are demonstration-only: no real trades, no truck dispatch, and no
production-grade market advice.

## Run the thin vertical slice (000)

**Feature 000 is local-dev only, unauthenticated, and not production-secure.**

### Prerequisites

- .NET 10 SDK
- Docker Desktop (Aspire SQL Server) — or set `UseInMemoryDatabase=true` on the API

### Aspire (recommended)

```powershell
cd c:\repos\dairydna
dotnet restore DairyDNA.sln
dotnet run --project src/DairyDNA.AppHost
```

Open the Aspire dashboard, then the **web** and **api** endpoints. On the demo
home page: Generate → Load demo summary → Run optimization → View recommendations.

### API-only (in-memory DB)

```powershell
dotnet run --project src/DairyDNA.Api
# default http://localhost:5114
# GET /health
# POST /api/generation-runs
```

Web alone needs `ApiBaseUrl` pointing at the API (Aspire wires this via service discovery).

### Tests

```powershell
dotnet test DairyDNA.sln
```

### Quickstart scenarios

See `specs/000-thin-vertical-slice/quickstart.md` (A–D).

## Spec Kit

- Constitution: `.specify/memory/constitution.md`
- Program plan: `specs/000-program/program-plan.md`
- Clarifications: `specs/000-program/clarifications.md`
- **All feature specs (000–013)**: [`specs/README.md`](./specs/README.md)
- Active feature pointer: `.specify/feature.json` (currently 000)

Cursor skills: `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, `/speckit-implement`, `/speckit-converge`

## Stack (pinned)

.NET 10 · ASP.NET Core Minimal APIs · Blazor Interactive Server · Semantic UI ·
Fluxor · .NET Aspire · EF Core · SQL Server · (later) ML.NET · OR-Tools

Temporary optimizer for 000: `naive-cm-v1` — see `docs/architecture/adr-0001-naive-optimizer.md`.
