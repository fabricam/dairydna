using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Optimization;
using DairyDNA.Application.Transport;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Replay;

public interface IReplayService
{
    Task<ReplayRunSummary> RunAsync(Guid generationId, DateOnly asOfDate, string? priceMode, CancellationToken ct = default);
    Task<ReplayRunSummary?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ReplayRunSummary>> ListAsync(Guid generationId, DateOnly? asOfDate, CancellationToken ct = default);
    Task<RegretWindowReportDto> BuildRegretReportAsync(Guid generationId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<RegretWindowReportDto?> GetReportAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Thin wrapper around <see cref="CreateOptimizationRunHandler"/>: replays a historical as-of date
/// using only data available as of that date, records the model/optimizer/costing versions used,
/// and compares the optimizer against simple baseline policies over a date window (regret report).
/// </summary>
public sealed class ReplayService : IReplayService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const decimal WinTolerance = 0.01m;

    private readonly IDairyDnaDbContext _db;
    private readonly CreateOptimizationRunHandler _createOptimizationRun;
    private readonly ITransportCostCalculator _transport;

    public ReplayService(IDairyDnaDbContext db, CreateOptimizationRunHandler createOptimizationRun, ITransportCostCalculator transport)
    {
        _db = db;
        _createOptimizationRun = createOptimizationRun;
        _transport = transport;
    }

    public async Task<ReplayRunSummary> RunAsync(Guid generationId, DateOnly asOfDate, string? priceMode, CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(g => g.Id == generationId, ct)
            ?? throw new KeyNotFoundException("Generation was not found.");
        if (asOfDate < gen.StartDate || asOfDate > gen.EndDate)
            throw new ArgumentOutOfRangeException(nameof(asOfDate), asOfDate, "AsOfDate must fall within the generation's dataset window.");

        var mode = string.IsNullOrWhiteSpace(priceMode) ? "Spot" : priceMode.Trim();
        var audit = await AuditLeakageAsync(generationId, asOfDate, mode, ct);
        if (!audit.Passed)
            throw new InvalidOperationException(audit.Statement);

        var optimizationRun = await _createOptimizationRun.HandleAsync(new CreateOptimizationRunRequest
        {
            GenerationId = generationId,
            AsOfDate = asOfDate,
            PriceMode = mode
        }, ct) ?? throw new KeyNotFoundException("Generation was not found.");

        var replayRun = new ReplayRun
        {
            Id = Guid.NewGuid(),
            GenerationId = generationId,
            AsOfDate = asOfDate,
            OptimizationRunId = optimizationRun.Id,
            PriceMode = mode,
            SupplyModelVersionId = await ResolveSupplyModelVersionAsync(generationId, ct),
            DemandModelVersionId = await ResolveDemandModelVersionAsync(generationId, ct),
            PriceModelVersionId = await ResolvePriceModelVersionAsync(generationId, ct),
            OptimizerVersion = optimizationRun.OptimizerVersion,
            CostingModelVersion = TransportCostCalculator.CostingModelVersion,
            LeakageAuditJson = JsonSerializer.Serialize(audit, JsonOptions),
            LeakagePassed = audit.Passed,
            CreatedAt = DateTimeOffset.UtcNow,
            DataClassification = "Synthetic"
        };
        _db.Add(replayRun);
        await _db.SaveChangesAsync(ct);

        return ToSummary(replayRun, optimizationRun);
    }

    public async Task<ReplayRunSummary?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var replayRun = await _db.ReplayRuns.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (replayRun is null) return null;
        var run = await _db.OptimizationRuns.FirstOrDefaultAsync(r => r.Id == replayRun.OptimizationRunId, ct);
        return run is null ? null : ToSummary(replayRun, run);
    }

    public async Task<IReadOnlyList<ReplayRunSummary>> ListAsync(Guid generationId, DateOnly? asOfDate, CancellationToken ct = default)
    {
        var replayRuns = await _db.ReplayRuns
            .Where(r => r.GenerationId == generationId && (asOfDate == null || r.AsOfDate == asOfDate))
            .OrderByDescending(r => r.AsOfDate).ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        if (replayRuns.Count == 0) return [];

        var runIds = replayRuns.Select(r => r.OptimizationRunId).ToList();
        var runMap = await _db.OptimizationRuns.Where(r => runIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);
        return replayRuns
            .Where(r => runMap.ContainsKey(r.OptimizationRunId))
            .Select(r => ToSummary(r, runMap[r.OptimizationRunId]))
            .ToList();
    }

