# Implementation Plan: Supply Forecasting

**Branch**: `005-supply-forecasting` | **Date**: 2026-07-28 | **Spec**: [spec.md](./spec.md)

## Summary

Add facility/region supply forecasts (horizons 1/7/14/28) with point + interval
bands via ML.NET behind `ISupplyForecastService`, time-ordered splits, WAPE vs
seasonal-naive, versioned model metadata, and a forecast chart UI with facility picker.

## Approach

1. Domain: SupplyModelVersion, SupplyForecast, FeatureSnapshot rows.
2. `DairyDNA.Forecasting`: feature builder (no leakage), seasonal-naive baseline,
   ML.NET SDCA regression, evaluation metrics, publish forecasts.
3. API: POST train/run, GET forecasts, GET model/experiment metadata.
4. UI `/forecasts/supply`: facility list/map picker + band chart (Forecast label).
5. Tests: leakage, coverage, baseline comparison, repro seed.

## Constitution

III Deterministic demos — Pass · VIII Modular (ML behind interface) — Pass · VII Honesty — Pass
