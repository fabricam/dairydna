# DairyDNA Constitution

## Core Principles

### I. Specifications Are the Source of Truth

No production feature may be implemented without an approved feature
specification (`spec.md`), implementation plan (`plan.md`), and task list
(`tasks.md`). Spec Kit artifacts are reviewable engineering deliverables, not
disposable prompts.

### II. Business Outcomes Before Technology

Feature specifications describe users, scenarios, requirements, constraints,
and measurable outcomes. Implementation details belong in `plan.md`. Technology
choices must serve demonstrated user value.

### III. Deterministic and Reproducible Demonstrations

Synthetic datasets MUST be reproducible from a seed. Model training MUST record
dataset version, feature schema, algorithm, hyperparameters, random seed,
evaluation metrics, and model version. Demo scenarios MUST be re-runnable to
the same logical outcome under the same configuration.

### IV. No Data Leakage

Forecasting train, validation, and test splits MUST preserve time ordering.
Future values MUST never be used to predict past observations. Feature
engineering MUST use explicit as-of timestamps.

### V. Optimization Feasibility Before Profitability

Every recommendation MUST satisfy inventory, truck capacity, plant capacity,
customer demand bounds, delivery window, shelf-life, and contract constraints.
Infeasible recommendations MUST never be presented as valid. High predicted
revenue alone NEVER justifies an infeasible movement.

### VI. Explainable Recommendations

Forecasts and movement recommendations MUST include the main factors,
assumptions, estimated revenue, estimated costs, binding constraints, and
expected contribution margin. Users MUST be able to answer “why this movement?”

### VII. Honest Uncertainty

Forecasts MUST include confidence or prediction intervals where technically
practical. The UI MUST distinguish actual values, forecasts, simulations, and
synthetic data. Forecasts MUST NOT be presented as guaranteed transaction
prices or outcomes.

### VIII. Modular Boundaries

Domain rules, forecasting, optimization, persistence, data generation,
ingestion, and presentation MUST remain independently testable. Domain MUST NOT
depend on ML.NET, OR-Tools, EF Core, or web frameworks.

### IX. Test-First Domain and Optimization Behavior

Business rules, feature transformations, optimization constraints, and
acceptance scenarios REQUIRE automated tests before implementation is
considered complete. Optimization REQUIRES known-answer tests and solver-
independent feasibility validation.

### X. Observable Distributed Behavior

Services MUST use structured logs, traces, health checks, and metrics through
.NET Aspire and OpenTelemetry. Long-running jobs MUST expose progress and
status.

### XI. Secure-by-Default Implementation

Secrets MUST NOT be committed. Inputs MUST be validated. Authorization MUST be
enforced for administrative functions. Generated demo data MUST contain no real
personally identifiable information and MUST NOT intentionally resemble real
customer identities.

### XII. Performance Budgets

Interactive dashboards SHOULD load their initial useful view within two seconds
under the documented demo workload. A normal optimization run SHOULD complete
within 30 seconds for the documented demo scenario. Forecast reads of
precomputed results SHOULD meet a 500 ms P95 budget.

### XIII. Versioned Data Contracts

Dataset schemas, API contracts, model input schemas, model outputs, and
optimization inputs MUST have explicit versions. Every forecast and
recommendation MUST be traceable to dataset, model, and optimizer versions.

### XIV. Small Independently Valuable Increments

Each feature MUST produce a testable business capability and MUST NOT depend on
completing the entire platform before it becomes useful. Prefer a thin
end-to-end slice before scaling data volume or model sophistication.

### XV. Simplicity Over Speculative Infrastructure

Begin with a modular monolith and independently hosted workers. Add messaging
or additional distributed services ONLY when justified by measured
requirements. Aspire orchestrates observability and local composition; it does
not justify premature microservice splits.

## Product Identity

- **Name**: DairyDNA
- **Type**: Demonstration ML decision-support and logistics optimization
  platform for dairy milk and cream allocation
- **Audience**: Operations planners, commodity/sales managers, plant managers,
  logistics coordinators, data scientists, and interview reviewers
- **Honesty boundary**: DairyDNA does not execute real trades, dispatch trucks,
  or provide production-grade market advice

## Governance

- Constitution checks are mandatory during `/speckit.plan` (before Phase 0 and
  again after Phase 1 design).
- Requirements MUST be traceable from specification → tasks → tests.
- Constitutional violations REQUIRE a documented exception in the feature plan
  Complexity Tracking table before implementation proceeds.
- `/speckit.analyze` MUST report no material inconsistencies before
  `/speckit.implement`.
- Features merge only when the Definition of Done in the program plan is met.

### Definition of Done (Summary)

Specifications, plans, tasks, implementation, tests, observability, security
validation, documentation of assumptions/limitations, and reproducible demo
behavior. ML features additionally require time-ordered evaluation, baseline
comparison, and no known leakage. Optimization features additionally require
independent feasibility validation, known-answer tests, and infeasible-case
handling.

**Version**: 1.0.0 | **Ratified**: 2026-07-26 | **Last Amended**: 2026-07-26