    public async Task<RegretWindowReportDto> BuildRegretReportAsync(Guid generationId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(g => g.Id == generationId, ct)
            ?? throw new KeyNotFoundException("Generation was not found.");
        if (endDate < startDate)
            throw new ArgumentException("EndDate must be on or after StartDate.");
        if (startDate < gen.StartDate || endDate > gen.EndDate)
            throw new ArgumentOutOfRangeException(nameof(startDate), startDate, "Window must fall within the generation's dataset range.");

        var facilities = await _db.Facilities.Where(f => f.GenerationId == generationId).ToListAsync(ct);
        var products = await _db.Products.Where(p => p.GenerationId == generationId).ToListAsync(ct);
        var customers = await _db.Customers.Where(c => c.GenerationId == generationId).ToListAsync(ct);
        var trucks = await _db.Trucks.Where(t => t.GenerationId == generationId).ToListAsync(ct);

        var days = new List<DailyRegretRow>();
        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var summary = await GetOrRunSpotReplayAsync(generationId, day, ct);

            var movements = await _db.RecommendedMovements.Where(m => m.OptimizationRunId == summary.OptimizationRunId).ToListAsync(ct);
            var optimizerResult = new OptimizerDayResult(
                summary.OptimizationRunId,
                summary.OptimizationStatus,
                summary.ObjectiveValue,
                DomainInvariants.Money(movements.Sum(m => m.ExpectedRevenue)),
                DomainInvariants.Money(movements.Sum(m => m.TransportationCost)),
                summary.UnservedCount,
                movements.Count);

            var lots = await _db.InventoryLots.Where(l => l.GenerationId == generationId && l.AsOfDate == day).ToListAsync(ct);
            var orders = await _db.Orders.Where(o => o.GenerationId == generationId && o.RequestDate == day && o.Status == OrderStatus.Open).ToListAsync(ct);
            var input = new AllocationInput
            {
                AsOfDate = day,
                Facilities = facilities,
                Products = products,
                InventoryLots = lots,
                Customers = customers,
                Orders = orders,
                Trucks = trucks
            };

            var baselines = new List<BaselinePolicyResult>
            {
                RunNearestCustomerGreedy(input),
                RunHighestPriceFirst(input)
            };

            var bestBaseline = baselines.Count == 0 ? 0m : baselines.Max(b => b.ObjectiveValue);
            var wins = optimizerResult.ObjectiveValue > bestBaseline + WinTolerance;
            var note = wins
                ? $"Optimizer objective {optimizerResult.ObjectiveValue:0.00} beat the best baseline ({bestBaseline:0.00}) on {day:yyyy-MM-dd}."
                : $"Optimizer objective {optimizerResult.ObjectiveValue:0.00} did not exceed the best baseline ({bestBaseline:0.00}) on {day:yyyy-MM-dd}; this day did not meet the regret bar.";

            days.Add(new DailyRegretRow(day, summary.Id, optimizerResult, baselines, wins, note));
        }

        var winDays = days.Count(d => d.OptimizerWins);
        var meetsBar = winDays > 0;
        var statement = meetsBar
            ? $"Optimizer wins on at least one primary metric (contribution margin) on {winDays} of {days.Count} day(s) in this window."
            : $"Optimizer did not win on any of the {days.Count} day(s) in this window; recommendations did not beat the naive baselines for this evaluation window (explicit failure to meet the bar).";

        var reportId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var dto = new RegretWindowReportDto(
            reportId, generationId, startDate, endDate, days,
            new RegretWindowSummary(days.Count, winDays, meetsBar, statement),
            "Synthetic", createdAt);

        _db.Add(new ReplayWindowReport
        {
            Id = reportId,
            GenerationId = generationId,
            StartDate = startDate,
            EndDate = endDate,
            ReportJson = JsonSerializer.Serialize(dto, JsonOptions),
            CreatedAt = createdAt
        });
        await _db.SaveChangesAsync(ct);

