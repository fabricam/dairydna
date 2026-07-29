using DairyDNA.Application.Abstractions;
using DairyDNA.DataGenerator;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DairyDNA.UnitTests.Generation;

public class ThinSliceGeneratorReproTests
{
    [Fact]
    public async Task Same_seed_produces_same_entity_counts()
    {
        var a = await GenerateThinAsync(104729);
        var b = await GenerateThinAsync(104729);
        a.counts.Should().BeEquivalentTo(b.counts);
        a.manifest.ConfigurationHash.Should().Be(b.manifest.ConfigurationHash);
        a.manifest.ProfileName.Should().Be(GenerationProfileCatalog.ThinSlice);
        a.manifest.GeneratorVersion.Should().Be(GenerationProfileCatalog.GeneratorVersion);
    }

    [Fact]
    public async Task Thin_slice_product_set_is_milk_and_cream_only()
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DairyDnaDbContext(options);
        var generator = new SyntheticDataGenerator(db);
        var manifest = await generator.GenerateAsync(new SyntheticGenerationRequest
        {
            ProfileName = GenerationProfileCatalog.ThinSlice,
            RandomSeed = 104729
        });
        manifest.Status.Should().Be(GenerationRunStatus.Completed);
        var products = await db.Products.Where(p => p.GenerationId == manifest.Id).Select(p => p.Code).ToListAsync();
        products.Should().BeEquivalentTo(["RAW_MILK", "CREAM"]);
        var counts = JsonSerializer.Deserialize<Dictionary<string, int>>(manifest.EntityCountsJson)!;
        counts["farms"].Should().Be(5);
        counts["facilities"].Should().Be(2);
        counts["customers"].Should().Be(5);
        counts["trucks"].Should().Be(3);
        counts["products"].Should().Be(2);
        counts["weatherObservations"].Should().BeGreaterThan(0);
        counts["shipments"].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Invalid_zero_farms_rejected_before_write()
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DairyDnaDbContext(options);
        var generator = new SyntheticDataGenerator(db);
        var act = async () => await generator.GenerateAsync(new SyntheticGenerationRequest
        {
            ProfileName = GenerationProfileCatalog.Custom,
            FarmCount = 0,
            FacilityCount = 1,
            CustomerCount = 1,
            TruckCount = 1
        });
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*FarmCount*");
        (await db.GenerationManifests.CountAsync()).Should().Be(0);
        (await db.Farms.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Validation_report_present_and_passed_on_thin_slice()
    {
        var (_, manifest) = await GenerateThinAsync(104729);
        var report = JsonSerializer.Deserialize<ValidationReport>(manifest.ValidationReportJson);
        report.Should().NotBeNull();
        report!.Passed.Should().BeTrue();
        report.Checks.Should().Contain(c => c.Name == "referential-lots" && c.Passed);
        report.SeasonalVariationDetected.Should().BeTrue();
    }

    [Fact]
    public async Task Custom_short_range_standard_product_set_completes()
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DairyDnaDbContext(options);
        var generator = new SyntheticDataGenerator(db);
        var manifest = await generator.GenerateAsync(new SyntheticGenerationRequest
        {
            ProfileName = GenerationProfileCatalog.Custom,
            RandomSeed = 42,
            FarmCount = 3,
            FacilityCount = 2,
            CustomerCount = 3,
            TruckCount = 2,
            ProductSet = "standard-six",
            StartDate = new DateOnly(2025, 12, 1),
            EndDate = new DateOnly(2025, 12, 29),
            DenseHistoryDays = 29,
            SparseCadenceDays = 1,
            MissingnessRate = 0.02m
        });
        manifest.Status.Should().Be(GenerationRunStatus.Completed);
        var productCount = await db.Products.CountAsync(p => p.GenerationId == manifest.Id);
        productCount.Should().Be(6);
    }

    private static async Task<(Dictionary<string, int> counts, GenerationManifest manifest)> GenerateThinAsync(int seed)
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DairyDnaDbContext(options);
        var generator = new ThinSliceGenerator(db);
        var manifest = await generator.GenerateAsync(new ThinSliceGenerationRequest { RandomSeed = seed });
        var counts = JsonSerializer.Deserialize<Dictionary<string, int>>(manifest.EntityCountsJson)!;
        return (counts, manifest);
    }
}
