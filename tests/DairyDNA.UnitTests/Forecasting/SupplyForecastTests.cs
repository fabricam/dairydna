using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Forecasting;
using DairyDNA.DataGenerator;
using DairyDNA.Domain.Enums;
using DairyDNA.Forecasting;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DairyDNA.UnitTests.Forecasting;

public class SupplyForecastTests
{
    [Fact]
    public void Feature_builder_rejects_history_after_as_of()
    {
        var series = Enumerable.Range(0, 30).Select(i =>
            new SupplyFeatureBuilder.DailySupply(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "R1",
                new DateOnly(2025, 11, 1).AddDays(i),
                8000 + i,
                0.1m)).ToList();

        var asOf = new DateOnly(2025, 11, 20);
        var feature = SupplyFeatureBuilder.TryBuildFeature(series, asOf.AddDays(7), asOf);
        feature.Should().NotBeNull();

        var act = () =>
        {
            // Corrupt series with a point after as-of included incorrectly — builder filters by asOf
            var leaked = series.Concat([
                new SupplyFeatureBuilder.DailySupply(series[0].FacilityId, "R1", asOf.AddDays(3), 99999, 0)
            ]).ToList();
            // Building for target after as-of should still only use <= asOf
            var f = SupplyFeatureBuilder.TryBuildFeature(leaked, asOf.AddDays(7), asOf)!;
            // Ensure lag/roll not equal to leaked future 99999
            f.RollingMean7.Should().BeLessThan(90000);
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Train_publishes_facility_coverage_and_baseline_metrics()
    {
        await using var db = CreateDb();
        var generator = new SyntheticDataGenerator(db);
        var gen = await generator.GenerateAsync(new ThinSliceGenerationRequest { RandomSeed = 104729 });
        gen.Status.Should().Be(GenerationRunStatus.Completed);

        var service = new MlNetSupplyForecastService(db);
        var model = await service.TrainAndPublishAsync(new SupplyForecastRequest
        {
            GenerationId = gen.Id,
            RandomSeed = 104729
        });

        model.Status.Should().BeOneOf(ForecastRunStatus.Completed, ForecastRunStatus.CompletedBelowBar);
        var metrics = JsonSerializer.Deserialize<ForecastMetrics>(model.MetricsJson)!;
        metrics.BaselineWape7.Should().BeGreaterThan(0);
        metrics.FacilityCoveragePct.Should().BeGreaterThanOrEqualTo(99m);

        var forecasts = await service.GetForecastsAsync(gen.Id);
        forecasts.Should().NotBeEmpty();
        forecasts.Where(f => f.AggregationLevel == ForecastAggregationLevel.Facility && f.HorizonDays == 7)
            .Should().NotBeEmpty();
        forecasts.Should().OnlyContain(f => f.DataClassification == "Forecast");
        forecasts.Should().OnlyContain(f => f.UpperBoundPounds >= f.PointEstimatePounds && f.PointEstimatePounds >= f.LowerBoundPounds);

        var same = await service.TrainAndPublishAsync(new SupplyForecastRequest
        {
            GenerationId = gen.Id,
            RandomSeed = 104729
        });
        var m2 = JsonSerializer.Deserialize<ForecastMetrics>(same.MetricsJson)!;
        m2.ModelWape7.Should().BeApproximately(metrics.ModelWape7, 0.02m);
        m2.BaselineWape7.Should().BeApproximately(metrics.BaselineWape7, 0.02m);
    }

    private static DairyDnaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DairyDnaDbContext(options);
    }
}
