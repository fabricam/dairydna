# ADR 0001: Temporary naive optimizer for Feature 000

## Status

Superseded by Feature 009 (`ortools-cm-v1` is system of record).

## Context

DairyDNA Feature 000 needed a deterministic allocation loop for the thin vertical
slice. The program plan pins Google OR-Tools for Feature 009.

## Decision

Ship `NaiveContributionMarginOptimizer` (`naive-cm-v1`) behind
`IAllocationOptimizer` for Feature 000. Feature 009 becomes the optimization
system of record using OR-Tools (`ortools-cm-v1`). Naive remains available only
via explicit `optimizerVersion=naive-cm-v1` for regression comparison.

## Consequences

- Known-answer tests and interview demos are deterministic without MIP setup.
- Recommendations may differ between naive and OR-Tools; document version
  on every OptimizationRun.
- Do not maintain two permanent optimizers without revisiting this ADR.
