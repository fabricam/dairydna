using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Application.Abstractions;

public interface IDairyDnaDbContext
{
    IQueryable<GenerationManifest> GenerationManifests { get; }
    IQueryable<Farm> Farms { get; }
    IQueryable<Facility> Facilities { get; }
    IQueryable<Product> Products { get; }
    IQueryable<InventoryLot> InventoryLots { get; }
    IQueryable<Customer> Customers { get; }
    IQueryable<Order> Orders { get; }
    IQueryable<Truck> Trucks { get; }
    IQueryable<MarketPrice> MarketPrices { get; }
    IQueryable<OptimizationRun> OptimizationRuns { get; }
    IQueryable<RecommendedMovement> RecommendedMovements { get; }
    IQueryable<Contract> Contracts { get; }
    IQueryable<Shipment> Shipments { get; }
    IQueryable<WeatherObservation> WeatherObservations { get; }

    void Add<T>(T entity) where T : class;
    void AddRange<T>(IEnumerable<T> entities) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class TransportCostBreakdown
{
    public decimal DistanceMiles { get; init; }
    public decimal FuelCost { get; init; }
    public decimal OperatingCost { get; init; }
    public decimal TotalEstimatedCost { get; init; }
}

public interface ITransportCostCalculator
{
    TransportCostBreakdown Calculate(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon,
        decimal costPerMile,
        decimal costPerHour,
        decimal quantityPounds);
}

public sealed class AllocationCandidateMovement
{
    public Guid OriginFacilityId { get; init; }
    public Guid DestinationCustomerId { get; init; }
    public Guid ProductId { get; init; }
    public Guid TruckId { get; init; }
    public Guid? OrderId { get; init; }
    public decimal QuantityPounds { get; init; }
    public decimal ExpectedUnitPrice { get; init; }
    public decimal ExpectedRevenue { get; init; }
    public decimal TransportationCost { get; init; }
    public decimal FuelCost { get; init; }
    public decimal OperatingCost { get; init; }
    public decimal DistanceMiles { get; init; }
    public decimal ExpectedContributionMargin { get; init; }
    public DateTimeOffset DepartureTime { get; init; }
    public DateTimeOffset ArrivalTime { get; init; }
    public string Explanation { get; init; } = string.Empty;
}

public sealed class UnusedInventoryRow
{
    public Guid FacilityId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public decimal QuantityPounds { get; init; }
}

public sealed class UnservedDemandRow
{
    public Guid OrderId { get; init; }
    public decimal RemainingQuantityPounds { get; init; }
}

public sealed class AllocationResult
{
    public OptimizationRunStatus Status { get; init; }
    public decimal ObjectiveValue { get; init; }
    public IReadOnlyList<AllocationCandidateMovement> Movements { get; init; } = [];
    public IReadOnlyList<UnusedInventoryRow> UnusedInventory { get; init; } = [];
    public IReadOnlyList<UnservedDemandRow> UnservedDemand { get; init; } = [];
    public string? FailureMessage { get; init; }
}

public sealed class AllocationInput
{
    public DateOnly AsOfDate { get; init; }
    public IReadOnlyList<Facility> Facilities { get; init; } = [];
    public IReadOnlyList<Product> Products { get; init; } = [];
    public IReadOnlyList<InventoryLot> InventoryLots { get; init; } = [];
    public IReadOnlyList<Customer> Customers { get; init; } = [];
    public IReadOnlyList<Order> Orders { get; init; } = [];
    public IReadOnlyList<Truck> Trucks { get; init; } = [];
}

public interface IAllocationOptimizer
{
    string Version { get; }
    AllocationResult Optimize(AllocationInput input, ITransportCostCalculator transportCostCalculator);
}
