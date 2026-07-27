# Data Model: Thin Vertical Slice (000)

**Date**: 2026-07-26  
**Schema version**: `dairydna.thin-slice.v1`

## Entities

### GenerationManifest

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | PK |
| ScenarioName | string | e.g. `thin-vertical-slice` |
| SchemaVersion | string | `dairydna.thin-slice.v1` |
| RandomSeed | int | Required; default `104729` |
| StartDate | date | Inclusive |
| EndDate | date | Inclusive; 90-day span for default |
| PlanningDate | date | Demo “today” |
| FarmCount | int | 5 |
| FacilityCount | int | 2 |
| CustomerCount | int | 5 |
| TruckCount | int | 3 |
| ConfigurationHash | string | Hash of config JSON |
| GeneratedAt | datetimeoffset | |
| Status | enum | Queued, Running, Completed, Failed |
| IsSynthetic | bool | Always true |

### Farm

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | FK |
| Name | string | Synthetic |
| RegionCode | string | |
| Latitude | decimal | |
| Longitude | decimal | |
| HerdSize | int | > 0 |
| Active | bool | |

### Facility

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| Name | string | |
| FacilityType | enum | Receiving, Separation, Storage (subset OK) |
| RegionCode | string | |
| Latitude / Longitude | decimal | |
| MilkStorageCapacityPounds | decimal | ≥ 0 |
| CreamStorageCapacityPounds | decimal | ≥ 0 |
| Active | bool | |

### Product

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| Code | string | `RAW_MILK`, `CREAM` unique |
| Name | string | |
| MaximumAgeHours | int | > 0 |
| UnitOfMeasure | string | `lb` |

### InventoryLot

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| FacilityId | uuid | |
| ProductId | uuid | |
| QuantityPounds | decimal | > 0 |
| ButterfatPercent | decimal | |
| ProducedAt | datetimeoffset | |
| ExpiresAt | datetimeoffset | MUST be > ProducedAt |
| QualityGrade | string | |
| Status | enum | Available, Allocated, Expired |
| AsOfDate | date | Snapshot day for planning |

**Validation**: Expired lots (`ExpiresAt <= asOf`) excluded from eligible supply.

### Customer

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| Name | string | Synthetic |
| RegionCode | string | |
| Latitude / Longitude | decimal | |
| Active | bool | |

### Order

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| CustomerId | uuid | |
| ProductId | uuid | |
| RequestedQuantityPounds | decimal | > 0 |
| MinimumAcceptableQuantityPounds | decimal | > 0 and ≤ Requested |
| RequestedDeliveryStart / End | datetimeoffset | End ≥ Start |
| OfferedPricePerPound | decimal | ≥ 0 |
| OrderType | enum | Spot (only type required in 000) |
| Status | enum | Open, Filled, Partial, Cancelled |
| RequestDate | date | Planning relevance |

### Truck

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| HomeFacilityId | uuid | |
| MaximumCapacityPounds | decimal | > 0 |
| CompatibleProductCodes | string[] / join | Must include product to haul |
| CostPerMile | decimal | ≥ 0 |
| CostPerHour | decimal | ≥ 0 |
| AvailableFrom / Until | datetimeoffset | |
| Status | enum | Available, Assigned, Unavailable |

### MarketPrice

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| ProductId | uuid | |
| EffectiveDate | date | |
| PricePerPound | decimal | ≥ 0 |
| PriceType | enum | StaticSpot |
| Source | string | `synthetic-static` |

### TransportCostQuote (computed / persisted with movement)

| Field | Type | Notes |
|-------|------|-------|
| DistanceMiles | decimal | |
| FuelCost | decimal(18,2) | |
| OperatingCost | decimal(18,2) | |
| TotalEstimatedCost | decimal(18,2) | |

### OptimizationRun

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| GenerationId | uuid | |
| AsOfDate | date | |
| OptimizerVersion | string | e.g. `naive-cm-v1` |
| Status | enum | Queued, Running, Feasible, Infeasible, Failed |
| ObjectiveValue | decimal(18,2) | Expected contribution margin |
| SolveDurationMilliseconds | int | |
| CreatedAt | datetimeoffset | |
| DatasetSchemaVersion | string | |

> UI may label an OptimizationRun result set as “Recommendations”; the canonical
> domain/API name is OptimizationRun.

### RecommendedMovement

| Field | Type | Notes |
|-------|------|-------|
| Id | uuid | |
| OptimizationRunId | uuid | |
| OriginFacilityId | uuid | |
| DestinationCustomerId | uuid | Customer destinations in 000 |
| ProductId | uuid | |
| QuantityPounds | decimal | > 0 |
| TruckId | uuid | |
| OrderId | uuid | nullable |
| ExpectedUnitPrice | decimal(18,2) | |
| ExpectedRevenue | decimal(18,2) | |
| TransportationCost | decimal(18,2) | |
| ExpectedContributionMargin | decimal(18,2) | MUST be ≥ 0 |
| DepartureTime / ArrivalTime | datetimeoffset | |
| Explanation | string | Human-readable factors |
| DistanceMiles | decimal | |
| FuelCost / OperatingCost | decimal(18,2) | |

### OptimizationRunResult (aggregates)

| Field | Type | Notes |
|-------|------|-------|
| UnusedInventory | list of facility/product/qty | |
| UnservedDemand | list of order/qty remaining | |

## Relationships

- GenerationManifest 1—* Farms, Facilities, Customers, Trucks, Products,
  InventoryLots, Orders, MarketPrices, OptimizationRuns
- Facility 1—* InventoryLots
- Customer 1—* Orders
- OptimizationRun 1—* RecommendedMovements

## State transitions

### OptimizationRun

`Queued → Running → Feasible | Infeasible | Failed`

Feasible may include zero movements (e.g., no demand).

### InventoryLot (planning view)

Available lots may be marked Allocated in result projections without mutating
historical snapshots incorrectly; prefer result-side unused quantities for 000.

## Invariants (must be tested)

1. Quantities and capacities are never negative.
2. `ExpiresAt > ProducedAt`.
3. Truck assignment respects capacity and compatibility.
4. Recommended quantity ≤ available inventory and ≤ remaining order demand.
5. `ExpectedContributionMargin >= 0` for every recommended movement.
6. Same seed + config → same entity counts and key aggregates.
7. Recommended arrival must fall within the order delivery window when a window is present.
8. Expired lots (`ExpiresAt <= asOf`) are never allocated.
9. Partial fills must not go below `MinimumAcceptableQuantityPounds` for a filled order line (unserved remainder is allowed).
