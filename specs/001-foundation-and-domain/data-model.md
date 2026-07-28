# Data Model: Foundation and Domain

## Entities (delta from 000)

### Contract (new)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| GenerationId | Guid | Scope |
| CustomerId | Guid | FK logical |
| ProductId | Guid | FK logical |
| StartDate, EndDate | DateOnly | End >= Start |
| MinimumQuantityPounds | decimal | >= 0 |
| MaximumQuantityPounds | decimal | >= Minimum |
| PricePerPound | decimal | >= 0 |
| ShortfallPenaltyPerPound | decimal | >= 0 |
| Active | bool | soft deactivate |

### Shipment (new — historical/planned read)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| GenerationId | Guid | |
| OriginFacilityId | Guid | |
| DestinationType | enum Facility\|Customer | |
| DestinationId | Guid | |
| ProductId | Guid | |
| QuantityPounds | decimal | > 0 |
| TruckId | Guid? | |
| DepartedAt / ArrivedAt | DateTimeOffset? | |
| Status | enum Planned\|InTransit\|Completed\|Cancelled | |

### Soft-deactivate flags
- Existing: Farm, Facility, Customer `Active`
- Add: Product, Truck `Active` (Truck.Unavailable status remains operational;
  Active=false means retired from network browse)

## Validation rules
- Positive quantities / non-negative capacities (existing)
- ExpiresAt > ProducedAt
- Contract EndDate >= StartDate; Max >= Min
- Non-empty trimmed names
- TruckCompatible for product assignment checks
- Delivery window ordering on orders
