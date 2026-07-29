# Implementation Plan: Synthetic Data Generator

**Branch**: `002-synthetic-data-generator` | **Date**: 2026-07-28 | **Spec**: [spec.md](./spec.md)

## Summary

Generalize the thin-slice generator into a profile-driven synthetic data engine
with `thin-slice` and `standard-demo` catalogs, custom overrides (with max
bounds), validation reports, weather factors, historical shipments, generator
versioning, and seed reproducibility.

## Technical Context

Pinned stack unchanged. Generator lives in `DairyDNA.DataGenerator`. Standard
demo uses full network counts (150/8/75/30, 6 products); default history is
**~3 years** with daily cadence — CI uses thin-slice + small custom profiles;
standard-demo generate is covered by a dedicated smoke test with optional
shortened range via custom overrides.

## Approach

1. `GenerationProfile` catalog + request DTO (`profileName`, seed, overrides).
2. Refactor generator to `SyntheticDataGenerator` (keep thin-slice path).
3. Persist `ValidationReportJson`, `GeneratorVersion`, `ProfileName` on manifest.
4. Add `WeatherObservation`; generate shipments + multi-product catalog.
5. API: list profiles, generate by profile, get validation report.
6. UI: profile picker on demo home.
7. Tests: repro, invalid profile, thin-slice counts, validation report present.

## Constitution

III Deterministic demos — Pass · VIII Modular — Pass · XI Open local — Exception (demo)
