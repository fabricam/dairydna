# Tasks: Model Governance

- [X] T001 Add `ModelLifecycleStatus`, `IModelVersion`, and `GovernanceAuditEvent`; extend
      Supply/Demand/PriceModelVersion with checksum + lifecycle fields.
- [X] T002 Add `GovernanceAuditEvents` to `DairyDnaDbContext` and `IDairyDnaDbContext`.
- [X] T003 Implement `ModelArtifactChecksum` and `ModelGovernanceService`
      (list/get/card/publish/retire + optimizer catalog).
- [X] T004 Wire supply/demand/price training to set `Candidate` + checksum, and prefer
      `Published` versions for inference (`GetLatestModelAsync`).
- [X] T005 Expose `/api/models` governance endpoints and dependency injection.
- [X] T006 Add `/models` UI page with lifecycle actions and a model-card WAPE comparison chart.
- [X] T007 Add unit coverage (`ModelGovernanceServiceTests`) and integration coverage
      (`ModelGovernanceApiTests`).
- [X] T008 Add plan, quickstart, and feature-index updates.
