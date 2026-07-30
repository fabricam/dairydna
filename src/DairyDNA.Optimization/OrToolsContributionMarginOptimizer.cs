using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;
using Google.OrTools.LinearSolver;

namespace DairyDNA.Optimization;

/// <summary>
/// Contribution-margin MIP allocator using Google OR-Tools (CBC). System of record for Feature 009.
/// </summary>
public sealed class OrToolsContributionMarginOptimizer : IAllocationOptimizer
{
    public const string VersionId = "ortools-cm-v1";
    public string Version => VersionId;

    public AllocationResult Optimize(AllocationInput input, ITransportCostCalculator transportCostCalculator)
    {
        var products = input.Products.ToDictionary(x => x.Id);
        var facilities = input.Facilities.ToDictionary(x => x.Id);
        var customers = input.Customers.ToDictionary(x => x.Id);
        var trucks = input.Trucks.Where(t => t.Status == TruckStatus.Available).ToDictionary(t => t.Id);

        var inventory = input.InventoryLots
            .Where(l => !DomainInvariants.IsExpired(l, input.AsOfDate) && l.Status == InventoryLotStatus.Available)
            .GroupBy(l => (l.FacilityId, l.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityPounds));

        var dayStart = new DateTimeOffset(input.AsOfDate.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc));
        var candidates = new List<LaneCandidate>();

        foreach (var order in input.Orders.Where(o => o.Status == OrderStatus.Open))
        {
            if (!products.TryGetValue(order.ProductId, out var product)) continue;
            if (!customers.TryGetValue(order.CustomerId, out var customer)) continue;

            foreach (var inv in inventory)
            {
                if (inv.Key.ProductId != order.ProductId || inv.Value <= 0) continue;
                if (!facilities.TryGetValue(inv.Key.FacilityId, out var facility)) continue;

                foreach (var truck in trucks.Values)
                {
                    if (!DomainInvariants.TruckCompatible(truck, product.Code)) continue;

                    var maxQty = DomainInvariants.Money(Math.Min(inv.Value, Math.Min(order.RequestedQuantityPounds, truck.MaximumCapacityPounds)));
                    if (maxQty <= 0) continue;
                    if (maxQty < order.MinimumAcceptableQuantityPounds) continue;

                    var cost = transportCostCalculator.Calculate(
                        facility.Latitude, facility.Longitude,
                        customer.Latitude, customer.Longitude,
                        truck.CostPerMile, truck.CostPerHour, maxQty);

                    var travelHours = (double)((cost.DistanceMiles / 45m) + 1m);
                    var earliestDeparture = dayStart > truck.AvailableFrom ? dayStart : truck.AvailableFrom;
                    var earliestArrival = earliestDeparture.AddHours(travelHours);
                    if (earliestArrival > order.RequestedDeliveryEnd) continue;

                    var arrival = order.RequestedDeliveryStart > earliestArrival
                        ? order.RequestedDeliveryStart
                        : earliestArrival;
                    if (arrival > order.RequestedDeliveryEnd) continue;
                    var departure = arrival.AddHours(-travelHours);
                    if (departure < earliestDeparture || departure > truck.AvailableUntil) continue;

                    var maxRevenue = DomainInvariants.Money(maxQty * order.OfferedPricePerPound);
                    var maxMargin = DomainInvariants.Money(maxRevenue - cost.TotalEstimatedCost);
                    if (maxMargin < 0) continue; // hold inventory rather than lose money

                    candidates.Add(new LaneCandidate(
                        inv.Key.FacilityId, order.CustomerId, order.ProductId, truck.Id, order.Id,
                        maxQty, order.MinimumAcceptableQuantityPounds, order.OfferedPricePerPound,
                        cost, departure, arrival, facility.Name, customer.Name, product.Code));
                }
            }
        }

        if (candidates.Count == 0)
        {
            return BuildEmptyFeasible(inventory, products, input.Orders);
        }

        var solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING")
                     ?? Solver.CreateSolver("SCIP_MIXED_INTEGER_PROGRAMMING")
                     ?? Solver.CreateSolver("GLOP_LINEAR_PROGRAMMING");
        if (solver is null)
        {
            return new AllocationResult
            {
                Status = OptimizationRunStatus.Failed,
                FailureMessage = "OR-Tools solver could not be created."
            };
        }

