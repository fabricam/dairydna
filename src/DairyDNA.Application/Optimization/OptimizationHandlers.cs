using System.Diagnostics;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Optimization;

public sealed class CreateOptimizationRunRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly? AsOfDate { get; set; }
    public string OptimizerVersion { get; set; } = "naive-cm-v1";
}

public sealed class CreateOptimizationRunHandler
{
    private readonly IDairyDnaDbContext _db;
    private readonly IAllocationOptimizer _optimizer;
    private readonly ITransportCostCalculator _transport;

    public CreateOptimizationRunHandler(IDairyDnaDbContext db, IAllocationOptimizer optimizer, ITransportCostCalculator transport)
    {
        _db = db;
        _optimizer = optimizer;
        _transport = transport;
    }

    public async Task<OptimizationRun?> HandleAsync(CreateOptimizationRunRequest request, CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == request.GenerationId, ct);
        if (gen is null) return null;

        var asOf = request.AsOfDate ?? gen.PlanningDate;
        var sw = Stopwatch.StartNew();

        var input = new AllocationInput
        {
            AsOfDate = asOf,
            Facilities = await _db.Facilities.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct),
            Products = await _db.Products.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct),
            InventoryLots = await _db.InventoryLots.Where(x => x.GenerationId == request.GenerationId && x.AsOfDate == asOf).ToListAsync(ct),
            Customers = await _db.Customers.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct),
            Orders = await _db.Orders.Where(x => x.GenerationId == request.GenerationId && x.RequestDate == asOf && x.Status == OrderStatus.Open).ToListAsync(ct),
            Trucks = await _db.Trucks.Where(x => x.GenerationId == request.GenerationId).ToListAsync(ct)
        };

        var result = _optimizer.Optimize(input, _transport);
        sw.Stop();

        var run = new OptimizationRun
        {
            Id = Guid.NewGuid(),
            GenerationId = request.GenerationId,
            AsOfDate = asOf,
            OptimizerVersion = _optimizer.Version,
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
                Explanation = m.Explanation
            });
        }

        await _db.SaveChangesAsync(ct);
        return run;
    }
}

public sealed class GetOptimizationRunHandler
{
    private readonly IDairyDnaDbContext _db;

    public GetOptimizationRunHandler(IDairyDnaDbContext db) => _db = db;

    public async Task<(OptimizationRun Run, List<RecommendedMovement> Movements)?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var run = await _db.OptimizationRuns.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (run is null) return null;
        var movements = await _db.RecommendedMovements.Where(x => x.OptimizationRunId == id).ToListAsync(ct);
        return (run, movements);
    }
}
