# Implementation Plan: Transportation Costing

**Status**: Implemented  
**Feature**: 008 Transportation Costing

## Technical approach

- Extend the application transport port with a request DTO and versioned
  breakdown while preserving the legacy positional `Calculate` overload used
  by the naive optimizer.
- Calculate a deterministic Haversine one-way distance. Bill two legs by
  default for the empty-return approximation.
- Price fuel as `billedMiles / mpg * fuelPricePerGallon`; calculate operating
  cost from billed miles plus one-way travel time and one load/unload hour.
- Round all monetary values with `DomainInvariants.Money`.
- Expose a minimal API at `POST /api/transport-cost` and document inputs and
  defaults at `GET /api/transport-cost/assumptions`.

## Validation

- Coordinates must be in valid latitude/longitude ranges and neither endpoint
  may be the missing `(0, 0)` sentinel.
- Rates and fuel price cannot be negative; mpg and quantity must be positive.
- If compatible product codes are supplied, the requested product code must
  appear in that comma-separated list.

## Testing

- Unit tests cover five known lanes, deterministic repeats, invalid
  coordinates, fuel sensitivity, empty-return behavior, and compatibility.
- Full solution tests are the release validation command.
