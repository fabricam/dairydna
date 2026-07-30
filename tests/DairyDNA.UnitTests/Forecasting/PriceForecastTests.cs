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

public class PriceForecastTests
{
    [Fact]
    public void Feature_builder_excludes_post_as_of_prices()
    {
        var series = Enumerable.Range(0, 30).Select(i => new PriceFeatureBuilder.DailyPrice("RAW_MILK", "R1", new DateOnly(2025, 11, 1).AddDays(i), .2m + i / 1000m)).ToList();
        var asOf = new DateOnly(2025, 11, 20);
        var feature = PriceFeatureBuilder.TryBuildFeature(series.Append(new("RAW_MILK", "R1", asOf.AddDays(3), 999m)).ToList(), asOf.AddDays(7), asOf);
        feature.Should().NotBeNull();
        feature!.RollingMean7.Should().BeLessThan(1);
    }

    [Fact]
    public async Task Train_publishes_nonnegative_bounded_prices_and_bundle()
    {
        await using var db = CreateDb();
        var generation = await new SyntheticDataGenerator(db).GenerateAsync(new ThinSliceGenerationRequest { RandomSeed = 104729 });
        var service = new MlNetPriceForecastService(db);
        var model = await service.TrainAndPublishAsync(new PriceForecastRequest { GenerationId = generation.Id });
        model.Status.Should().BeOneOf(ForecastRunStatus.Completed, ForecastRunStatus.CompletedBelowBar);
        JsonSerializer.Deserialize<ForecastMetrics>(model.MetricsJson)!.BaselineWape7.Should().BeGreaterThan(0);
        var forecasts = await service.GetForecastsAsync(generation.Id);
        forecasts.Should().Contain(x => x.ProductCode == "RAW_MILK" && x.HorizonDays == 7);
        forecasts.Should().Contain(x => x.ProductCode == "CREAM");
        forecasts.Should().OnlyContain(x => x.LowerBoundPricePerPound >= 0 && x.UpperBoundPricePerPound >= x.PointEstimatePricePerPound && x.PointEstimatePricePerPound >= x.LowerBoundPricePerPound);
        (await service.GetOptimizationBundleAsync(generation.Id, generation.PlanningDate)).Items.Should().NotBeEmpty();
    }

    private static DairyDnaDbContext CreateDb() => new(new DbContextOptionsBuilder<DairyDnaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
