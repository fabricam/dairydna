using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Ingestion;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.DataIngestion;

public sealed class PublicDataImporter : IPublicDataImporter
{
    public const string SchemaVersion = ImportSourceCatalog.SchemaVersion;
    private readonly IDairyDnaDbContext _db;
    private readonly string _fixtureDirectory;

    public PublicDataImporter(IDairyDnaDbContext db, string? fixtureDirectory = null)
    {
        _db = db;
        if (fixtureDirectory is not null)
            _fixtureDirectory = fixtureDirectory;
        else
        {
            var asmDir = Path.GetDirectoryName(typeof(PublicDataImporter).Assembly.Location) ?? AppContext.BaseDirectory;
            _fixtureDirectory = Path.Combine(asmDir, "Fixtures");
        }
    }

    public async Task EnsureSourcesSeededAsync(CancellationToken cancellationToken = default)
    {
        foreach (var source in ImportSourceCatalog.Defaults)
        {
            if (!await _db.ImportSources.AnyAsync(x => x.Code == source.Code, cancellationToken))
                _db.Add(CloneSource(source));
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSourcesSeededAsync(cancellationToken);
        return await _db.ImportSources.Where(x => x.Active).OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportRun>> ListRunsAsync(CancellationToken cancellationToken = default)
        => await _db.ImportRuns.OrderByDescending(x => x.StartedAt).ToListAsync(cancellationToken);

    public Task<ImportRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.ImportRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<QuarantineItem>> GetQuarantineAsync(Guid importRunId, CancellationToken cancellationToken = default)
        => await _db.QuarantineItems.Where(x => x.ImportRunId == importRunId).ToListAsync(cancellationToken);

    public async Task<ImportRun> ImportAsync(ImportRunRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSourcesSeededAsync(cancellationToken);
        var source = await _db.ImportSources.FirstOrDefaultAsync(x => x.Code == request.SourceCode && x.Active, cancellationToken)
            ?? throw new ArgumentException($"Unknown or inactive import source '{request.SourceCode}'.");

        var fileName = request.FixtureFileName ?? source.FixtureFileName;
        string content;
        if (!string.IsNullOrWhiteSpace(request.InlinePayloadJson))
            content = request.InlinePayloadJson!;
        else
        {
            var path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(_fixtureDirectory, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Fixture not found: {path}");
            content = await File.ReadAllTextAsync(path, cancellationToken);
        }

        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var prior = await _db.ImportRuns
            .Where(x => x.PayloadChecksumSha256 == checksum
                        && x.SchemaVersion == source.SchemaVersion
                        && (x.Status == ImportRunStatus.Completed || x.Status == ImportRunStatus.SkippedIdempotent))
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var run = new ImportRun
        {
            Id = Guid.NewGuid(),
            ImportSourceId = source.Id,
            SourceCode = source.Code,
            SeriesKind = source.SeriesKind,
            SchemaVersion = source.SchemaVersion,
            PayloadChecksumSha256 = checksum,
            Status = ImportRunStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            DataClassification = "Public"
        };
        _db.Add(run);
        _db.Add(new RawPayload
        {
            Id = Guid.NewGuid(),
            ImportRunId = run.Id,
            FileName = fileName,
            ContentType = "application/json",
            ChecksumSha256 = checksum,
            ContentUtf8 = content,
            StoredAt = DateTimeOffset.UtcNow
        });

        if (prior is not null)
        {
            run.Status = ImportRunStatus.SkippedIdempotent;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.PriorSuccessfulRunId = prior.Id;
            run.CanonicalRowCount = prior.CanonicalRowCount;
            run.RawRowCount = prior.RawRowCount;
            run.QuarantineCount = 0;
            run.FailureMessage = $"Identical checksum+schema already imported as run {prior.Id}.";
            await _db.SaveChangesAsync(cancellationToken);
            return run;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (!root.TryGetProperty("schemaVersion", out var schemaEl) ||
                schemaEl.GetString() != SchemaVersion)
            {
                return await FailQuarantineAsync(run, "Schema version missing or unsupported.", content, cancellationToken);
            }

            if (!root.TryGetProperty("seriesKind", out var kindEl) ||
                !Enum.TryParse<ImportSeriesKind>(kindEl.GetString(), ignoreCase: true, out var kind) ||
                kind != source.SeriesKind)
            {
                return await FailQuarantineAsync(run, "seriesKind mismatch or invalid.", content, cancellationToken);
            }

            if (!root.TryGetProperty("rows", out var rowsEl) || rowsEl.ValueKind != JsonValueKind.Array || rowsEl.GetArrayLength() == 0)
            {
                return await FailQuarantineAsync(run, "Empty or missing rows array.", content, cancellationToken);
            }

            var sourceLabel = root.TryGetProperty("sourceLabel", out var labelEl)
                ? labelEl.GetString() ?? "Public"
                : "Public";

            var quarantine = new List<QuarantineItem>();
            var canonical = 0;
            var rowNum = 0;

            switch (source.SeriesKind)
            {
                case ImportSeriesKind.DairyMarketPrice:
                    foreach (var row in rowsEl.EnumerateArray())
                    {
                        rowNum++;
                        if (!TryReadDairy(row, out var product, out var region, out var date, out var price, out var reason))
                        {
                            quarantine.Add(Item(run.Id, rowNum, reason, row));
                            continue;
                        }
                        _db.Add(new PublicMarketPrice
                        {
                            Id = Guid.NewGuid(),
                            ImportRunId = run.Id,
                            ProductCode = product,
                            RegionCode = region,
                            EffectiveDate = date,
                            PricePerPound = price,
                            SourceLabel = sourceLabel,
                            DataClassification = "Public"
                        });
                        canonical++;
                    }
                    break;

                case ImportSeriesKind.Weather:
                    foreach (var row in rowsEl.EnumerateArray())
                    {
                        rowNum++;
                        if (!TryReadWeather(row, out var region, out var date, out var temp, out var heat, out var reason))
                        {
                            quarantine.Add(Item(run.Id, rowNum, reason, row));
                            continue;
                        }
                        _db.Add(new PublicWeatherObservation
                        {
                            Id = Guid.NewGuid(),
                            ImportRunId = run.Id,
                            RegionCode = region,
                            ObservationDate = date,
                            TemperatureF = temp,
                            HeatStressIndex = heat,
                            SourceLabel = sourceLabel,
                            DataClassification = "Public"
                        });
                        canonical++;
                    }
                    break;

                case ImportSeriesKind.FuelPrice:
                    foreach (var row in rowsEl.EnumerateArray())
                    {
                        rowNum++;
                        if (!TryReadFuel(row, out var region, out var date, out var price, out var cadence, out var reason))
                        {
                            quarantine.Add(Item(run.Id, rowNum, reason, row));
                            continue;
                        }
                        _db.Add(new FuelPriceObservation
                        {
                            Id = Guid.NewGuid(),
                            ImportRunId = run.Id,
                            RegionCode = region,
                            EffectiveDate = date,
                            PricePerGallon = price,
                            Cadence = cadence,
                            SourceLabel = sourceLabel,
                            DataClassification = "Public"
                        });
                        canonical++;
                    }
                    break;
            }

            if (quarantine.Count > 0)
                _db.AddRange(quarantine);

            run.RawRowCount = rowNum;
            run.CanonicalRowCount = canonical;
            run.QuarantineCount = quarantine.Count;
            run.CompletedAt = DateTimeOffset.UtcNow;

            if (canonical == 0)
            {
                run.Status = ImportRunStatus.Failed;
                run.FailureMessage = "All rows quarantined; no canonical observations written.";
            }
            else if (quarantine.Count > 0)
            {
                run.Status = ImportRunStatus.CompletedWithQuarantine;
            }
            else
            {
                run.Status = ImportRunStatus.Completed;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return run;
        }
        catch (JsonException ex)
        {
            return await FailQuarantineAsync(run, $"JSON parse error: {ex.Message}", content, cancellationToken);
        }
    }

    private async Task<ImportRun> FailQuarantineAsync(ImportRun run, string reason, string content, CancellationToken ct)
    {
        _db.Add(new QuarantineItem
        {
            Id = Guid.NewGuid(),
            ImportRunId = run.Id,
            RowNumber = null,
            Reason = reason,
            SampleJson = content.Length > 2000 ? content[..2000] : content
        });
        run.Status = ImportRunStatus.Failed;
        run.FailureMessage = reason;
        run.QuarantineCount = 1;
        run.CanonicalRowCount = 0;
        run.CompletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return run;
    }

    private static QuarantineItem Item(Guid runId, int row, string reason, JsonElement sample) => new()
    {
        Id = Guid.NewGuid(),
        ImportRunId = runId,
        RowNumber = row,
        Reason = reason,
        SampleJson = sample.GetRawText()
    };

    private static bool TryReadDairy(JsonElement row, out string product, out string region, out DateOnly date, out decimal price, out string reason)
    {
        product = region = reason = string.Empty;
        date = default;
        price = 0;
        product = row.TryGetProperty("productCode", out var p) ? p.GetString() ?? "" : "";
        region = row.TryGetProperty("regionCode", out var r) ? r.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(product) || string.IsNullOrWhiteSpace(region))
        {
            reason = "productCode and regionCode required";
            return false;
        }
        if (!row.TryGetProperty("effectiveDate", out var d) || !DateOnly.TryParse(d.GetString(), out date))
        {
            reason = "effectiveDate invalid";
            return false;
        }
        if (!row.TryGetProperty("pricePerPound", out var pr) || !pr.TryGetDecimal(out price) || price < 0)
        {
            reason = "pricePerPound invalid";
            return false;
        }
        return true;
    }

    private static bool TryReadWeather(JsonElement row, out string region, out DateOnly date, out decimal temp, out decimal heat, out string reason)
    {
        region = reason = string.Empty;
        date = default;
        temp = heat = 0;
        region = row.TryGetProperty("regionCode", out var r) ? r.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(region))
        {
            reason = "regionCode required";
            return false;
        }
        if (!row.TryGetProperty("observationDate", out var d) || !DateOnly.TryParse(d.GetString(), out date))
        {
            reason = "observationDate invalid";
            return false;
        }
        if (!row.TryGetProperty("temperatureF", out var t) || !t.TryGetDecimal(out temp))
        {
            reason = "temperatureF invalid";
            return false;
        }
        if (!row.TryGetProperty("heatStressIndex", out var h) || !h.TryGetDecimal(out heat) || heat < 0)
        {
            reason = "heatStressIndex invalid";
            return false;
        }
        return true;
    }

    private static bool TryReadFuel(JsonElement row, out string region, out DateOnly date, out decimal price, out string cadence, out string reason)
    {
        region = cadence = reason = string.Empty;
        date = default;
        price = 0;
        region = row.TryGetProperty("regionCode", out var r) ? r.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(region))
        {
            reason = "regionCode required";
            return false;
        }
        if (!row.TryGetProperty("effectiveDate", out var d) || !DateOnly.TryParse(d.GetString(), out date))
        {
            reason = "effectiveDate invalid";
            return false;
        }
        if (!row.TryGetProperty("pricePerGallon", out var pr) || !pr.TryGetDecimal(out price) || price < 0)
        {
            reason = "pricePerGallon invalid";
            return false;
        }
        cadence = row.TryGetProperty("cadence", out var c) ? c.GetString() ?? "Weekly" : "Weekly";
        return true;
    }

    private static ImportSource CloneSource(ImportSource s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        DisplayName = s.DisplayName,
        SeriesKind = s.SeriesKind,
        SchemaVersion = s.SchemaVersion,
        FixtureFileName = s.FixtureFileName,
        Active = s.Active
    };
}
