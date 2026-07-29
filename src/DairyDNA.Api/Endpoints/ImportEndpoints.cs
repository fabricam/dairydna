using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Ingestion;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Api.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/import-sources", async (IPublicDataImporter importer, CancellationToken ct) =>
        {
            var sources = await importer.ListSourcesAsync(ct);
            return Results.Ok(sources.Select(s => new
            {
                code = s.Code,
                displayName = s.DisplayName,
                seriesKind = s.SeriesKind.ToString(),
                schemaVersion = s.SchemaVersion,
                fixtureFileName = s.FixtureFileName,
                dataClassification = "Public"
            }));
        });

        var group = app.MapGroup("/api/import-runs");
        group.MapPost("/", async (ImportRequestBody body, IPublicDataImporter importer, CancellationToken ct) =>
        {
            try
            {
                var run = await importer.ImportAsync(new ImportRunRequest
                {
                    SourceCode = body.SourceCode,
                    FixtureFileName = body.FixtureFileName,
                    InlinePayloadJson = body.InlinePayloadJson
                }, ct);
                return Results.Accepted($"/api/import-runs/{run.Id}", ToDto(run));
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["sourceCode"] = [ex.Message] });
            }
            catch (FileNotFoundException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        });

        group.MapGet("/", async (IPublicDataImporter importer, CancellationToken ct) =>
            Results.Ok((await importer.ListRunsAsync(ct)).Select(ToDto)));

        group.MapGet("/{id:guid}", async (Guid id, IPublicDataImporter importer, CancellationToken ct) =>
        {
            var run = await importer.GetRunAsync(id, ct);
            return run is null ? Results.NotFound() : Results.Ok(ToDto(run));
        });

        group.MapGet("/{id:guid}/quarantine", async (Guid id, IPublicDataImporter importer, CancellationToken ct) =>
        {
            var run = await importer.GetRunAsync(id, ct);
            if (run is null) return Results.NotFound();
            var items = await importer.GetQuarantineAsync(id, ct);
            return Results.Ok(items.Select(q => new
            {
                id = q.Id,
                rowNumber = q.RowNumber,
                reason = q.Reason,
                sampleJson = q.SampleJson
            }));
        });

        app.MapGet("/api/public/market-prices", async (IDairyDnaDbContext db, string? regionCode, string? productCode, CancellationToken ct) =>
        {
            var q = db.PublicMarketPrices.AsQueryable();
            if (!string.IsNullOrWhiteSpace(regionCode)) q = q.Where(x => x.RegionCode == regionCode);
            if (!string.IsNullOrWhiteSpace(productCode)) q = q.Where(x => x.ProductCode == productCode);
            var rows = await q.OrderBy(x => x.EffectiveDate).ToListAsync(ct);
            return Results.Ok(new
            {
                dataClassification = "Public",
                disclaimer = "External/public market observations — not DairyDNA forecasts or trading recommendations.",
                items = rows.Select(r => new
                {
                    productCode = r.ProductCode,
                    regionCode = r.RegionCode,
                    effectiveDate = r.EffectiveDate,
                    pricePerPound = r.PricePerPound,
                    sourceLabel = r.SourceLabel,
                    importRunId = r.ImportRunId,
                    dataClassification = r.DataClassification
                })
            });
        });

        app.MapGet("/api/public/weather", async (IDairyDnaDbContext db, string? regionCode, CancellationToken ct) =>
        {
            var q = db.PublicWeatherObservations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(regionCode)) q = q.Where(x => x.RegionCode == regionCode);
            var rows = await q.OrderBy(x => x.ObservationDate).ToListAsync(ct);
            return Results.Ok(new { dataClassification = "Public", items = rows });
        });

        app.MapGet("/api/public/fuel-prices", async (IDairyDnaDbContext db, string? regionCode, CancellationToken ct) =>
        {
            var q = db.FuelPriceObservations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(regionCode)) q = q.Where(x => x.RegionCode == regionCode);
            var rows = await q.OrderBy(x => x.EffectiveDate).ToListAsync(ct);
            return Results.Ok(new { dataClassification = "Public", items = rows });
        });

        return app;
    }

    private static object ToDto(DairyDNA.Domain.Entities.ImportRun run) => new
    {
        id = run.Id,
        sourceCode = run.SourceCode,
        seriesKind = run.SeriesKind.ToString(),
        schemaVersion = run.SchemaVersion,
        payloadChecksumSha256 = run.PayloadChecksumSha256,
        status = run.Status.ToString(),
        startedAt = run.StartedAt,
        completedAt = run.CompletedAt,
        rawRowCount = run.RawRowCount,
        canonicalRowCount = run.CanonicalRowCount,
        quarantineCount = run.QuarantineCount,
        dataClassification = run.DataClassification,
        failureMessage = run.FailureMessage,
        priorSuccessfulRunId = run.PriorSuccessfulRunId,
        disclaimer = "Public/external data — not DairyDNA forecasts."
    };
}

public sealed class ImportRequestBody
{
    public string SourceCode { get; set; } = string.Empty;
    public string? FixtureFileName { get; set; }
    public string? InlinePayloadJson { get; set; }
}
