using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Dashboard;

public sealed record AgeRiskBucket(string Band, int LotCount, decimal QuantityPounds, string RiskLevel);
public sealed record PricePoint(DateOnly Date, string ProductCode, decimal PricePerPound, string Classification);
public sealed record FacilityDetailDto(
    Guid Id,
    string Name,
    string FacilityType,
    string RegionCode,
    decimal Latitude,
    decimal Longitude,
    decimal MilkStorageCapacityPounds,
    decimal CreamStorageCapacityPounds,
    bool Active,
    IReadOnlyList<InventorySummaryRow> Inventory,
    string DataClassification);

// Reuse demo row shapes where practical
public sealed record InventorySummaryRow(Guid FacilityId, string FacilityName, string ProductCode, decimal QuantityPounds, DateTimeOffset? OldestExpiresAt, int? DaysToExpiry);
public sealed record DemandSummaryRow(Guid OrderId, string CustomerName, string ProductCode, decimal RequestedQuantityPounds, decimal OfferedPricePerPound);
public sealed record FleetSummaryRow(Guid TruckId, decimal MaximumCapacityPounds, string Status, bool Active);
public sealed record NetworkMapPoint(Guid Id, string Kind, string Name, decimal Latitude, decimal Longitude, bool Active);

public sealed record DashboardModel(
    Guid GenerationId,
    DateOnly AsOfDate,
    DateOnly DatasetStart,
    DateOnly DatasetEnd,
    string DataClassification,
    string? Warning,
    IReadOnlyList<InventorySummaryRow> Inventory,
    IReadOnlyList<DemandSummaryRow> Demand,
    IReadOnlyList<FleetSummaryRow> Fleet,
    IReadOnlyList<AgeRiskBucket> InventoryAgeRisk,
    IReadOnlyList<PricePoint> PriceSeries,
    IReadOnlyList<NetworkMapPoint> Network,
    int OmittedFromMapCount);

public enum DashboardQueryStatus { Ok, NotFound, BadRequest }

public sealed record DashboardQueryResult(DashboardQueryStatus Status, DashboardModel? Model, string? Error);

public sealed class GetDashboardHandler
{
    private readonly IDairyDnaDbContext _db;

    public GetDashboardHandler(IDairyDnaDbContext db) => _db = db;

    public async Task<(DashboardModel? Model, string? Error)> HandleAsync(
        Guid generationId,
        DateOnly? asOfDate,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var r = await HandleDetailedAsync(generationId, asOfDate, includeInactive, ct);
        return (r.Model, r.Error);
    }

    public async Task<DashboardQueryResult> HandleDetailedAsync(
        Guid generationId,
        DateOnly? asOfDate,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == generationId, ct);
        if (gen is null) return new(DashboardQueryStatus.NotFound, null, "Unknown dataset/generation id.");

        var day = asOfDate ?? gen.PlanningDate;
        if (day < gen.StartDate || day > gen.EndDate)
            return new(DashboardQueryStatus.BadRequest, null,
                $"As-of date {day:yyyy-MM-dd} is outside dataset range {gen.StartDate:yyyy-MM-dd}–{gen.EndDate:yyyy-MM-dd}.");

        var facilities = await _db.Facilities.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        var farms = await _db.Farms.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        var customers = await _db.Customers.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        var products = await _db.Products.Where(x => x.GenerationId == generationId).ToDictionaryAsync(x => x.Id, ct);
        var facilityMap = facilities.ToDictionary(x => x.Id);

        if (!includeInactive)
        {
            facilities = facilities.Where(f => f.Active).ToList();
            farms = farms.Where(f => f.Active).ToList();
            customers = customers.Where(c => c.Active).ToList();
        }

        var lots = await _db.InventoryLots
            .Where(x => x.GenerationId == generationId && x.AsOfDate == day && x.Status == InventoryLotStatus.Available)
            .ToListAsync(ct);

        var inventory = lots
            .Where(l => facilities.Any(f => f.Id == l.FacilityId))
            .GroupBy(x => new { x.FacilityId, x.ProductId })
            .Select(g =>
            {
                var oldest = g.Min(x => x.ExpiresAt);
                var days = (int)Math.Floor((oldest - day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).TotalDays);
                return new InventorySummaryRow(
                    g.Key.FacilityId,
                    facilityMap[g.Key.FacilityId].Name,
                    products[g.Key.ProductId].Code,
                    g.Sum(x => x.QuantityPounds),
                    oldest,
                    days);
            })
            .ToList();

