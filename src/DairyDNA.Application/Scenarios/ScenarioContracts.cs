using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Application.Scenarios;

public sealed class ScenarioOverrides
{
    public decimal? FuelPricePerGallon { get; set; }
    public decimal? CapacityScaleFactor { get; set; }
    public decimal? DistantCustomerPriceBump { get; set; }
    public decimal? DemandScaleFactor { get; set; }
    public bool? HeatFlag { get; set; }
    public Dictionary<string, decimal>? UserPrices { get; set; }
}

public sealed class CreateScenarioDefinitionRequest
{
    public Guid GenerationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScenarioOverrides Overrides { get; set; } = new();
}

public sealed record ScenarioComparisonRun(
    Guid RunId,
    OptimizationRunStatus Status,
    decimal ObjectiveValue,
    int UnservedCount,
    int UnusedCount,
    bool IsRecommended);

public sealed record ScenarioMovementDiff(
    Guid OriginFacilityId,
    Guid DestinationCustomerId,
    Guid ProductId,
    decimal BaseQuantityPounds,
    decimal ScenarioQuantityPounds);

public sealed record ScenarioComparison(
    ScenarioComparisonRun Base,
    ScenarioComparisonRun Scenario,
    IReadOnlyList<ScenarioMovementDiff> MovementDiffs,
    string DataClassification,
    string HonestyLabel);