        var useVars = new Variable[candidates.Count];
        var qtyVars = new Variable[candidates.Count];
        var objective = solver.Objective();
        objective.SetMaximization();

        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            useVars[i] = solver.MakeBoolVar($"use_{i}");
            qtyVars[i] = solver.MakeNumVar(0, (double)c.MaxQty, $"qty_{i}");
            // qty <= max * use
            var link = solver.MakeConstraint(0, 0, $"link_{i}");
            link.SetCoefficient(qtyVars[i], 1);
            link.SetCoefficient(useVars[i], -(double)c.MaxQty);
            // qty >= min * use (partial fills allowed only down to minimum when shipping)
            var minLink = solver.MakeConstraint(0, double.PositiveInfinity, $"min_{i}");
            minLink.SetCoefficient(qtyVars[i], 1);
            minLink.SetCoefficient(useVars[i], -(double)c.MinQty);

            objective.SetCoefficient(qtyVars[i], (double)c.UnitPrice);
            objective.SetCoefficient(useVars[i], -(double)c.TripCost.TotalEstimatedCost);
        }

        // Inventory constraints
        foreach (var group in candidates.Select((c, i) => (c, i)).GroupBy(x => (x.c.FacilityId, x.c.ProductId)))
        {
            var available = (double)inventory[(group.Key.FacilityId, group.Key.ProductId)];
            var row = solver.MakeConstraint(0, available, $"inv_{group.Key.FacilityId:N}_{group.Key.ProductId:N}");
            foreach (var item in group)
                row.SetCoefficient(qtyVars[item.i], 1);
        }

        // Order constraints
        foreach (var group in candidates.Select((c, i) => (c, i)).GroupBy(x => x.c.OrderId))
        {
            var order = input.Orders.First(o => o.Id == group.Key);
            var row = solver.MakeConstraint(0, (double)order.RequestedQuantityPounds, $"ord_{group.Key:N}");
            foreach (var item in group)
                row.SetCoefficient(qtyVars[item.i], 1);
        }

        // Truck capacity
        foreach (var group in candidates.Select((c, i) => (c, i)).GroupBy(x => x.c.TruckId))
        {
            var truck = trucks[group.Key];
            var row = solver.MakeConstraint(0, (double)truck.MaximumCapacityPounds, $"trk_{group.Key:N}");
            foreach (var item in group)
                row.SetCoefficient(qtyVars[item.i], 1);
        }

        // One active lane per truck (single-leg demo) — softens over-assignment of trip costs
        foreach (var group in candidates.Select((c, i) => (c, i)).GroupBy(x => x.c.TruckId))
        {
            var row = solver.MakeConstraint(0, 1, $"trk_use_{group.Key:N}");
            foreach (var item in group)
                row.SetCoefficient(useVars[item.i], 1);
        }

        solver.SetTimeLimit(20_000); // ms
        var status = solver.Solve();
        if (status is not (Solver.ResultStatus.OPTIMAL or Solver.ResultStatus.FEASIBLE))
        {
            return new AllocationResult
            {
                Status = OptimizationRunStatus.Failed,
                FailureMessage = $"OR-Tools solve status: {status}"
            };
        }

        var movements = new List<AllocationCandidateMovement>();
        var remainingInv = inventory.ToDictionary(x => x.Key, x => x.Value);
        var remainingDemand = input.Orders.ToDictionary(o => o.Id, o => o.RequestedQuantityPounds);
        var remainingTruck = trucks.ToDictionary(t => t.Key, t => t.Value.MaximumCapacityPounds);

        for (var i = 0; i < candidates.Count; i++)
        {
            if (useVars[i].SolutionValue() < 0.5) continue;
            var qty = DomainInvariants.Money((decimal)qtyVars[i].SolutionValue());
            if (qty <= 0) continue;
            var c = candidates[i];

            // Recompute economics for solved qty (costing is trip-based)
            var cost = transportCostCalculator.Calculate(
                facilities[c.FacilityId].Latitude, facilities[c.FacilityId].Longitude,
                customers[c.CustomerId].Latitude, customers[c.CustomerId].Longitude,
                trucks[c.TruckId].CostPerMile, trucks[c.TruckId].CostPerHour, qty);
            var revenue = DomainInvariants.Money(qty * c.UnitPrice);
            var margin = DomainInvariants.Money(revenue - cost.TotalEstimatedCost);
            if (margin < 0) continue;

            remainingInv[(c.FacilityId, c.ProductId)] -= qty;
            remainingDemand[c.OrderId] -= qty;
            remainingTruck[c.TruckId] -= qty;

            movements.Add(new AllocationCandidateMovement
            {
                OriginFacilityId = c.FacilityId,
                DestinationCustomerId = c.CustomerId,
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
                Explanation =
                    $"OR-Tools {VersionId}: ship {qty} lb {c.ProductCode} from {c.FacilityName} to {c.CustomerName}. " +
                    $"Revenue {revenue:0.00}, transport {cost.TotalEstimatedCost:0.00} (fuel {cost.FuelCost:0.00}, operating {cost.OperatingCost:0.00}, {cost.CostingModelVersion}), margin {margin:0.00}. " +
                    $"Assumptions: {cost.Assumptions}; single-leg; hold negative-margin inventory; soft min fill when used."
            });
        }

        var validation = FeasibilityValidator.Validate(movements, input);
        if (!validation.IsValid)
        {
            return new AllocationResult
            {
                Status = OptimizationRunStatus.Failed,
                FailureMessage = validation.Message,
                Movements = []
            };
        }

        return BuildResult(movements, remainingInv, products, remainingDemand);
    }

    private static AllocationResult BuildEmptyFeasible(
        Dictionary<(Guid FacilityId, Guid ProductId), decimal> inventory,
        Dictionary<Guid, Product> products,
        IReadOnlyList<Order> orders)
    {
        var remainingDemand = orders.ToDictionary(o => o.Id, o => o.RequestedQuantityPounds);
        return BuildResult([], inventory, products, remainingDemand);
    }

    private static AllocationResult BuildResult(
        List<AllocationCandidateMovement> movements,
        Dictionary<(Guid FacilityId, Guid ProductId), decimal> remainingInv,
        Dictionary<Guid, Product> products,
        Dictionary<Guid, decimal> remainingDemand)
    {
        var unused = remainingInv
            .Where(kv => kv.Value > 0)
            .Select(kv => new UnusedInventoryRow
            {
                FacilityId = kv.Key.FacilityId,
                ProductCode = products[kv.Key.ProductId].Code,
                QuantityPounds = DomainInvariants.Money(kv.Value)
            })
            .ToList();

        var unserved = remainingDemand
            .Where(kv => kv.Value > 0)
            .Select(kv => new UnservedDemandRow
            {
                OrderId = kv.Key,
                RemainingQuantityPounds = DomainInvariants.Money(kv.Value)
            })
            .ToList();

        return new AllocationResult
        {
            Status = OptimizationRunStatus.Feasible,
            ObjectiveValue = DomainInvariants.Money(movements.Sum(m => m.ExpectedContributionMargin)),
            Movements = movements,
            UnusedInventory = unused,
            UnservedDemand = unserved
        };
    }

    private sealed record LaneCandidate(
        Guid FacilityId,
        Guid CustomerId,
        Guid ProductId,
        Guid TruckId,
        Guid OrderId,
        decimal MaxQty,
        decimal MinQty,
        decimal UnitPrice,
        TransportCostBreakdown TripCost,
        DateTimeOffset Departure,
        DateTimeOffset Arrival,
        string FacilityName,
        string CustomerName,
        string ProductCode);
}

