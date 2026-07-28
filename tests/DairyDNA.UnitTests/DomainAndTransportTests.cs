using DairyDNA.Application.Transport;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Rules;
using FluentAssertions;

namespace DairyDNA.UnitTests;

public class DomainInvariantTests
{
    [Fact]
    public void Rejects_negative_capacity()
    {
        var act = () => DomainInvariants.EnsureNonNegative(-1, "capacity");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rejects_expires_before_produced()
    {
        var produced = DateTimeOffset.UtcNow;
        var act = () => DomainInvariants.EnsureExpiresAfterProduced(produced, produced.AddHours(-1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Truck_compatibility_works()
    {
        var truck = new Truck { CompatibleProductCodes = "RAW_MILK,CREAM" };
        DomainInvariants.TruckCompatible(truck, "CREAM").Should().BeTrue();
        DomainInvariants.TruckCompatible(truck, "BUTTER").Should().BeFalse();
    }

    [Fact]
    public void Rejects_contract_end_before_start()
    {
        var act = () => DomainInvariants.EnsureContractDates(new DateOnly(2025, 12, 1), new DateOnly(2025, 1, 1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rejects_empty_name()
    {
        var act = () => DomainInvariants.EnsureNonEmptyName("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rejects_non_positive_order_quantity()
    {
        var order = new Order
        {
            RequestedQuantityPounds = 0,
            MinimumAcceptableQuantityPounds = 0,
            RequestedDeliveryStart = DateTimeOffset.UtcNow,
            RequestedDeliveryEnd = DateTimeOffset.UtcNow.AddHours(1)
        };
        var act = () => DomainInvariants.ValidateOrder(order);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rejects_incompatible_truck_product_assignment()
    {
        var truck = new Truck { CompatibleProductCodes = "RAW_MILK" };
        var act = () => DomainInvariants.EnsureTruckProductCompatible(truck, "CREAM");
        act.Should().Throw<ArgumentException>();
    }
}

public class TransportCostCalculatorTests
{
    [Fact]
    public void Produces_explainable_cost_breakdown()
    {
        var calc = new TransportCostCalculator();
        var result = calc.Calculate(43, -89, 43.5m, -88.5m, 1.5m, 60m, 10000m);
        result.DistanceMiles.Should().BeGreaterThan(0);
        result.FuelCost.Should().BeGreaterThan(0);
        result.OperatingCost.Should().BeGreaterThan(0);
        result.TotalEstimatedCost.Should().Be(result.FuelCost + result.OperatingCost);
    }
}
