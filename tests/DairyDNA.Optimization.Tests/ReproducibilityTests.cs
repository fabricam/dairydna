using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Transport;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Optimization;
using FluentAssertions;

namespace DairyDNA.Optimization.Tests;

public class ReproducibilityTests
{
    [Fact]
    public void Same_input_yields_identical_objective_quantities_and_costs_within_tolerance()
    {
        var transport = new TransportCostCalculator();
        var optimizer = new NaiveContributionMarginOptimizer();
        var input = BuildInput();

        var a = optimizer.Optimize(input, transport);
        var b = optimizer.Optimize(input, transport);

        a.ObjectiveValue.Should().Be(b.ObjectiveValue);
        a.Movements.Select(m => m.QuantityPounds).Should().Equal(b.Movements.Select(m => m.QuantityPounds));
        for (var i = 0; i < a.Movements.Count; i++)
        {
            Math.Abs(a.Movements[i].TransportationCost - b.Movements[i].TransportationCost).Should().BeLessThanOrEqualTo(0.01m);
            Math.Abs(a.Movements[i].ExpectedContributionMargin - b.Movements[i].ExpectedContributionMargin).Should().BeLessThanOrEqualTo(0.01m);
        }
    }

    private static AllocationInput BuildInput()
    {
        var facilityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var truckId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var day = new DateOnly(2025, 12, 29);
        var produced = day.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc);

        return new AllocationInput
        {
            AsOfDate = day,
            Facilities = [new Facility { Id = facilityId, Name = "F", Latitude = 43m, Longitude = -89m, Active = true }],
            Products = [new Product { Id = productId, Code = "RAW_MILK", Name = "Milk", MaximumAgeHours = 72 }],
            Customers = [new Customer { Id = customerId, Name = "C", Latitude = 43.1m, Longitude = -89m, Active = true }],
            InventoryLots =
            [
                new InventoryLot
                {
                    Id = Guid.NewGuid(), FacilityId = facilityId, ProductId = productId, QuantityPounds = 8000,
                    ProducedAt = produced, ExpiresAt = produced.AddHours(40), Status = InventoryLotStatus.Available, AsOfDate = day
                }
            ],
            Orders =
            [
                new Order
                {
                    Id = Guid.NewGuid(), CustomerId = customerId, ProductId = productId,
                    RequestedQuantityPounds = 5000, MinimumAcceptableQuantityPounds = 1,
                    RequestedDeliveryStart = day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                    RequestedDeliveryEnd = day.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc),
                    OfferedPricePerPound = 2m, Status = OrderStatus.Open, RequestDate = day
                }
            ],
            Trucks =
            [
                new Truck
                {
                    Id = truckId, MaximumCapacityPounds = 50000, CompatibleProductCodes = "RAW_MILK,CREAM",
                    CostPerMile = 1.25m, CostPerHour = 55m, Status = TruckStatus.Available,
                    AvailableFrom = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    AvailableUntil = day.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                }
            ]
        };
    }
}