        return dto;
    }

    public async Task<RegretWindowReportDto?> GetReportAsync(Guid id, CancellationToken ct = default)
    {
        var report = await _db.ReplayWindowReports.FirstOrDefaultAsync(r => r.Id == id, ct);
        return report is null ? null : JsonSerializer.Deserialize<RegretWindowReportDto>(report.ReportJson, JsonOptions);
    }

    private async Task<ReplayRunSummary> GetOrRunSpotReplayAsync(Guid generationId, DateOnly day, CancellationToken ct)
    {
        var existing = await _db.ReplayRuns
            .Where(r => r.GenerationId == generationId && r.AsOfDate == day && r.PriceMode == "Spot")
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (existing is null)
            return await RunAsync(generationId, day, "Spot", ct);

        var run = await _db.OptimizationRuns.FirstAsync(r => r.Id == existing.OptimizationRunId, ct);
        return ToSummary(existing, run);
    }

    private async Task<LeakageAuditResult> AuditLeakageAsync(Guid generationId, DateOnly asOf, string priceMode, CancellationToken ct)
    {
        var lotsForDay = await _db.InventoryLots.Where(l => l.GenerationId == generationId && l.AsOfDate == asOf).ToListAsync(ct);
        var lotViolations = lotsForDay.Where(l => l.AsOfDate > asOf).ToList();

        var ordersForDay = await _db.Orders.Where(o => o.GenerationId == generationId && o.RequestDate == asOf).ToListAsync(ct);
        var orderViolations = ordersForDay.Where(o => o.RequestDate > asOf).ToList();

        var violations = new List<string>();
        violations.AddRange(lotViolations.Select(l => $"InventoryLot {l.Id} has AsOfDate {l.AsOfDate:yyyy-MM-dd} after replay date {asOf:yyyy-MM-dd}."));
        violations.AddRange(orderViolations.Select(o => $"Order {o.Id} has RequestDate {o.RequestDate:yyyy-MM-dd} after replay date {asOf:yyyy-MM-dd}."));

        var forecastChecked = 0;
        var forecastViolationCount = 0;
        if (!string.Equals(priceMode, "Spot", StringComparison.OrdinalIgnoreCase))
        {
            var latestPriceModel = await _db.PriceModelVersions
                .Where(m => m.GenerationId == generationId)
                .OrderByDescending(m => m.TrainedAt)
                .FirstOrDefaultAsync(ct);
            if (latestPriceModel is not null)
            {
                var forecasts = await _db.PriceForecasts
                    .Where(f => f.ModelVersionId == latestPriceModel.Id && f.HorizonDays == 1)
                    .ToListAsync(ct);
                forecastChecked = forecasts.Count;
                var forecastViolations = forecasts.Where(f => f.AsOfDate > asOf).ToList();
                forecastViolationCount = forecastViolations.Count;
                violations.AddRange(forecastViolations.Select(f =>
                    $"PriceForecast {f.Id} has AsOfDate {f.AsOfDate:yyyy-MM-dd} after replay date {asOf:yyyy-MM-dd}."));
            }
        }

        var passed = violations.Count == 0;
        return new LeakageAuditResult
        {
            Passed = passed,
            InventoryLotsChecked = lotsForDay.Count,
            InventoryLotsViolating = lotViolations.Count,
            OrdersChecked = ordersForDay.Count,
            OrdersViolating = orderViolations.Count,
            ForecastRowsChecked = forecastChecked,
            ForecastRowsViolating = forecastViolationCount,
            Violations = violations,
            Statement = passed
                ? $"Leakage audit passed: {lotsForDay.Count} inventory lot row(s), {ordersForDay.Count} order row(s), and {forecastChecked} forecast row(s) checked; all effective dates are on or before {asOf:yyyy-MM-dd}."
                : $"Leakage audit FAILED: {violations.Count} row(s) reference data with an effective date after {asOf:yyyy-MM-dd}."
        };
    }

    private async Task<Guid?> ResolveSupplyModelVersionAsync(Guid generationId, CancellationToken ct)
    {
        var published = await _db.SupplyModelVersions
            .Where(m => m.GenerationId == generationId && m.LifecycleStatus == ModelLifecycleStatus.Published)
            .OrderByDescending(m => m.PublishedAt).FirstOrDefaultAsync(ct);
        if (published is not null) return published.Id;
        var latest = await _db.SupplyModelVersions
            .Where(m => m.GenerationId == generationId && m.LifecycleStatus != ModelLifecycleStatus.Retired)
            .OrderByDescending(m => m.TrainedAt).FirstOrDefaultAsync(ct);
        return latest?.Id;
    }

    private async Task<Guid?> ResolveDemandModelVersionAsync(Guid generationId, CancellationToken ct)
    {
        var published = await _db.DemandModelVersions
            .Where(m => m.GenerationId == generationId && m.LifecycleStatus == ModelLifecycleStatus.Published)
            .OrderByDescending(m => m.PublishedAt).FirstOrDefaultAsync(ct);
        if (published is not null) return published.Id;
        var latest = await _db.DemandModelVersions
            .Where(m => m.GenerationId == generationId && m.LifecycleStatus != ModelLifecycleStatus.Retired)
            .OrderByDescending(m => m.TrainedAt).FirstOrDefaultAsync(ct);
        return latest?.Id;
    }

    private async Task<Guid?> ResolvePriceModelVersionAsync(Guid generationId, CancellationToken ct)
    {
        var published = await _db.PriceModelVersions
            .Where(m => m.GenerationId == generationId && m.LifecycleStatus == ModelLifecycleStatus.Published)
            .OrderByDescending(m => m.PublishedAt).FirstOrDefaultAsync(ct);
        if (published is not null) return published.Id;
        var latest = await _db.PriceModelVersions
            .Where(m => m.GenerationId == generationId && m.LifecycleStatus != ModelLifecycleStatus.Retired)
            .OrderByDescending(m => m.TrainedAt).FirstOrDefaultAsync(ct);
        return latest?.Id;
    }

    private static ReplayRunSummary ToSummary(ReplayRun r, OptimizationRun run) => new(
        r.Id, r.GenerationId, r.AsOfDate, r.OptimizationRunId, r.PriceMode,
        r.SupplyModelVersionId, r.DemandModelVersionId, r.PriceModelVersionId,
        r.OptimizerVersion, r.CostingModelVersion, r.LeakagePassed, r.LeakageAuditJson,
        r.CreatedAt, r.DataClassification,
        run.Status.ToString(), run.ObjectiveValue,
        JsonDocument.Parse(run.UnservedDemandJson).RootElement.GetArrayLength(),
        JsonDocument.Parse(run.UnusedInventoryJson).RootElement.GetArrayLength());

    /// <summary>Baseline: for each available lot (deterministic order), fill the geographically
    /// nearest open order for the same product first. Ignores truck time windows and capacity —
    /// a documented proxy, not an alternate feasible optimizer.</summary>
    private BaselinePolicyResult RunNearestCustomerGreedy(AllocationInput input)
    {
        var facilities = input.Facilities.ToDictionary(f => f.Id);
        var customers = input.Customers.ToDictionary(c => c.Id);
        var products = input.Products.ToDictionary(p => p.Id);

        var lots = input.InventoryLots
            .Where(l => !DomainInvariants.IsExpired(l, input.AsOfDate) && l.Status == InventoryLotStatus.Available)
            .OrderBy(l => l.FacilityId).ThenBy(l => l.ProductId).ThenBy(l => l.Id)
            .Select(l => new MutableLotRow(l.FacilityId, l.ProductId, l.QuantityPounds))
            .ToList();
        var demand = input.Orders
            .Where(o => o.Status == OrderStatus.Open)
            .OrderBy(o => o.Id)
            .Select(o => new MutableOrderRow(o.Id, o.CustomerId, o.ProductId, o.RequestedQuantityPounds, o.OfferedPricePerPound))
            .ToList();

        decimal revenue = 0, cost = 0;
        var movementCount = 0;

        foreach (var lot in lots)
        {
            if (!facilities.TryGetValue(lot.FacilityId, out var facility)) continue;
            while (lot.Remaining > 0)
            {
                MutableOrderRow? nearest = null;
                var bestDistance = double.MaxValue;
                foreach (var candidate in demand)
                {
                    if (candidate.Remaining <= 0 || candidate.ProductId != lot.ProductId) continue;
                    if (!customers.TryGetValue(candidate.CustomerId, out var candidateCustomer)) continue;
                    var distance = (double)TransportCostCalculator.HaversineMiles(
                        (double)facility.Latitude, (double)facility.Longitude, (double)candidateCustomer.Latitude, (double)candidateCustomer.Longitude);
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    nearest = candidate;
                }
                if (nearest is null) break;

                var qty = DomainInvariants.Money(Math.Min(lot.Remaining, nearest.Remaining));
                if (qty <= 0) break;

                var customer = customers[nearest.CustomerId];
                var (rev, txCost) = PriceAndCost(facility, customer, products, lot.ProductId, input.Trucks, qty, nearest.OfferedPricePerPound);
                revenue += rev;
                cost += txCost;
                movementCount++;
                lot.Remaining -= qty;
                nearest.Remaining -= qty;
            }
        }

        var unservedCount = demand.Count(d => d.Remaining > 0);
        return new BaselinePolicyResult("NearestCustomerGreedy",
            DomainInvariants.Money(revenue - cost), DomainInvariants.Money(revenue), DomainInvariants.Money(cost),
            unservedCount, movementCount);
    }

    /// <summary>Baseline: fill open orders highest-offered-price-first from available inventory of
    /// the matching product. Ignores truck time windows and capacity — a documented proxy.</summary>
    private BaselinePolicyResult RunHighestPriceFirst(AllocationInput input)
    {
        var facilities = input.Facilities.ToDictionary(f => f.Id);
        var customers = input.Customers.ToDictionary(c => c.Id);
        var products = input.Products.ToDictionary(p => p.Id);

        var lots = input.InventoryLots
            .Where(l => !DomainInvariants.IsExpired(l, input.AsOfDate) && l.Status == InventoryLotStatus.Available)
            .OrderBy(l => l.FacilityId).ThenBy(l => l.Id)
            .Select(l => new MutableLotRow(l.FacilityId, l.ProductId, l.QuantityPounds))
            .ToList();

        var orders = input.Orders
            .Where(o => o.Status == OrderStatus.Open)
            .OrderByDescending(o => o.OfferedPricePerPound).ThenBy(o => o.Id)
            .ToList();

        decimal revenue = 0, cost = 0;
        var movementCount = 0;
        var unservedCount = 0;

        foreach (var order in orders)
        {
            if (!customers.TryGetValue(order.CustomerId, out var customer)) { unservedCount++; continue; }
            var remaining = order.RequestedQuantityPounds;
            foreach (var lot in lots.Where(l => l.ProductId == order.ProductId && l.Remaining > 0))
            {
                if (remaining <= 0) break;
                if (!facilities.TryGetValue(lot.FacilityId, out var facility)) continue;
                var qty = DomainInvariants.Money(Math.Min(remaining, lot.Remaining));
                if (qty <= 0) continue;

                var (rev, txCost) = PriceAndCost(facility, customer, products, order.ProductId, input.Trucks, qty, order.OfferedPricePerPound);
                revenue += rev;
                cost += txCost;
                movementCount++;
                lot.Remaining -= qty;
                remaining -= qty;
            }
            if (remaining > 0) unservedCount++;
        }

        return new BaselinePolicyResult("HighestPriceFirst",
            DomainInvariants.Money(revenue - cost), DomainInvariants.Money(revenue), DomainInvariants.Money(cost),
            unservedCount, movementCount);
    }

    private (decimal Revenue, decimal Cost) PriceAndCost(
        Facility facility, Customer customer, Dictionary<Guid, Product> products, Guid productId,
        IReadOnlyList<Truck> trucks, decimal qty, decimal unitPrice)
    {
        var truck = products.TryGetValue(productId, out var product)
            ? trucks.FirstOrDefault(t => DomainInvariants.TruckCompatible(t, product.Code))
            : null;
        var breakdown = _transport.Calculate(
            facility.Latitude, facility.Longitude, customer.Latitude, customer.Longitude,
            truck?.CostPerMile ?? 0m, truck?.CostPerHour ?? 0m, qty);
        return (DomainInvariants.Money(qty * unitPrice), breakdown.TotalEstimatedCost);
    }

    private sealed class MutableLotRow(Guid facilityId, Guid productId, decimal quantity)
    {
        public Guid FacilityId { get; } = facilityId;
        public Guid ProductId { get; } = productId;
        public decimal Remaining { get; set; } = quantity;
    }

    private sealed class MutableOrderRow(Guid orderId, Guid customerId, Guid productId, decimal quantity, decimal offeredPricePerPound)
    {
        public Guid OrderId { get; } = orderId;
        public Guid CustomerId { get; } = customerId;
        public Guid ProductId { get; } = productId;
        public decimal Remaining { get; set; } = quantity;
        public decimal OfferedPricePerPound { get; } = offeredPricePerPound;
    }
}
