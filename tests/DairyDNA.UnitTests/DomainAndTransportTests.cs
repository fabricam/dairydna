using DairyDNA.Application.Transport;
using DairyDNA.Application.Abstractions;
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
        result.CostingModelVersion.Should().Be("transport-cost-v2");
        result.EmptyReturnIncluded.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 0, 2, 0, 69.09, 433.81)]
    [InlineData(2, 0, 4, 0, 138.19, 807.63)]
    [InlineData(0, 1, 0, 2, 69.09, 433.81)]
    [InlineData(60, 1, 60, 2, 34.55, 246.90)]
    [InlineData(1, 0, 1, 0, 0, 60)]
    public void Calculates_known_lanes_to_two_decimal_places(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon,
        decimal expectedOneWayMiles,
        decimal expectedTotal)
    {
        var result = new TransportCostCalculator().Calculate(CreateRequest(originLat, originLon, destLat, destLon));

        result.OneWayMiles.Should().BeApproximately(expectedOneWayMiles, 0.01m);
        result.TotalEstimatedCost.Should().BeApproximately(expectedTotal, 0.01m);
        result.TotalEstimatedCost.Should().Be(result.FuelCost + result.OperatingCost);
    }

    [Fact]
    public void Is_deterministic_for_repeated_calls()
    {
        var calculator = new TransportCostCalculator();
        var request = CreateRequest(43m, -89m, 43.5m, -88.5m);
        var expected = calculator.Calculate(request);

        for (var i = 0; i < 10; i++)
            calculator.Calculate(request).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Rejects_missing_or_invalid_coordinates()
    {
        var calculator = new TransportCostCalculator();

        var missingDestination = () => calculator.Calculate(CreateRequest(43m, -89m, 0m, 0m));
        var invalidLatitude = () => calculator.Calculate(CreateRequest(91m, -89m, 43m, -88m));

        missingDestination.Should().Throw<ArgumentException>();
        invalidLatitude.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Higher_fuel_price_increases_fuel_cost()
    {
        var calculator = new TransportCostCalculator();
        var lowPrice = calculator.Calculate(CreateRequest(43m, -89m, 43.5m, -88.5m, fuelPrice: 3m));
        var highPrice = calculator.Calculate(CreateRequest(43m, -89m, 43.5m, -88.5m, fuelPrice: 5m));

        highPrice.FuelCost.Should().BeGreaterThan(lowPrice.FuelCost);
    }

    [Fact]
    public void Empty_return_policy_doubles_billed_miles()
    {
        var calculator = new TransportCostCalculator();
        var roundTrip = calculator.Calculate(CreateRequest(43m, -89m, 43.5m, -88.5m, includeEmptyReturn: true));
        var oneWay = calculator.Calculate(CreateRequest(43m, -89m, 43.5m, -88.5m, includeEmptyReturn: false));

        roundTrip.BilledMiles.Should().Be(oneWay.BilledMiles * 2m);
        roundTrip.EmptyReturnIncluded.Should().BeTrue();
        oneWay.EmptyReturnIncluded.Should().BeFalse();
    }

    [Fact]
    public void Rejects_incompatible_product()
    {
        var request = new TransportCostRequest
        {
            OriginLat = 43m,
            OriginLon = -89m,
            DestLat = 43.5m,
            DestLon = -88.5m,
            CostPerMile = 1.5m,
            CostPerHour = 60m,
            QuantityPounds = 10_000m,
            ProductCode = "CREAM",
            CompatibleProductCodes = "RAW_MILK"
        };

        var act = () => new TransportCostCalculator().Calculate(request);

        act.Should().Throw<ArgumentException>();
    }

    private static TransportCostRequest CreateRequest(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon,
        decimal? fuelPrice = null,
        bool? includeEmptyReturn = null) =>
        new()
        {
            OriginLat = originLat,
            OriginLon = originLon,
            DestLat = destLat,
            DestLon = destLon,
            CostPerMile = 1.5m,
            CostPerHour = 60m,
            QuantityPounds = 10_000m,
            FuelPricePerGallon = fuelPrice,
            IncludeEmptyReturn = includeEmptyReturn
        };
}
