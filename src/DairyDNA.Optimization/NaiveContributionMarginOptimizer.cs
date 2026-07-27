using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;

namespace DairyDNA.Optimization;

/// <summary>
/// Deterministic greedy contribution-margin allocator (temporary; Feature 009 OR-Tools is system of record).
/// </summary>
public sealed class NaiveContributionMarginOptimizer : IAllocationOptimizer
{
    public string Version => "naive-cm-v1";

    public AllocationResult Optimize(AllocationInput input, ITransportCostCalculator transportCostCalculator)
    {
        var products = input.Products.ToDictionary(x => x.Id);
        var facilities = input.Facilities.ToDictionary(x => x.Id);
        var customers = input.Customers.ToDictionary(x => x.Id);

        var remainingInventory = input.InventoryLots
            .Where(l => !DomainInvariants.IsExpired(l, input.AsOfDate) && l.Status == InventoryLotStatus.Available)
            .Select(l => new MutableLot(l))
            .ToList();

        var remainingDemand = input.Orders
            .Select(o => new MutableDemand(o))
            .ToList();

        var truckRemaining = input.Trucks
            .Where(t => t.Status == TruckStatus.Available)
            .ToDictionary(t => t.Id, t => t.MaximumCapacityPounds);

        var dayStart = new DateTimeOffset(input.AsOfDate.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc));

        var candidates = new List<ScoredCandidate>();

        foreach (var demand in remainingDemand)
        {
            if (!products.TryGetValue(demand.Order.ProductId, out var product)) continue;
            if (!customers.TryGetValue(demand.Order.CustomerId, out var customer)) continue;

            foreach (var lot in remainingInventory.Where(l => l.Lot.ProductId == demand.Order.ProductId && l.Remaining > 0))
            {
                if (!facilities.TryGetValue(lot.Lot.FacilityId, out var facility)) continue;

                foreach (var truck in input.Trucks.Where(t => t.Status == TruckStatus.Available))
                {
                    if (!DomainInvariants.TruckCompatible(truck, product.Code)) continue;
                    if (truckRemaining[truck.Id] <= 0) continue;

                    var maxQty = Math.Min(lot.Remaining, Math.Min(demand.Remaining, truckRemaining[truck.Id]));
                    if (maxQty < demand.Order.MinimumAcceptableQuantityPounds && demand.Remaining == demand.Order.RequestedQuantityPounds)
                    {
                        // still allow smaller partials later if some already filled; for first assignment require at least min if taking from full request
                    }

                    if (maxQty <= 0) continue;

                    // Try quantities: full max, and min acceptable if different
                    var qtyOptions = new HashSet<decimal> { DomainInvariants.Money(maxQty) };
                    if (demand.Order.MinimumAcceptableQuantityPounds <= maxQty)
                        qtyOptions.Add(DomainInvariants.Money(demand.Order.MinimumAcceptableQuantityPounds));

                    foreach (var qty in qtyOptions.Where(q => q > 0 && q <= maxQty))
                    {
                        var cost = transportCostCalculator.Calculate(
                            facility.Latitude, facility.Longitude,
                            customer.Latitude, customer.Longitude,
                            truck.CostPerMile, truck.CostPerHour, qty);

                        var travelHours = (double)((cost.DistanceMiles / 45m) + 1m);
                        var earliestDeparture = dayStart > truck.AvailableFrom ? dayStart : truck.AvailableFrom;
                        var earliestArrival = earliestDeparture.AddHours(travelHours);
                        if (earliestArrival > demand.Order.RequestedDeliveryEnd)
                            continue;

                        // Delay departure so arrival lands in the delivery window when possible
                        var arrival = demand.Order.RequestedDeliveryStart > earliestArrival
                            ? demand.Order.RequestedDeliveryStart
                            : earliestArrival;
                        if (arrival > demand.Order.RequestedDeliveryEnd)
                            continue;

                        var departure = arrival.AddHours(-travelHours);
                        if (departure < earliestDeparture || departure > truck.AvailableUntil)
                            continue;

                        var revenue = DomainInvariants.Money(qty * demand.Order.OfferedPricePerPound);
                        var margin = DomainInvariants.Money(revenue - cost.TotalEstimatedCost);
                        if (margin < 0) continue;

                        var explanation =
                            $"Assign {qty} lb {product.Code} from {facility.Name} to {customer.Name} via truck {truck.Id:N}. " +
                            $"Revenue {revenue:0.00}, transport {cost.TotalEstimatedCost:0.00} (fuel {cost.FuelCost:0.00}, operating {cost.OperatingCost:0.00}), margin {margin:0.00}. " +
                            $"Binding: delivery window [{demand.Order.RequestedDeliveryStart:u}–{demand.Order.RequestedDeliveryEnd:u}], truck capacity, non-negative margin. Assumptions: static spot price, single-leg, empty-return miles included.";

                        candidates.Add(new ScoredCandidate(
                            margin,
                            facility.Id,
                            customer.Id,
                            product.Id,
                            truck.Id,
                            demand.Order.Id,
                            qty,
                            demand.Order.OfferedPricePerPound,
                            revenue,
                            cost,
                            departure,
                            arrival,
                            explanation,
                            lot,
                            demand));
                    }
                }
            }
        }

        // Deterministic order: margin desc, then ids
        var ordered = candidates
            .OrderByDescending(c => c.Margin)
            .ThenBy(c => c.OriginFacilityId)
            .ThenBy(c => c.DestinationCustomerId)
            .ThenBy(c => c.ProductId)
            .ThenBy(c => c.TruckId)
            .ThenBy(c => c.OrderId)
            .ThenByDescending(c => c.Quantity)
            .ToList();

