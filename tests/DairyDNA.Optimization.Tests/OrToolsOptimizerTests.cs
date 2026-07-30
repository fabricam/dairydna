using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Transport;
using DairyDNA.Domain.Enums;
using DairyDNA.Optimization;
using FluentAssertions;

namespace DairyDNA.Optimization.Tests;

public class OrToolsOptimizerTests
{
    private readonly TransportCostCalculator _transport = new();
    private readonly OrToolsContributionMarginOptimizer _optimizer = new();

    [Fact]
    public void Default_version_is_ortools()
    {
        _optimizer.Version.Should().Be("ortools-cm-v1");
    }

    [Fact]
    public void Positive_margin_lane_ships()
    {
        var input = KnownAnswerFixtures.BuildSimplePublic(price: 2.0m, distanceCustomerLat: 43.1m);
        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        result.Movements.Should().NotBeEmpty();
        result.Movements.Should().OnlyContain(m => m.ExpectedContributionMargin >= 0);
        FeasibilityValidator.Validate(result.Movements, input).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Negative_margin_only_holds_inventory()
    {
        var input = KnownAnswerFixtures.BuildSimplePublic(price: 0.01m, distanceCustomerLat: 48m);
        var result = _optimizer.Optimize(input, _transport);
        result.Status.Should().Be(OptimizationRunStatus.Feasible);
        result.Movements.Should().BeEmpty();
    }

    [Fact]
    public void Reproducible_objective()
    {
        var input = KnownAnswerFixtures.BuildSimplePublic(price: 2.0m, distanceCustomerLat: 43.1m);
        var a = _optimizer.Optimize(input, _transport);
        var b = _optimizer.Optimize(input, _transport);
        a.ObjectiveValue.Should().Be(b.ObjectiveValue);
        a.Movements.Select(m => m.QuantityPounds).Should().Equal(b.Movements.Select(m => m.QuantityPounds));
    }

    [Fact]
    public void Resolver_defaults_to_ortools_and_can_select_naive()
    {
        var resolver = new AllocationOptimizerResolver(new OrToolsContributionMarginOptimizer(), new NaiveContributionMarginOptimizer());
        resolver.Resolve(null).Version.Should().Be("ortools-cm-v1");
        resolver.Resolve("naive-cm-v1").Version.Should().Be("naive-cm-v1");
    }
}
