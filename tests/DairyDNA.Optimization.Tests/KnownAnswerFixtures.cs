using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Transport;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Optimization;
using FluentAssertions;

namespace DairyDNA.Optimization.Tests;

public class KnownAnswerFixtures
{
    private readonly TransportCostCalculator _transport = new();
    private readonly NaiveContributionMarginOptimizer _optimizer = new();

    [Fact]
    public void One_origin_one_customer_ships_when_margin_positive()
    {
        var input = BuildSimple(price: 2.0m, distanceCustomerLat: 43.1m);
        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        result.Movements.Should().NotBeEmpty();
        result.Movements.Sum(m => m.QuantityPounds).Should().BeGreaterThan(0);
        result.Movements.Should().OnlyContain(m => m.ExpectedContributionMargin >= 0);
    }

    [Fact]
    public void Higher_distant_price_can_lose_to_nearer_lower_price()
    {
        var facilityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var nearId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var farId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var productId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var truckId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var day = new DateOnly(2025, 12, 29);

        var input = new AllocationInput
        {
            AsOfDate = day,
            Facilities =
            [
                new Facility { Id = facilityId, Name = "F1", Latitude = 43m, Longitude = -89m, Active = true }
            ],
            Products = [new Product { Id = productId, Code = "CREAM", Name = "Cream", MaximumAgeHours = 48 }],
            Customers =
            [
                new Customer { Id = nearId, Name = "Near", Latitude = 43.05m, Longitude = -89.05m, Active = true },
                new Customer { Id = farId, Name = "Far", Latitude = 48m, Longitude = -95m, Active = true }
            ],
            InventoryLots =
            [
                new InventoryLot
                {
                    Id = Guid.NewGuid(), FacilityId = facilityId, ProductId = productId, QuantityPounds = 10000,
                    ProducedAt = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    ExpiresAt = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(40),
                    Status = InventoryLotStatus.Available, AsOfDate = day
                }
            ],
            Orders =
            [
                new Order
                {
                    Id = Guid.NewGuid(), CustomerId = nearId, ProductId = productId, RequestedQuantityPounds = 5000,
                    MinimumAcceptableQuantityPounds = 1000,
                    RequestedDeliveryStart = day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                    RequestedDeliveryEnd = day.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc),
                    OfferedPricePerPound = 1.0m, Status = OrderStatus.Open, RequestDate = day
                },
                new Order
                {
                    Id = Guid.NewGuid(), CustomerId = farId, ProductId = productId, RequestedQuantityPounds = 5000,
                    MinimumAcceptableQuantityPounds = 1000,
                    RequestedDeliveryStart = day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                    RequestedDeliveryEnd = day.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc),
                    OfferedPricePerPound = 1.2m, Status = OrderStatus.Open, RequestDate = day
                }
            ],
            Trucks =
            [
                new Truck
                {
                    Id = truckId, MaximumCapacityPounds = 50000, CompatibleProductCodes = "CREAM",
                    CostPerMile = 2.5m, CostPerHour = 80m, Status = TruckStatus.Available,
                    AvailableFrom = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    AvailableUntil = day.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                }
            ]
        };

        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        result.Movements.Should().NotBeEmpty();
        // Prefer near customer due to transport; far high price should not dominate all volume
        result.Movements.Count(m => m.DestinationCustomerId == nearId).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Insufficient_capacity_remains_feasible_with_unused_or_unserved()
    {
        var input = BuildSimple(price: 5m, truckCapacity: 1000, inventory: 10000, demand: 10000);
        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        (result.UnusedInventory.Sum(x => x.QuantityPounds) + result.UnservedDemand.Sum(x => x.RemainingQuantityPounds))
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public void Negative_margin_only_holds_inventory()
    {
        var input = BuildSimple(price: 0.01m, costPerMile: 50m);
        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        result.Movements.Should().BeEmpty();
        result.UnusedInventory.Should().NotBeEmpty();
    }

    [Fact]
    public void Expired_inventory_excluded()
    {
        var input = BuildSimple(price: 5m, expired: true);
        var result = _optimizer.Optimize(input, _transport);
        result.Movements.Should().BeEmpty();
        result.UnusedInventory.Should().BeEmpty(); // expired filtered before unused aggregation of available lots
    }

    [Fact]
    public void Zero_demand_yields_no_movements()
    {
        var input = BuildSimple(price: 5m, withOrder: false);
        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        result.Movements.Should().BeEmpty();
        result.UnusedInventory.Should().NotBeEmpty();
    }

    [Fact]
    public void Reproducible_objective_and_quantities()
    {
        var input = BuildSimple(price: 2m);
        var a = _optimizer.Optimize(input, _transport);
        var b = _optimizer.Optimize(input, _transport);
        a.ObjectiveValue.Should().Be(b.ObjectiveValue);
        a.Movements.Select(m => m.QuantityPounds).Should().Equal(b.Movements.Select(m => m.QuantityPounds));
        for (var i = 0; i < a.Movements.Count; i++)
        {
            Math.Abs(a.Movements[i].TransportationCost - b.Movements[i].TransportationCost).Should().BeLessThanOrEqualTo(0.01m);
        }
    }

    public static AllocationInput BuildSimplePublic(decimal price, decimal distanceCustomerLat = 43.1m)
        => BuildSimple(price, distanceCustomerLat);

    private static AllocationInput BuildSimple(
        decimal price,
        decimal distanceCustomerLat = 43.1m,
        decimal truckCapacity = 50000,
        decimal inventory = 8000,
        decimal demand = 5000,
        decimal costPerMile = 1.25m,
        bool expired = false,
        bool withOrder = true)
    {
        var facilityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var truckId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var day = new DateOnly(2025, 12, 29);
        var produced = day.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc);
        var expires = expired
            ? new DateTimeOffset(day.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : produced.AddHours(40);

        return new AllocationInput
        {
            AsOfDate = day,
            Facilities = [new Facility { Id = facilityId, Name = "F", Latitude = 43m, Longitude = -89m, Active = true }],
            Products = [new Product { Id = productId, Code = "RAW_MILK", Name = "Milk", MaximumAgeHours = 72 }],
            Customers = [new Customer { Id = customerId, Name = "C", Latitude = distanceCustomerLat, Longitude = -89m, Active = true }],
            InventoryLots =
            [
                new InventoryLot
                {
                    Id = Guid.NewGuid(), FacilityId = facilityId, ProductId = productId, QuantityPounds = inventory,
                    ProducedAt = produced, ExpiresAt = expires, Status = InventoryLotStatus.Available, AsOfDate = day
                }
            ],
            Orders = withOrder
            ?
            [
                new Order
                {
                    Id = Guid.NewGuid(), CustomerId = customerId, ProductId = productId,
                    RequestedQuantityPounds = demand, MinimumAcceptableQuantityPounds = 1,
                    RequestedDeliveryStart = day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                    RequestedDeliveryEnd = day.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc),
                    OfferedPricePerPound = price, Status = OrderStatus.Open, RequestDate = day
                }
            ]
            : [],
            Trucks =
            [
                new Truck
                {
                    Id = truckId, MaximumCapacityPounds = truckCapacity, CompatibleProductCodes = "RAW_MILK,CREAM",
                    CostPerMile = costPerMile, CostPerHour = 55m, Status = TruckStatus.Available,
                    AvailableFrom = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    AvailableUntil = day.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                }
            ]
        };
    }
}
