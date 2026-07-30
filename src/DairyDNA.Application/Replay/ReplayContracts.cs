namespace DairyDNA.Application.Replay;

public sealed class RunReplayRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly AsOfDate { get; set; }
    /// <summary>Spot | ForecastPoint | ForecastLower | ForecastUpper; defaults to Spot (matches 009's PriceMode).</summary>
    public string? PriceMode { get; set; }
}

public sealed class RegretReportRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

/// <summary>Independent leakage check over the rows a replay could draw from — confirms every
/// checked row's effective date is on or before the replay's as-of date.</summary>
public sealed class LeakageAuditResult
{
    public bool Passed { get; set; }
    public int InventoryLotsChecked { get; set; }
    public int InventoryLotsViolating { get; set; }
    public int OrdersChecked { get; set; }
    public int OrdersViolating { get; set; }
    public int ForecastRowsChecked { get; set; }
    public int ForecastRowsViolating { get; set; }
    public IReadOnlyList<string> Violations { get; set; } = [];
    public string Statement { get; set; } = string.Empty;
}

public sealed record ReplayRunSummary(
    Guid Id,
    Guid GenerationId,
    DateOnly AsOfDate,
    Guid OptimizationRunId,
    string PriceMode,
    Guid? SupplyModelVersionId,
    Guid? DemandModelVersionId,
    Guid? PriceModelVersionId,
    string OptimizerVersion,
    string CostingModelVersion,
    bool LeakagePassed,
    string LeakageAuditJson,
    DateTimeOffset CreatedAt,
    string DataClassification,
    string OptimizationStatus,
    decimal ObjectiveValue,
    int UnservedCount,
    int UnusedCount);

/// <summary>A simple, deterministic non-optimizer allocation policy used only as a regret baseline.
/// Ignores truck time-window feasibility — documented proxy economics, not an alternate optimizer.</summary>
public sealed record BaselinePolicyResult(
    string PolicyName,
    decimal ObjectiveValue,
    decimal TotalRevenue,
    decimal TotalTransportCost,
    int UnservedCount,
    int MovementCount);

public sealed record OptimizerDayResult(
    Guid OptimizationRunId,
    string Status,
    decimal ObjectiveValue,
    decimal TotalRevenue,
    decimal TotalTransportCost,
    int UnservedCount,
    int MovementCount);

public sealed record DailyRegretRow(
    DateOnly AsOfDate,
    Guid ReplayRunId,
    OptimizerDayResult Optimizer,
    IReadOnlyList<BaselinePolicyResult> Baselines,
    bool OptimizerWins,
    string Note);

public sealed record RegretWindowSummary(
    int TotalDays,
    int OptimizerWinDays,
    bool MeetsBar,
    string Statement);

public sealed record RegretWindowReportDto(
    Guid Id,
    Guid GenerationId,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<DailyRegretRow> Days,
    RegretWindowSummary Summary,
    string DataClassification,
    DateTimeOffset CreatedAt);
