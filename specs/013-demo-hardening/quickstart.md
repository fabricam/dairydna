# Quickstart: Demo Hardening

1. Bring up the demo with one command:

   ```powershell
   dotnet run --project src/DairyDNA.AppHost
   ```

   or use `./scripts/demo-start.ps1` (add `-InMemory` to skip Docker
   Desktop/SQL Server). Both print the Aspire dashboard URL and remind you to
   follow `docs/demo/presenter-script.md`.

2. Follow **`docs/demo/presenter-script.md`** — the versioned walkthrough
   that exercises the network map, at least one chart (inventory age /
   forecast band / margin / regret), and recommendations, using the fixed
   **`docs/demo/seed-pack.md`** dataset (`thin-slice` profile, seed
   `104729`).

3. Optional one-shot bring-up for CI or a rushed demo:

   ```powershell
   Invoke-RestMethod -Uri http://localhost:5114/api/demo/bootstrap -Method Post -Body '{}' -ContentType 'application/json'
   ```

   Runs generate (`thin-slice`, seed `104729`) → apply the flagship scenario
   pack → one optimization run, and returns the generation id, optimization
   run id, objective value, and flagship scenario names. Calling it twice
   must return the same objective/quantities — this is asserted by
   `tests/DairyDNA.IntegrationTests/DemoBootstrapApiTests.cs`.

4. Smoke-check a running API:

   ```powershell
   ./scripts/demo-smoke.ps1 -Bootstrap
   ```

5. Run the demo-hardening-specific tests:

   ```powershell
   dotnet test tests/DairyDNA.UnitTests --filter "FullyQualifiedName~Demo"
   dotnet test tests/DairyDNA.IntegrationTests --filter "FullyQualifiedName~DemoBootstrap"
   ```

6. Run the lightweight CI-ish checks locally (build, test, secret scan,
   health-check presence):

   ```powershell
   ./scripts/ci-checks.ps1
   ```

   The same checks run in `.github/workflows/ci.yml` on push/PR to `main`.

7. Read `docs/demo/hardening-notes.md` for the reference machine profile,
   measured performance budgets (dashboard ≤2s, optimize ≤30s — both met
   with wide headroom), known limitations (including a documented
   `thin-slice`+`104729` zero-recommended-movement case and its
   `standard-demo` workaround), R1 logistics simplifications, and how to see
   OpenTelemetry traces for generate/optimize in the Aspire dashboard.

8. Read `docs/demo/honesty-boundary.md` (or the README's honesty-boundary
   section) before presenting: synthetic data only, recommendations are
   suggestions not executed trades/dispatch, local-dev/unauthenticated only.
