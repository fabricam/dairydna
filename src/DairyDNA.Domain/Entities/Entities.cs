using DairyDNA.Domain.Enums;

namespace DairyDNA.Domain.Entities;

public sealed class GenerationManifest
{
    public Guid Id { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "dairydna.thin-slice.v1";
    public int RandomSeed { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly PlanningDate { get; set; }
    public int FarmCount { get; set; }
    public int FacilityCount { get; set; }
    public int CustomerCount { get; set; }
    public int TruckCount { get; set; }
    public string ConfigurationHash { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public GenerationRunStatus Status { get; set; }
    public bool IsSynthetic { get; set; } = true;
    public string? FailureMessage { get; set; }
    public string EntityCountsJson { get; set; } = "{}";
    public string GeneratorVersion { get; set; } = "synthetic-gen-v2";
    public string ProfileName { get; set; } = "thin-slice";
    public string ValidationReportJson { get; set; } = "{}";
}

public sealed class WeatherObservation
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public DateOnly ObservationDate { get; set; }
    public decimal TemperatureF { get; set; }
    public decimal HeatStressIndex { get; set; }
}

public sealed class Farm
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int HerdSize { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Facility
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FacilityType FacilityType { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal MilkStorageCapacityPounds { get; set; }
    public decimal CreamStorageCapacityPounds { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Product
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaximumAgeHours { get; set; }
    public string UnitOfMeasure { get; set; } = "lb";
    public bool Active { get; set; } = true;
}

public sealed class InventoryLot
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityPounds { get; set; }
    public decimal ButterfatPercent { get; set; }
    public DateTimeOffset ProducedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string QualityGrade { get; set; } = "A";
    public InventoryLotStatus Status { get; set; }
    public DateOnly AsOfDate { get; set; }
}

public sealed class Customer
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public decimal RequestedQuantityPounds { get; set; }
    public decimal MinimumAcceptableQuantityPounds { get; set; }
    public DateTimeOffset RequestedDeliveryStart { get; set; }
    public DateTimeOffset RequestedDeliveryEnd { get; set; }
    public decimal OfferedPricePerPound { get; set; }
    public OrderType OrderType { get; set; } = OrderType.Spot;
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public DateOnly RequestDate { get; set; }
}

public sealed class Truck
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public Guid HomeFacilityId { get; set; }
    public decimal MaximumCapacityPounds { get; set; }
    public string CompatibleProductCodes { get; set; } = "RAW_MILK,CREAM";
    public decimal CostPerMile { get; set; }
    public decimal CostPerHour { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableUntil { get; set; }
    public TruckStatus Status { get; set; } = TruckStatus.Available;
    public bool Active { get; set; } = true;
}

public sealed class MarketPrice
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public Guid ProductId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public decimal PricePerPound { get; set; }
    public MarketPriceType PriceType { get; set; } = MarketPriceType.StaticSpot;
    public string Source { get; set; } = "synthetic-static";
}

public sealed class OptimizationRun
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public DateOnly AsOfDate { get; set; }
    public string OptimizerVersion { get; set; } = "naive-cm-v1";
    public OptimizationRunStatus Status { get; set; }
    public decimal ObjectiveValue { get; set; }
    public int SolveDurationMilliseconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string DatasetSchemaVersion { get; set; } = "dairydna.thin-slice.v1";
    public string UnusedInventoryJson { get; set; } = "[]";
    public string UnservedDemandJson { get; set; } = "[]";
    public string? FailureMessage { get; set; }
}

public sealed class RecommendedMovement
{
    public Guid Id { get; set; }
    public Guid OptimizationRunId { get; set; }
    public Guid OriginFacilityId { get; set; }
    public Guid DestinationCustomerId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityPounds { get; set; }
    public Guid TruckId { get; set; }
    public Guid? OrderId { get; set; }
    public decimal ExpectedUnitPrice { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal TransportationCost { get; set; }
    public decimal FuelCost { get; set; }
    public decimal OperatingCost { get; set; }
    public decimal DistanceMiles { get; set; }
    public decimal ExpectedContributionMargin { get; set; }
    public DateTimeOffset DepartureTime { get; set; }
    public DateTimeOffset ArrivalTime { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public sealed class Contract
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MinimumQuantityPounds { get; set; }
    public decimal MaximumQuantityPounds { get; set; }
    public decimal PricePerPound { get; set; }
    public decimal ShortfallPenaltyPerPound { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Shipment
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public Guid OriginFacilityId { get; set; }
    public DestinationType DestinationType { get; set; }
    public Guid DestinationId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityPounds { get; set; }
    public Guid? TruckId { get; set; }
    public DateTimeOffset? DepartedAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Planned;
}
