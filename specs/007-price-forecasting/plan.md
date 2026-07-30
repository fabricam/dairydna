# Implementation Plan: Price Forecasting

## Technical context
- .NET 10, EF Core, ML.NET SDCA regression, Blazor Interactive Server.
- Mirrors Features 005/006, using precomputed versioned forecasts and `ForecastBandChart`.

## Design
1. Merge synthetic `MarketPrices` with available public prices by product, region, and date. Synthetic observations are projected to active demo regions.
2. Build lag-1, lag-7, rolling-7, and day-of-year features from observations known at or before the as-of date. Split history chronologically (80/20).
3. Evaluate ML predictions against a last-price baseline with WAPE, MAE, RMSE, bias, and interval coverage. Clamp negative predictions to zero.
4. Publish point/lower/upper price forecasts for every observed active product and region at 1/7/14/28 day horizons.
5. Expose forecasts, actuals, model metadata, and an optimization price bundle. Mark every response as `Forecast` and explicitly disclaim trade-quote semantics.

## Validation
- Unit tests cover as-of leakage and non-negative forecast/bundle output.
- Integration test trains and retrieves forecast and optimization bundle endpoints.
- `dotnet test DairyDNA.sln` passes.
