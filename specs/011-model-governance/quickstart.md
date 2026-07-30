# Quickstart: Model Governance

1. Generate a thin-slice dataset from the Demo page.
2. Train a supply, demand, or price forecast (their pages, or `POST /api/forecasts/{family}/runs`).
   The resulting model version is registered as `Candidate` with a computed artifact checksum.
3. Open `/models`, filter by family, and select a version to view its model card (intent, data
   summary, WAPE-vs-baseline chart, limitations, and leakage-control statement).
4. Publish a candidate that meets the quality gate (or check "override" and provide a reason to
   publish anyway). Publishing retires any previously published version in the same family and
   generation and records an audit event.
5. Retire a published version; new inference for that family/generation falls back to the newest
   remaining non-retired trained version and no longer selects the retired one by default.
6. `GET /api/models/optimizers` lists the read-only optimizer version catalog
   (`ortools-cm-v1`, `naive-cm-v1`, `transport-cost-v2`) referenced by optimization runs (009).

Model cards and lifecycle changes are demo governance features — publishing/retiring changes which
model version future inference in this generation defaults to; it does not delete any historical
forecasts or audit history.
