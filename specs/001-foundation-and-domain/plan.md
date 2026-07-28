# Implementation Plan: Foundation and Domain

**Branch**: `001-foundation-and-domain` | **Date**: 2026-07-28 | **Spec**: [spec.md](./spec.md)

## Summary

Expand DairyDNA beyond the thin-slice demo into browsable, validated reference
data: farms, facilities, customers, products, trucks, contracts (plus shipment
read model). Soft-deactivate without deleting history. Network map includes
farms. Builds on Feature 000 types and Aspire stack; no forecasting/optimize
work required for acceptance.

## Technical Context

**Language/Version**: .NET 10 · **UI**: Blazor + Semantic UI + Fluxor · **API**:
Minimal APIs · **DB**: EF Core + SQL Server / InMemory · **Tests**: xUnit

**Constraints**: Pounds; entities scoped by `GenerationId` (browse after generate
or against seeded foundation generation); open local access same as 000.

## Constitution Check

| Principle | Status |
|-----------|--------|
| I–III, V–VI, VIII–X, XII–XV | Pass |
| IV Leakage | N/A |
| VII Honesty | Pass — Synthetic labels |
| XI Secure-by-default | Exception — open local demo (same as 000) |

## Project Structure

```text
src/DairyDNA.Domain/          # Contract, Shipment; Active on Product/Truck; validators
src/DairyDNA.Application/     # Reference-data handlers
src/DairyDNA.Infrastructure/  # DbSets + EnsureCreated
src/DairyDNA.Api/Endpoints/   # ReferenceEndpoints.cs
src/DairyDNA.Web/             # Network + entity browse pages
tests/DairyDNA.UnitTests/     # Invariant coverage
tests/DairyDNA.IntegrationTests/  # CRUD rejection + list/deactivate
```

## Complexity Tracking

| Violation | Why needed | Alternative rejected |
|-----------|------------|----------------------|
| Open local access | Demo continuity with 000 | Full auth deferred to 013 |

## Implementation Approach

1. Domain: add `Contract`, `Shipment`; `Active` on Product/Truck; expand validators
   (contract dates, non-empty names, truck–product assignment helper).
2. Persistence: DbSets; EnsureCreated remains for local demo.
3. API: Minimal endpoints for list/detail/create + soft-deactivate; filter
   `activeOnly=true` by default; generationId query required.
4. Web: Reference data hub + network map (farms/facilities/customers) + list/detail.
5. Tests: invariant matrix + API rejection + deactivate hides from default lists.
6. Docs: quickstart Scenario for browse after generate.

## Risks

- Generation-scoped IDs: UI must carry selected generationId (from 000 generate).
- InMemory EnsureCreated: schema expands automatically on restart.
