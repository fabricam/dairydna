# Demand Forecasting Quickstart

1. Create a synthetic generation through the Demo page or `POST /api/generation-runs`.
2. Train forecasts with `POST /api/forecasts/demand/runs` and a `generationId`.
3. Query customer forecasts with `GET /api/forecasts/demand?generationId={id}&customerId={customerId}`.
4. Query customer order history with `GET /api/forecasts/demand/actuals?generationId={id}&customerId={customerId}`.
5. Open `/forecasts/demand`, train the selected generation, and select a customer. The chart labels open orders separately from forecast bands.

Forecasts cover RAW_MILK and CREAM at customer and region aggregation levels, for 1, 7, 14, and 28 day horizons. Sparse customers are explicitly marked `coldStart`.
