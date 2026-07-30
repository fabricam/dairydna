# Implementation Plan: Scenario Simulation

**Branch**: `010-scenario-simulation` | **Date**: 2026-07-29 | **Spec**: [spec.md](./spec.md)

## Summary
Persist versioned scenario definitions and runs, apply transient overlays to optimization inputs, and expose a base-versus-scenario compare view.

## Approach
1. Add `ScenarioDefinition` and `ScenarioRun` persistence linked to `OptimizationRun`.
2. Use JSON `ScenarioOverrides` to scale inventory or demand, modify prices, and override fuel costing without mutating source data.
3. Supply four idempotent flagship definitions and API endpoints for definitions, runs, and comparisons.
4. Present scenario outcomes with explicit simulation and recommendation-status labels.
