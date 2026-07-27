using DairyDNA.Application.Abstractions;
using DairyDNA.DataGenerator;
using DairyDNA.Domain.Entities;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.UnitTests.Generation;

public class ThinSliceGeneratorReproTests
{
    [Fact]
    public async Task Same_seed_produces_same_entity_counts()
    {
        var a = await GenerateAsync(104729);
        var b = await GenerateAsync(104729);
        a.counts.Should().BeEquivalentTo(b.counts);
        a.manifest.ConfigurationHash.Should().Be(b.manifest.ConfigurationHash);
    }

    private static async Task<(GenerationManifest manifest, Dictionary<string, int> counts)> GenerateAsync(int seed)
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DairyDnaDbContext(options);
        var generator = new ThinSliceGenerator(db);
        var manifest = await generator.GenerateAsync(new ThinSliceGenerationRequest { RandomSeed = seed });
        var counts = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(manifest.EntityCountsJson)!;
        return (manifest, counts);
    }
}
