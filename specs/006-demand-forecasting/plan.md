# Implementation Plan: Demand Forecasting

## Technical context
- .NET 10, EF Core, ML.NET SDCA regression, Blazor Interactive Server.
- Reuse Feature 005 model-version, precomputed forecast, endpoint, chart, and test patterns.

## Design
1. Aggregate order history by customer, product, and request date.
2. Build lag-7, lag-14, rolling-7, and calendar features using only rows before the feature date and as-of date.
3. Use a time-ordered 80/20 train/test split. Report WAPE, MAE, RMSE, bias, interval coverage, and lag-7 baseline WAPE.
4. Publish customer and region results for RAW_MILK and CREAM, horizons 1/7/14/28. Sparse customers use a regional mean and `ColdStart`.
5. Expose precomputed results through `/api/forecasts/demand` and show them with distinct open-order and forecast labels.

## Validation
- Unit tests cover as-of leakage filtering and forecast output.
- Integration tests train and read the demand API.
- Full solution test suite must pass.
