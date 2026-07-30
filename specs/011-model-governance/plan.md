# Implementation Plan: Model Governance

**Branch**: `011-model-governance` | **Date**: 2026-07-29 | **Spec**: [spec.md](./spec.md)

## Summary
Add a candidate → published → retired lifecycle, artifact checksums, and an audited
publish/retire workflow directly on the existing `SupplyModelVersion` / `DemandModelVersion` /
`PriceModelVersion` entities, plus a model-card view for reviewers. Optimizer versions (009)
remain a small read-only catalog — no new optimizer registry is introduced.

## Approach
1. **Domain**: add `ModelLifecycleStatus` (Candidate/Published/Retired) and a shared
   `IModelVersion` interface implemented by the three existing forecast model version entities
   (they already have an identical shape) so governance code can operate on any family without a
   parallel model hierarchy. Add `GovernanceAuditEvent` for publish/retire/override audit rows.
2. **Persistence**: extend `DairyDnaDbContext`/`IDairyDnaDbContext` with `GovernanceAuditEvents`;
   no changes to existing forecast tables' shape beyond the new nullable/default-valued columns.
3. **Application**: `ModelGovernanceService` lists/gets/publishes/retires across all three
   families, computes a reproducible SHA-256 checksum (`ModelArtifactChecksum`) from training
   metadata, enforces "checksum required to publish" and "quality gate unless overridden", retires
   the previous published version in the same family + generation on publish, and builds a model
   card (intent, data summary, metrics vs. baseline, limitations, leakage-control statement).
4. **Forecasting wiring**: `MlNetSupplyForecastService`/`MlNetDemandForecastService`/
   `MlNetPriceForecastService` set `LifecycleStatus = Candidate` and compute the checksum right
   after training; `GetLatestModelAsync` now prefers the family's `Published` version (by
   `PublishedAt`) and falls back to the newest non-retired trained version so forecasts stay
   available before a governance review happens (documented fallback, not silent).
5. **API**: `/api/models` (list/get/card/publish/retire) plus `/api/models/optimizers` for the
   read-only optimizer catalog.
6. **UI**: `/models` page — family filter, version table with lifecycle/publish/retire actions,
   and a model-card panel with an SVG bar chart comparing model vs. baseline WAPE.
