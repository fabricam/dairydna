# ADR 0001: Temporary naive optimizer for Feature 000

## Status

Accepted

## Context

DairyDNA Feature 000 needs a deterministic allocation loop for the thin vertical
slice. The program plan pins Google OR-Tools for Feature 009.

## Decision

Ship `NaiveContributionMarginOptimizer` (`naive-cm-v1`) behind
`IAllocationOptimizer` for Feature 000. Feature 009 becomes the optimization
system of record using OR-Tools.

## Consequences

- Known-answer tests and interview demos are deterministic without MIP setup.
- Recommendations may differ from future OR-Tools solutions; document version
  on every OptimizationRun.
- Do not maintain two permanent optimizers without revisiting this ADR.
