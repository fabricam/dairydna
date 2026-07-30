using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Forecasting;
using DairyDNA.DataGenerator;
using DairyDNA.Domain.Enums;
using DairyDNA.Forecasting;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.UnitTests.Forecasting;

public class DemandForecastTests
{
    [Fact]
    public void Feature_builder_excludes_post_as_of_orders()
    {
        var customer = Guid.NewGuid();
        var series = Enumerable.Range(0, 30).Select(i => new DemandFeatureBuilder.DailyDemand(customer, "R1", "RAW_MILK", new DateOnly(2025, 11, 1).AddDays(i), 1000 + i)).ToList();
        var asOf = new DateOnly(2025, 11, 20);
        var feature = DemandFeatureBuilder.TryBuildFeature(series.Append(new(customer, "R1", "RAW_MILK", asOf.AddDays(3), 99999)).ToList(), asOf.AddDays(7), asOf);
        feature.Should().NotBeNull();
        feature!.RollingMean7.Should().BeLessThan(90000);
    }

    [Fact]
    public async Task Train_publishes_customer_and_region_forecasts()
    {
        await using var db = CreateDb();
        var gen = await new SyntheticDataGenerator(db).GenerateAsync(new ThinSliceGenerationRequest { RandomSeed = 104729 });
        var service = new MlNetDemandForecastService(db);
        var model = await service.TrainAndPublishAsync(new DemandForecastRequest { GenerationId = gen.Id });
        model.Status.Should().BeOneOf(ForecastRunStatus.Completed, ForecastRunStatus.CompletedBelowBar);
        JsonSerializer.Deserialize<ForecastMetrics>(model.MetricsJson)!.BaselineWape7.Should().BeGreaterThan(0);
        var forecasts = await service.GetForecastsAsync(gen.Id);
        forecasts.Should().NotBeEmpty();
        forecasts.Should().Contain(x => x.AggregationLevel == ForecastAggregationLevel.Customer && x.HorizonDays == 7);
        forecasts.Should().Contain(x => x.AggregationLevel == ForecastAggregationLevel.Region);
        forecasts.Should().OnlyContain(x => x.UpperBoundPounds >= x.PointEstimatePounds && x.PointEstimatePounds >= x.LowerBoundPounds);
    }

    private static DairyDnaDbContext CreateDb() => new(new DbContextOptionsBuilder<DairyDnaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
