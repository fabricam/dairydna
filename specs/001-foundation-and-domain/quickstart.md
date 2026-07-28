# Quickstart: Foundation and Domain (001)

## Prerequisites

.NET 10 SDK; Docker optional (Aspire) or API `UseInMemoryDatabase=true`.

```powershell
cd c:\repos\dairydna
dotnet build DairyDNA.sln
dotnet run --project src/DairyDNA.AppHost
# or: dotnet run --project src/DairyDNA.Api
```

## Scenario A — Browse after generate

1. Open Web UI → **Generate thin-slice data** (creates generation).
2. Open **Network** (`/network`) — map shows farms, facilities, customers.
3. Open **Reference data** (`/reference`) — list farms/facilities/customers/products/trucks/contracts.
4. Soft-deactivate a customer → default list hides it; detail by id still works.

## Scenario B — Invalid create rejected

```http
POST /api/facilities?generationId={id}
{ "name": "Bad", "facilityType": "Storage", "milkStorageCapacityPounds": -1, ... }
```

Expected: 400 Problem Details; no row persisted.

## Scenario C — Health

`GET /health` → Healthy.
