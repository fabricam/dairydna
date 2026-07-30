# Price Forecasting Quickstart

1. Create a synthetic generation through the Demo page or `POST /api/generation-runs`.
2. Train prices with `POST /api/forecasts/price/runs` and a `generationId`.
3. Retrieve market price forecasts with `GET /api/forecasts/price?generationId={id}&productCode=RAW_MILK`.
4. Retrieve actual synthetic price history with `GET /api/forecasts/price/actuals?generationId={id}&productCode=RAW_MILK`.
5. Retrieve point/lower/upper prices for optimization with `GET /api/forecasts/price/optimization-bundle?generationId={id}&asOfDate={yyyy-MM-dd}`.
6. Open `/forecasts/price`, train a selected generation, and select RAW_MILK or CREAM.

Forecasts cover active products at the region grain for horizons 1, 7, 14, and 28 days. They are estimates with uncertainty bounds, not executable trade quotes or guaranteed clearing prices.
