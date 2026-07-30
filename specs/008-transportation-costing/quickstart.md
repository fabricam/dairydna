# Transportation Costing Quickstart

## Estimate a lane

```http
POST /api/transport-cost
Content-Type: application/json

{
  "originLat": 43.0,
  "originLon": -89.0,
  "destLat": 43.5,
  "destLon": -88.5,
  "costPerMile": 1.50,
  "costPerHour": 60.00,
  "quantityPounds": 10000,
  "productCode": "RAW_MILK",
  "compatibleProductCodes": "RAW_MILK,CREAM"
}
```

The response identifies `transport-cost-v2`, reports one-way and billed miles,
and returns fuel, operating, and total estimated costs rounded to cents.

## Override fuel or return policy

Set `fuelPricePerGallon`, `mpg`, or `includeEmptyReturn`. Defaults are $3.50
per gallon, 6.5 mpg, and `true` respectively.

## Review model assumptions

```http
GET /api/transport-cost/assumptions
```

The endpoint returns the version and fixed assumptions: 45 mph average speed,
1.0 hour load/unload, and round-trip billing when the empty return is included.
