using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Application.Ingestion;

public sealed class ImportRunRequest
{
    public string SourceCode { get; set; } = string.Empty;
    /// <summary>Optional override path or fixture file name; defaults to source catalog fixture.</summary>
    public string? FixtureFileName { get; set; }
    /// <summary>Optional inline JSON payload (tests / custom); when set, fixture file is not read.</summary>
    public string? InlinePayloadJson { get; set; }
}

public interface IPublicDataImporter
{
    Task EnsureSourcesSeededAsync(CancellationToken cancellationToken = default);
    Task<ImportRun> ImportAsync(ImportRunRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportRun>> ListRunsAsync(CancellationToken cancellationToken = default);
    Task<ImportRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuarantineItem>> GetQuarantineAsync(Guid importRunId, CancellationToken cancellationToken = default);
}

public static class ImportSourceCatalog
{
    public const string SchemaVersion = "dairydna.public.v1";

    public static IReadOnlyList<ImportSource> Defaults { get; } =
    [
        new ImportSource
        {
            Id = Guid.Parse("aaaaaaaa-0003-4000-8000-000000000001"),
            Code = "fixture-dairy-prices",
            DisplayName = "Fixture dairy market prices",
            SeriesKind = ImportSeriesKind.DairyMarketPrice,
            SchemaVersion = SchemaVersion,
            FixtureFileName = "dairy-market-prices.json",
            Active = true
        },
        new ImportSource
        {
            Id = Guid.Parse("aaaaaaaa-0003-4000-8000-000000000002"),
            Code = "fixture-weather",
            DisplayName = "Fixture regional weather",
            SeriesKind = ImportSeriesKind.Weather,
            SchemaVersion = SchemaVersion,
            FixtureFileName = "weather.json",
            Active = true
        },
        new ImportSource
        {
            Id = Guid.Parse("aaaaaaaa-0003-4000-8000-000000000003"),
            Code = "fixture-fuel-prices",
            DisplayName = "Fixture weekly fuel prices",
            SeriesKind = ImportSeriesKind.FuelPrice,
            SchemaVersion = SchemaVersion,
            FixtureFileName = "fuel-prices.json",
            Active = true
        }
    ];
}