public static class FeasibilityValidator
{
    public sealed record Result(bool IsValid, string Message);

    public static Result Validate(IReadOnlyList<AllocationCandidateMovement> movements, AllocationInput input)
    {
        if (movements.Any(m => m.ExpectedContributionMargin < 0))
            return new(false, "Validator rejected negative-margin movement.");
        if (movements.Any(m => m.QuantityPounds <= 0))
            return new(false, "Validator rejected non-positive quantity.");

        var inv = input.InventoryLots
            .Where(l => !DomainInvariants.IsExpired(l, input.AsOfDate) && l.Status == InventoryLotStatus.Available)
            .GroupBy(l => (l.FacilityId, l.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityPounds));

        foreach (var g in movements.GroupBy(m => (m.OriginFacilityId, m.ProductId)))
        {
            if (!inv.TryGetValue(g.Key, out var avail) || g.Sum(x => x.QuantityPounds) > avail + 0.01m)
                return new(false, "Validator: inventory exceeded.");
        }

        foreach (var g in movements.GroupBy(m => m.OrderId))
        {
            var order = input.Orders.First(o => o.Id == g.Key);
            if (g.Sum(x => x.QuantityPounds) > order.RequestedQuantityPounds + 0.01m)
                return new(false, "Validator: order quantity exceeded.");
        }

        foreach (var g in movements.GroupBy(m => m.TruckId))
        {
            var truck = input.Trucks.First(t => t.Id == g.Key);
            if (g.Sum(x => x.QuantityPounds) > truck.MaximumCapacityPounds + 0.01m)
                return new(false, "Validator: truck capacity exceeded.");
        }

        return new(true, "ok");
    }
}
