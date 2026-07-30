using System.Diagnostics;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Demo;
using DairyDNA.Application.Scenarios;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Optimization;

public sealed class CreateOptimizationRunRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly? AsOfDate { get; set; }
    /// <summary>Default OR-Tools system of record; pass naive-cm-v1 for regression compare.</summary>
    public string? OptimizerVersion { get; set; }
    public string PriceMode { get; set; } = "Spot"; // Spot | ForecastPoint | ForecastLower | ForecastUpper
    public bool SafetyStockEnabled { get; set; } = true;
    /// <summary>Ephemeral, persisted-by-scenario overlays; never mutates source rows.</summary>
    public ScenarioOverrides? ScenarioOverrides { get; set; }
}

public interface IAllocationOptimizerResolver
{
    IAllocationOptimizer Resolve(string? version);
}

public sealed class CreateOptimizationRunHandler
{
    private readonly IDairyDnaDbContext _db;
    private readonly IAllocationOptimizerResolver _resolvers;
    private readonly ITransportCostCalculator _transport;

    public CreateOptimizationRunHandler(
        IDairyDnaDbContext db,
        IAllocationOptimizerResolver resolvers,
        ITransportCostCalculator transport)
    {
        _db = db;
        _resolvers = resolvers;
        _transport = transport;
    }

    public async Task<OptimizationRun?> HandleAsync(CreateOptimizationRunRequest request, CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == request.GenerationId, ct);
        if (gen is null) return null;

        var optimizer = _resolvers.Resolve(request.OptimizerVersion);
        var asOf = request.AsOfDate ?? gen.PlanningDate;
        var sw = Stopwatch.StartNew();

        var lots = await _db.InventoryLots.Where(x => x.GenerationId == request.GenerationId && x.AsOfDate == asOf).ToListAsync(ct);
        if (request.SafetyStockEnabled)
        {
            lots = lots.Select(l => new InventoryLot
            {
                Id = l.Id,
                GenerationId = l.GenerationId,
                FacilityId = l.FacilityId,
                ProductId = l.ProductId,
                QuantityPounds = DomainInvariants.Money(l.QuantityPounds * 0.95m),
                ButterfatPercent = l.ButterfatPercent,
                ProducedAt = l.ProducedAt,
                ExpiresAt = l.ExpiresAt,
                QualityGrade = l.QualityGrade,
                Status = l.Status,
                AsOfDate = l.AsOfDate
            }).ToList();
        }

        var orders = await _db.Orders.Where(x => x.GenerationId == request.GenerationId && x.RequestDate == asOf && x.Status == OrderStatus.Open).ToListAsync(ct);
        // Clone orders so forecast price overlays do not mutate persistence
        orders = orders.Select(o => new Order
        {
            Id = o.Id,
            GenerationId = o.GenerationId,
            CustomerId = o.CustomerId,
            ProductId = o.ProductId,
            RequestedQuantityPounds = o.RequestedQuantityPounds,
            MinimumAcceptableQuantityPounds = o.MinimumAcceptableQuantityPounds,
            RequestedDeliveryStart = o.RequestedDeliveryStart,
            RequestedDeliveryEnd = o.RequestedDeliveryEnd,
            OfferedPricePerPound = o.OfferedPricePerPound,
            OrderType = o.OrderType,
            Status = o.Status,
            RequestDate = o.RequestDate
        }).ToList();

        ApplyScenarioOverrides(lots, orders, request.ScenarioOverrides);

