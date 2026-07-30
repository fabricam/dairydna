# Tasks: Historical Replay

- [X] T001 Add `ReplayRun` and `ReplayWindowReport` entities.
- [X] T002 Add `ReplayRuns`/`ReplayWindowReports` to `DairyDnaDbContext` and `IDairyDnaDbContext`
      (keys/indexes, unique `OptimizationRunId` per replay run).
- [X] T003 Implement `ReplayService` (leakage audit, `RunAsync`/`GetAsync`/`ListAsync`) as a thin
      wrapper over `CreateOptimizationRunHandler` + existing as-of filtering.
- [X] T004 Implement `NearestCustomerGreedy` and `HighestPriceFirst` baseline policies and
      `BuildRegretReportAsync` (day-by-day optimizer-vs-baseline comparison + window summary).
- [X] T005 Expose `/api/replay` endpoints (runs + regret reports) and dependency injection.
- [X] T006 Add `/replay` UI page (day scrubber, replay summary + reused network map/recommendation
      highlights, regret-report comparison chart) and a nav link.
- [X] T007 Add unit coverage (`ReplayServiceTests`: determinism, leakage audit, regret baselines) and
      integration coverage (`ReplayApiTests`: generate → run → get → regret report, linked
      `OptimizationRun`); generate the checked-in regret-window fixture.
- [X] T008 Add plan, quickstart, and feature-index updates.
