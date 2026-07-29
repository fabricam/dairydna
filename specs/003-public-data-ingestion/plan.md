# Implementation Plan: Public Data Ingestion

**Branch**: `003-public-data-ingestion` | **Date**: 2026-07-28 | **Spec**: [spec.md](./spec.md)

## Summary

Add fixture-driven public data ingestion for dairy market prices, weather, and
fuel prices with versioned raw payloads, schema validation, quarantine, and
idempotent re-import. Canonical observations carry source + import-run provenance
and are labeled Public/external in API/UI.

## Approach

1. Domain: ImportSource, ImportRun, RawPayload, QuarantineItem, PublicMarketPrice,
   PublicWeatherObservation, FuelPriceObservation + ImportRunStatus enum.
2. Project `DairyDNA.DataIngestion`: fixture loader, validators, import service.
3. API: list sources, run import from fixture name, list runs, get quarantine.
4. Minimal Web page to trigger fixture imports and show run status.
5. Tests: happy path ×3, malformed quarantine, idempotent checksum re-import.

## Constitution

III Deterministic demos — Pass · XI Open local (fixtures) — Pass · XIII Contracts — Pass