        var input = new AllocationInput
        {
            AsOfDate = asOf,
            Facilities = await _db.Facilities.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct),
            Products = await _db.Products.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct),
            InventoryLots = lots,
            Customers = await _db.Customers.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct),
            Orders = orders,
            Trucks = await _db.Trucks.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct)
        };
        ApplyUserPriceOverrides(input, request.ScenarioOverrides);

        if (!string.Equals(request.PriceMode, "Spot", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyForecastPricesAsync(input, request.GenerationId, asOf, request.PriceMode, ct);
        }

        var transport = request.ScenarioOverrides?.FuelPricePerGallon is { } fuel
            ? new FuelOverrideTransportCostCalculator(_transport, fuel)
            : _transport;
        var result = optimizer.Optimize(input, transport);
        sw.Stop();

        var run = new OptimizationRun
        {
            Id = Guid.NewGuid(),
            GenerationId = request.GenerationId,
            AsOfDate = asOf,
            OptimizerVersion = optimizer.Version,
            Status = result.Status,
            ObjectiveValue = DomainInvariants.Money(result.ObjectiveValue),
            SolveDurationMilliseconds = (int)sw.ElapsedMilliseconds,
            CreatedAt = DateTimeOffset.UtcNow,
            DatasetSchemaVersion = gen.SchemaVersion,
            UnusedInventoryJson = JsonSerializer.Serialize(result.UnusedInventory),
            UnservedDemandJson = JsonSerializer.Serialize(result.UnservedDemand),
            FailureMessage = result.FailureMessage
        };

        _db.Add(run);
        foreach (var m in result.Movements)
        {
            _db.Add(new RecommendedMovement
            {
                Id = Guid.NewGuid(),
                OptimizationRunId = run.Id,
                OriginFacilityId = m.OriginFacilityId,
                DestinationCustomerId = m.DestinationCustomerId,
                ProductId = m.ProductId,
                QuantityPounds = m.QuantityPounds,
                TruckId = m.TruckId,
                OrderId = m.OrderId,
                ExpectedUnitPrice = m.ExpectedUnitPrice,
                ExpectedRevenue = m.ExpectedRevenue,
                TransportationCost = m.TransportationCost,
                FuelCost = m.FuelCost,
                OperatingCost = m.OperatingCost,
                DistanceMiles = m.DistanceMiles,
                ExpectedContributionMargin = m.ExpectedContributionMargin,
                DepartureTime = m.DepartureTime,
                ArrivalTime = m.ArrivalTime,
                Explanation = m.Explanation + $" PriceMode={request.PriceMode}; SafetyStock={request.SafetyStockEnabled}."
            });
        }

        await _db.SaveChangesAsync(ct);
        return run;
    }

    private static void ApplyScenarioOverrides(
        List<InventoryLot> lots,
        List<Order> orders,
        ScenarioOverrides? overrides)
    {
        if (overrides is null) return;
        ValidateOverrides(overrides);

        if (overrides.CapacityScaleFactor is { } capacity)
            foreach (var lot in lots)
                lot.QuantityPounds = DomainInvariants.Money(lot.QuantityPounds * capacity);

        if (overrides.DemandScaleFactor is { } demand)
            foreach (var order in orders)
            {
                order.RequestedQuantityPounds = DomainInvariants.Money(order.RequestedQuantityPounds * demand);
                order.MinimumAcceptableQuantityPounds = DomainInvariants.Money(order.MinimumAcceptableQuantityPounds * demand);
            }

    }

    private static void ApplyUserPriceOverrides(AllocationInput input, ScenarioOverrides? overrides)
    {
        if (overrides is null) return;
        if (overrides.UserPrices is { Count: > 0 } prices)
        {
            var productCodes = input.Products.ToDictionary(p => p.Id, p => p.Code);
            foreach (var order in input.Orders)
                if (productCodes.TryGetValue(order.ProductId, out var code) && prices.TryGetValue(code, out var price))
                {
                    if (price < 0) throw new ArgumentOutOfRangeException(nameof(overrides.UserPrices));
                    order.OfferedPricePerPound = DomainInvariants.Money(price);
                }
        }

        if (overrides.DistantCustomerPriceBump is not { } bump) return;
        var customers = input.Customers.ToDictionary(c => c.Id);
        foreach (var order in input.Orders)
            if (customers.TryGetValue(order.CustomerId, out var customer) && IsDistant(customer, input.Facilities))
                order.OfferedPricePerPound = DomainInvariants.Money(order.OfferedPricePerPound + bump);
    }

    private static bool IsDistant(Customer customer, IReadOnlyList<Facility> facilities) =>
        facilities.Count > 0 && facilities.Min(f =>
            Math.Pow((double)(customer.Latitude - f.Latitude), 2) +
            Math.Pow((double)(customer.Longitude - f.Longitude), 2)) > 4;

    private static void ValidateOverrides(ScenarioOverrides overrides)
    {
        if (overrides.FuelPricePerGallon is < 0)
            throw new ArgumentOutOfRangeException(nameof(overrides.FuelPricePerGallon));
        if (overrides.CapacityScaleFactor is <= 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(overrides.CapacityScaleFactor));
        if (overrides.DemandScaleFactor is <= 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(overrides.DemandScaleFactor));
    }

    private async Task ApplyForecastPricesAsync(AllocationInput input, Guid generationId, DateOnly asOf, string mode, CancellationToken ct)
    {
        var latest = await _db.PriceModelVersions
            .Where(m => m.GenerationId == generationId)
            .OrderByDescending(m => m.TrainedAt)
            .FirstOrDefaultAsync(ct);
        if (latest is null) return;

        var horizon = 1;
        var forecasts = await _db.PriceForecasts
            .Where(f => f.ModelVersionId == latest.Id && f.HorizonDays == horizon)
            .ToListAsync(ct);
        if (forecasts.Count == 0) return;

        var byProduct = forecasts.GroupBy(f => f.ProductCode).ToDictionary(g => g.Key, g => g.First());
        var products = input.Products.ToDictionary(p => p.Id);
        foreach (var order in input.Orders)
        {
            if (!products.TryGetValue(order.ProductId, out var product)) continue;
            if (!byProduct.TryGetValue(product.Code, out var fc)) continue;
            order.OfferedPricePerPound = mode.ToLowerInvariant() switch
            {
                "forecastlower" => fc.LowerBoundPricePerPound,
                "forecastupper" => fc.UpperBoundPricePerPound,
                _ => fc.PointEstimatePricePerPound
            };
        }
    }
}

public sealed class GetOptimizationRunHandler
{
    private readonly IDairyDnaDbContext _db;

    public GetOptimizationRunHandler(IDairyDnaDbContext db) => _db = db;

    public async Task<(OptimizationRun Run, List<RecommendedMovement> Movements, List<NetworkMapPoint> Network)?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var run = await _db.OptimizationRuns.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (run is null) return null;
        var movements = await _db.RecommendedMovements.Where(x => x.OptimizationRunId == id).ToListAsync(ct);
        var facilities = await _db.Facilities.Where(x => x.GenerationId == run.GenerationId).ToListAsync(ct);
        var customers = await _db.Customers.Where(x => x.GenerationId == run.GenerationId).ToListAsync(ct);
        var network = facilities
            .Select(f => new NetworkMapPoint(f.Id, "Facility", f.Name, f.Latitude, f.Longitude))
            .Concat(customers.Select(c => new NetworkMapPoint(c.Id, "Customer", c.Name, c.Latitude, c.Longitude)))
            .ToList();
        return (run, movements, network);
    }
}
