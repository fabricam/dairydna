using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Ingestion;
using DairyDNA.DataIngestion;
using DairyDNA.Domain.Enums;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.UnitTests.Ingestion;

public class PublicDataImporterTests
{
    [Theory]
    [InlineData("fixture-dairy-prices", 10)]
    [InlineData("fixture-weather", 10)]
    [InlineData("fixture-fuel-prices", 10)]
    public async Task Fixture_imports_succeed(string sourceCode, int expectedRows)
    {
        await using var db = CreateDb();
        var importer = new PublicDataImporter(db);
        var run = await importer.ImportAsync(new ImportRunRequest { SourceCode = sourceCode });
        run.Status.Should().Be(ImportRunStatus.Completed);
        run.CanonicalRowCount.Should().Be(expectedRows);
        run.DataClassification.Should().Be("Public");
        run.PayloadChecksumSha256.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Malformed_fixture_is_quarantined_without_canonical_rows()
    {
        await using var db = CreateDb();
        var importer = new PublicDataImporter(db);
        var before = await db.PublicMarketPrices.CountAsync();
        var run = await importer.ImportAsync(new ImportRunRequest
        {
            SourceCode = "fixture-dairy-prices",
            FixtureFileName = "dairy-market-prices.malformed.json"
        });
        run.Status.Should().Be(ImportRunStatus.Failed);
        run.QuarantineCount.Should().BeGreaterThan(0);
        (await db.PublicMarketPrices.CountAsync()).Should().Be(before);
        (await importer.GetQuarantineAsync(run.Id)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Identical_checksum_reimport_is_idempotent()
    {
        await using var db = CreateDb();
        var importer = new PublicDataImporter(db);
        var first = await importer.ImportAsync(new ImportRunRequest { SourceCode = "fixture-dairy-prices" });
        var count = await db.PublicMarketPrices.CountAsync();
        var second = await importer.ImportAsync(new ImportRunRequest { SourceCode = "fixture-dairy-prices" });
        second.Status.Should().Be(ImportRunStatus.SkippedIdempotent);
        second.PriorSuccessfulRunId.Should().Be(first.Id);
        (await db.PublicMarketPrices.CountAsync()).Should().Be(count);
    }

    private static DairyDnaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DairyDnaDbContext(options);
    }
}
