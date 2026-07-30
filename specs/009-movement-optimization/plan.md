# Implementation Plan: Movement Optimization (OR-Tools)

**Branch**: `009-movement-optimization` | **Date**: 2026-07-29 | **Spec**: [spec.md](./spec.md)

## Summary

Replace naive greedy as the default allocator with Google OR-Tools CBC MIP
(`ortools-cm-v1`) behind `IAllocationOptimizer`, keep `naive-cm-v1` selectable,
add independent feasibility validation, price-mode hooks, and safety-stock option.

## Approach

1. `OrToolsContributionMarginOptimizer` — MIP over feasible lanes (qty + use).
2. `AllocationOptimizerResolver` — default OR-Tools; `naive-cm-v1` explicit.
3. FeasibilityValidator after solve.
4. Optimization request: priceMode, safetyStockEnabled, optimizerVersion.
5. Tests: known-answer, reproducibility, API default version.

## Constitution

III Deterministic demos — Pass · VIII Modular — Pass · ADR-0001 superseded as system of record
