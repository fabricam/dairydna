# Implementation Plan: Operational Dashboard

**Branch**: `004-operational-dashboard` | **Date**: 2026-07-28 | **Spec**: [spec.md](./spec.md)

## Summary

Ship an ops dashboard for a selected generation + as-of date with network map,
inventory age/risk chart, demand/fleet summaries, and price time-series —
labeled Synthetic/Public. Fluxor + Semantic UI; no forecasting/OR-Tools required.

## Approach

1. `GET /api/dashboard` read model (context, panels, chart series, map points).
2. Blazor `/dashboard` page with Fluxor context + independent panel loading.
3. Reuse `NetworkMap`; add inventory age histogram + price sparkline SVG charts.
4. Facility drill-down retaining generation/as-of query context.
5. Empty/error states for missing dataset; tests + quickstart.

## Constitution

VII Honesty labels — Pass · XII ≤2s useful view (summaries) — Pass · FR-006 no ML — Pass