        var ageBuckets = new[]
        {
            ("0–1d", 0, 1, "Critical"),
            ("2–3d", 2, 3, "High"),
            ("4–7d", 4, 7, "Moderate"),
            ("8d+", 8, int.MaxValue, "Lower")
        }.Select(b =>
        {
            var matching = lots.Where(l =>
            {
                var days = (int)Math.Floor((l.ExpiresAt - day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).TotalDays);
                return days >= b.Item2 && days <= b.Item3;
            }).ToList();
            return new AgeRiskBucket(b.Item1, matching.Count, matching.Sum(x => x.QuantityPounds), b.Item4);
        }).ToList();

        var customerMap = customers.ToDictionary(x => x.Id);
        var orders = await _db.Orders
            .Where(x => x.GenerationId == generationId && x.RequestDate == day && x.Status == OrderStatus.Open)
            .ToListAsync(ct);
        var demand = orders
            .Where(o => customerMap.ContainsKey(o.CustomerId))
            .Select(o => new DemandSummaryRow(
                o.Id, customerMap[o.CustomerId].Name, products[o.ProductId].Code,
                o.RequestedQuantityPounds, o.OfferedPricePerPound))
            .ToList();

        var trucks = await _db.Trucks.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        if (!includeInactive) trucks = trucks.Where(t => t.Active).ToList();
        var fleet = trucks.Select(t => new FleetSummaryRow(t.Id, t.MaximumCapacityPounds, t.Status.ToString(), t.Active)).ToList();

        var startWindow = day.AddDays(-14);
        var syntheticPrices = await _db.MarketPrices
            .Where(x => x.GenerationId == generationId && x.EffectiveDate >= startWindow && x.EffectiveDate <= day)
            .ToListAsync(ct);
        var priceSeries = syntheticPrices
            .Where(p => products.ContainsKey(p.ProductId))
            .Select(p => new PricePoint(p.EffectiveDate, products[p.ProductId].Code, p.PricePerPound, "Synthetic"))
            .ToList();

        var publicPrices = await _db.PublicMarketPrices
            .Where(x => x.EffectiveDate >= startWindow && x.EffectiveDate <= day)
            .ToListAsync(ct);
        priceSeries.AddRange(publicPrices.Select(p =>
            new PricePoint(p.EffectiveDate, p.ProductCode, p.PricePerPound, "Public")));
        priceSeries = priceSeries.OrderBy(p => p.Date).ThenBy(p => p.ProductCode).ToList();

        var candidates = farms.Select(f => new NetworkMapPoint(f.Id, "Farm", f.Name, f.Latitude, f.Longitude, f.Active))
            .Concat(facilities.Select(f => new NetworkMapPoint(f.Id, "Facility", f.Name, f.Latitude, f.Longitude, f.Active)))
            .Concat(customers.Select(c => new NetworkMapPoint(c.Id, "Customer", c.Name, c.Latitude, c.Longitude, c.Active)))
            .ToList();
        var mapped = candidates.Where(p => p.Latitude != 0 || p.Longitude != 0).ToList();
        var omitted = candidates.Count - mapped.Count;

        string? warning = omitted > 0 ? $"{omitted} entit(ies) omitted from map (missing coordinates)." : null;

        var model = new DashboardModel(
            generationId, day, gen.StartDate, gen.EndDate, "Synthetic", warning,
            inventory, demand, fleet, ageBuckets, priceSeries, mapped, omitted);
        return new(DashboardQueryStatus.Ok, model, null);
    }

    public async Task<FacilityDetailDto?> GetFacilityAsync(Guid generationId, Guid facilityId, DateOnly? asOfDate, CancellationToken ct = default)
    {
        var (model, _) = await HandleAsync(generationId, asOfDate, includeInactive: true, ct);
        if (model is null) return null;
        var facility = await _db.Facilities.FirstOrDefaultAsync(x => x.Id == facilityId && x.GenerationId == generationId, ct);
        if (facility is null) return null;
        var inv = model.Inventory.Where(i => i.FacilityId == facilityId).ToList();
        return new FacilityDetailDto(
            facility.Id, facility.Name, facility.FacilityType.ToString(), facility.RegionCode,
            facility.Latitude, facility.Longitude, facility.MilkStorageCapacityPounds, facility.CreamStorageCapacityPounds,
            facility.Active, inv, "Synthetic");
    }
}
