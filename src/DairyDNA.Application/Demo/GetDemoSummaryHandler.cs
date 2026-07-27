using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Demo;

public sealed record InventorySummaryRow(Guid FacilityId, string FacilityName, string ProductCode, decimal QuantityPounds, DateTimeOffset? OldestExpiresAt);
public sealed record DemandSummaryRow(Guid OrderId, string CustomerName, string ProductCode, decimal RequestedQuantityPounds, decimal OfferedPricePerPound);
public sealed record PriceSummaryRow(string ProductCode, decimal PricePerPound, string PriceType);
public sealed record TruckSummaryRow(Guid TruckId, decimal MaximumCapacityPounds, string Status);

public sealed record DemoSummary(
    Guid GenerationId,
    DateOnly AsOfDate,
    string DataClassification,
    IReadOnlyList<InventorySummaryRow> Inventory,
    IReadOnlyList<DemandSummaryRow> Demand,
    IReadOnlyList<PriceSummaryRow> Prices,
    IReadOnlyList<TruckSummaryRow> Fleet);

public sealed class GetDemoSummaryHandler
{
    private readonly IDairyDnaDbContext _db;

    public GetDemoSummaryHandler(IDairyDnaDbContext db) => _db = db;

    public async Task<DemoSummary?> HandleAsync(Guid generationId, DateOnly? asOfDate, CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == generationId, ct);
        if (gen is null) return null;
        var day = asOfDate ?? gen.PlanningDate;

        var facilities = await _db.Facilities.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        var products = await _db.Products.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        var productMap = products.ToDictionary(x => x.Id);
        var facilityMap = facilities.ToDictionary(x => x.Id);

        var lots = await _db.InventoryLots.Where(x => x.GenerationId == generationId && x.AsOfDate == day).ToListAsync(ct);
        var inventory = lots
            .GroupBy(x => new { x.FacilityId, x.ProductId })
            .Select(g =>
            {
                var product = productMap[g.Key.ProductId];
                var facility = facilityMap[g.Key.FacilityId];
                return new InventorySummaryRow(
                    g.Key.FacilityId,
                    facility.Name,
                    product.Code,
                    g.Sum(x => x.QuantityPounds),
                    g.Min(x => x.ExpiresAt));
            })
            .ToList();

        var customers = await _db.Customers.Where(x => x.GenerationId == generationId).ToDictionaryAsync(x => x.Id, ct);
        var orders = await _db.Orders.Where(x => x.GenerationId == generationId && x.RequestDate == day && x.Status == OrderStatus.Open).ToListAsync(ct);
        var demand = orders.Select(o => new DemandSummaryRow(
            o.Id,
            customers[o.CustomerId].Name,
            productMap[o.ProductId].Code,
            o.RequestedQuantityPounds,
            o.OfferedPricePerPound)).ToList();

        var prices = await _db.MarketPrices.Where(x => x.GenerationId == generationId && x.EffectiveDate == day).ToListAsync(ct);
        var priceRows = prices.Select(p => new PriceSummaryRow(productMap[p.ProductId].Code, p.PricePerPound, p.PriceType.ToString())).ToList();

        var trucks = await _db.Trucks.Where(x => x.GenerationId == generationId).ToListAsync(ct);
        var fleet = trucks.Select(t => new TruckSummaryRow(t.Id, t.MaximumCapacityPounds, t.Status.ToString())).ToList();

        return new DemoSummary(generationId, day, "Synthetic", inventory, demand, priceRows, fleet);
    }
}