        var movements = new List<AllocationCandidateMovement>();
        var usedCandidateKeys = new HashSet<string>();

        foreach (var c in ordered)
        {
            if (c.Lot.Remaining <= 0 || c.Demand.Remaining <= 0) continue;
            if (truckRemaining[c.TruckId] <= 0) continue;

            var qty = Math.Min(c.Quantity, Math.Min(c.Lot.Remaining, Math.Min(c.Demand.Remaining, truckRemaining[c.TruckId])));
            if (qty <= 0) continue;

            // Respect minimum only when this would be the only fill starting from full request with qty below min
            if (c.Demand.Filled == 0 && qty < c.Demand.Order.MinimumAcceptableQuantityPounds && qty < c.Demand.Remaining)
            {
                if (Math.Min(c.Lot.Remaining, Math.Min(c.Demand.Remaining, truckRemaining[c.TruckId])) < c.Demand.Order.MinimumAcceptableQuantityPounds)
                    continue;
                qty = Math.Min(qty, c.Demand.Order.MinimumAcceptableQuantityPounds);
            }

            qty = DomainInvariants.Money(qty);
            if (qty <= 0) continue;

            // Recompute economics for final qty
            var facility = facilities[c.OriginFacilityId];
            var customer = customers[c.DestinationCustomerId];
            var truck = input.Trucks.First(t => t.Id == c.TruckId);
            var cost = transportCostCalculator.Calculate(
                facility.Latitude, facility.Longitude,
                customer.Latitude, customer.Longitude,
                truck.CostPerMile, truck.CostPerHour, qty);
            var revenue = DomainInvariants.Money(qty * c.UnitPrice);
            var margin = DomainInvariants.Money(revenue - cost.TotalEstimatedCost);
            if (margin < 0) continue;

            var key = $"{c.OriginFacilityId}:{c.DestinationCustomerId}:{c.ProductId}:{c.TruckId}:{c.OrderId}:{qty}";
            if (!usedCandidateKeys.Add(key)) continue;

            c.Lot.Remaining -= qty;
            c.Demand.Remaining -= qty;
            c.Demand.Filled += qty;
            truckRemaining[c.TruckId] -= qty;

            movements.Add(new AllocationCandidateMovement
            {
                OriginFacilityId = c.OriginFacilityId,
                DestinationCustomerId = c.DestinationCustomerId,
                ProductId = c.ProductId,
                TruckId = c.TruckId,
                OrderId = c.OrderId,
                QuantityPounds = qty,
                ExpectedUnitPrice = c.UnitPrice,
                ExpectedRevenue = revenue,
                TransportationCost = cost.TotalEstimatedCost,
                FuelCost = cost.FuelCost,
                OperatingCost = cost.OperatingCost,
                DistanceMiles = cost.DistanceMiles,
                ExpectedContributionMargin = margin,
                DepartureTime = c.Departure,
                ArrivalTime = c.Arrival,
                Explanation = c.Explanation
            });
        }

        // Independent feasibility validation
        foreach (var m in movements)
        {
            if (m.ExpectedContributionMargin < 0)
                return Failed("Negative margin movement produced.");
            if (m.QuantityPounds <= 0)
                return Failed("Non-positive quantity.");
        }

        var unused = remainingInventory
            .Where(l => l.Remaining > 0)
            .GroupBy(l => new { l.Lot.FacilityId, Code = products[l.Lot.ProductId].Code })
            .Select(g => new UnusedInventoryRow
            {
                FacilityId = g.Key.FacilityId,
                ProductCode = g.Key.Code,
                QuantityPounds = DomainInvariants.Money(g.Sum(x => x.Remaining))
            })
            .ToList();

        var unserved = remainingDemand
            .Where(d => d.Remaining > 0)
            .Select(d => new UnservedDemandRow
            {
                OrderId = d.Order.Id,
                RemainingQuantityPounds = DomainInvariants.Money(d.Remaining)
            })
            .ToList();

        var objective = DomainInvariants.Money(movements.Sum(m => m.ExpectedContributionMargin));

        // Always Feasible when constraints satisfied (including zero movements)
        return new AllocationResult
        {
            Status = OptimizationRunStatus.Feasible,
            ObjectiveValue = objective,
            Movements = movements,
            UnusedInventory = unused,
            UnservedDemand = unserved
        };
    }

    private static AllocationResult Failed(string message) => new()
    {
        Status = OptimizationRunStatus.Failed,
        FailureMessage = message
    };

    private sealed class MutableLot(InventoryLot lot)
    {
        public InventoryLot Lot { get; } = lot;
        public decimal Remaining { get; set; } = lot.QuantityPounds;
    }

    private sealed class MutableDemand(Order order)
    {
        public Order Order { get; } = order;
        public decimal Remaining { get; set; } = order.RequestedQuantityPounds;
        public decimal Filled { get; set; }
    }

    private sealed record ScoredCandidate(
        decimal Margin,
        Guid OriginFacilityId,
        Guid DestinationCustomerId,
        Guid ProductId,
        Guid TruckId,
        Guid OrderId,
        decimal Quantity,
        decimal UnitPrice,
        decimal Revenue,
        TransportCostBreakdown Cost,
        DateTimeOffset Departure,
        DateTimeOffset Arrival,
        string Explanation,
        MutableLot Lot,
        MutableDemand Demand);
}
