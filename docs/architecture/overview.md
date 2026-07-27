# DairyDNA Architecture Overview

See `specs/000-program/program-plan.md` for the authoritative Spec Kit program
plan. This note captures the structural intent for implementers.

## Style

Modular monolith with background workers, orchestrated locally by .NET Aspire.

## Layers

| Project | Responsibility |
|---------|----------------|
| `DairyDNA.Web` | Blazor dashboards with Semantic UI; Fluxor for UI state |
| `DairyDNA.Api` | ASP.NET Core Minimal APIs (no controllers), authz, OpenAPI |
| `DairyDNA.Domain` | Entities, invariants, domain services |
| `DairyDNA.Application` | Use cases and ports |
| `DairyDNA.Infrastructure` | EF Core (SQL Server), files, adapters |
| `DairyDNA.Forecasting` | Features, ML.NET, backtesting |
| `DairyDNA.Optimization` | OR-Tools models and explanations |
| `DairyDNA.DataGenerator` | Seeded synthetic data |
| `DairyDNA.DataIngestion` | Public dataset import |
| `DairyDNA.Worker` | Long-running jobs |
| `DairyDNA.AppHost` | Aspire composition |

## Hard Rules

- Domain has no ML.NET / OR-Tools / EF / web dependencies.
- `DairyDNA.Api` uses Minimal APIs only — no MVC controllers.
- Feasibility before profitability.
- Forecasts carry versions and uncertainty; UI labels synthetic vs actual vs forecast.
- Feature 000 may ship a temporary naive optimizer; Feature 009 is system of record.
