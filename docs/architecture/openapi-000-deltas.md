# OpenAPI deltas (000)

Runtime OpenAPI is emitted via `MapOpenApi()` on the API (`/openapi/v1.json` in Development).

Source of truth for the feature contract remains:

`specs/000-thin-vertical-slice/contracts/openapi.yaml`

## Intentional deltas

- JSON property names are camelCase in responses (ASP.NET defaults).
- Generation `POST` accepts optional body fields with thin-slice defaults when omitted.
- `EnsureCreated` is used instead of applying EF migrations at startup for the 000 demo.
- Problem Details is enabled for unhandled exceptions; not every 4xx is modeled as a typed Problem Details schema yet.

Contract smoke tests in `tests/DairyDNA.ContractTests` exercise the live Minimal API shapes against the critical paths in the YAML.
