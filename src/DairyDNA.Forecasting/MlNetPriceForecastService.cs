using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Forecasting;
using DairyDNA.Application.Governance;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace DairyDNA.Forecasting;

public sealed class MlNetPriceForecastService : IPriceForecastService
{
    private static readonly int[] DefaultHorizons = [1, 7, 14, 28];
    private readonly IDairyDnaDbContext _db;
    public MlNetPriceForecastService(IDairyDnaDbContext db) => _db = db;

    public async Task<PriceModelVersion> TrainAndPublishAsync(PriceForecastRequest request, CancellationToken cancellationToken = default)
    {
        var generation = await _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == request.GenerationId, cancellationToken)
            ?? throw new ArgumentException("Unknown generation id.");
        var asOf = request.AsOfDate ?? generation.PlanningDate;
        var horizons = request.Horizons is { Length: > 0 } ? request.Horizons.Distinct().Where(h => h > 0).ToArray() : DefaultHorizons;
        var products = await _db.Products.Where(x => x.GenerationId == generation.Id && x.Active).ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);
        var regions = (await _db.Facilities.Where(x => x.GenerationId == generation.Id && x.Active).Select(x => x.RegionCode).ToListAsync(cancellationToken))
            .Concat(await _db.Customers.Where(x => x.GenerationId == generation.Id && x.Active).Select(x => x.RegionCode).ToListAsync(cancellationToken))
            .Distinct().DefaultIfEmpty("R1").ToList();
        var synthetic = await _db.MarketPrices.Where(x => x.GenerationId == generation.Id && x.EffectiveDate <= asOf && products.Keys.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.EffectiveDate, x.PricePerPound }).ToListAsync(cancellationToken);
        var publicRows = await _db.PublicMarketPrices.Where(x => x.EffectiveDate <= asOf).ToListAsync(cancellationToken);
        var observations = synthetic.SelectMany(x => regions.Select(r => new PriceFeatureBuilder.DailyPrice(products[x.ProductId], r, x.EffectiveDate, x.PricePerPound)))
            .Concat(publicRows.Select(x => new PriceFeatureBuilder.DailyPrice(x.ProductCode, x.RegionCode, x.EffectiveDate, x.PricePerPound)))
            .GroupBy(x => (x.ProductCode, x.RegionCode, x.Date))
            .Select(g => g.Last()).OrderBy(x => x.ProductCode).ThenBy(x => x.RegionCode).ThenBy(x => x.Date).ToList();
        var allDates = observations.Select(x => x.Date).Distinct().OrderBy(x => x).ToList();
        if (allDates.Count < 21) throw new InvalidOperationException("Insufficient history to train price forecast (need ≥21 distinct days).");

        var cut = allDates[Math.Max(14, (int)(allDates.Count * .8))];
        var train = new List<PriceFeatureBuilder.FeatureRow>(); var test = new List<PriceFeatureBuilder.FeatureRow>();
        var series = observations.GroupBy(x => (x.ProductCode, x.RegionCode)).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).ToList());
        foreach (var values in series.Values)
            foreach (var day in values)
            {
                var feature = PriceFeatureBuilder.TryBuildFeature(values, day.Date, day.Date);
                if (feature is not null) (day.Date < cut ? train : test).Add(feature);
            }
        if (train.Count < 20) throw new InvalidOperationException("Insufficient training rows after feature build.");

        var ml = new MLContext(request.RandomSeed);
        var pipeline = ml.Transforms.Concatenate("Features", nameof(MlRow.SinDoy), nameof(MlRow.CosDoy), nameof(MlRow.Lag1), nameof(MlRow.Lag7), nameof(MlRow.RollingMean7))
            .Append(ml.Regression.Trainers.Sdca(labelColumnName: nameof(MlRow.Label), featureColumnName: "Features", maximumNumberOfIterations: 100));
        var model = pipeline.Fit(ml.Data.LoadFromEnumerable(train.Select(ToMl)));
        var engine = ml.Model.CreatePredictionEngine<MlRow, MlPrediction>(model);
        Func<PriceFeatureBuilder.FeatureRow, float> predict = x => Math.Max(0, engine.Predict(ToMl(x)).Score);
        var metrics = Evaluate(test, predict);
        if (metrics.WapeImprovementPct < 0)
        {
            predict = x => Math.Max(0, x.Lag1);
            metrics = Evaluate(test, predict);
        }
        var meets = metrics.WapeImprovementPct >= 0 && Math.Abs(metrics.Bias) <= .05m;
        var version = new PriceModelVersion
        {
            Id = Guid.NewGuid(), GenerationId = generation.Id, Algorithm = metrics.WapeImprovementPct < 0 ? "last-price-baseline" : "mlnet-sdca",
            DatasetVersion = $"{generation.SchemaVersion}:{generation.ConfigurationHash[..Math.Min(16, generation.ConfigurationHash.Length)]}",
            RandomSeed = request.RandomSeed, HyperparametersJson = JsonSerializer.Serialize(new { iterations = 100, horizons, baseline = "last-price", sourceMix = publicRows.Count == 0 ? "synthetic-only" : "synthetic+public" }),
            MetricsJson = JsonSerializer.Serialize(metrics), MeetsAcceptanceBar = meets, Status = meets ? ForecastRunStatus.Completed : ForecastRunStatus.CompletedBelowBar,
            TrainedAt = DateTimeOffset.UtcNow, Notes = publicRows.Count == 0 ? "Synthetic-only source mix; public market prices were not available." : "Synthetic and public market prices merged by product, region, and date.",
            DataClassification = "Forecast"
        };
        version.LifecycleStatus = ModelLifecycleStatus.Candidate;
        version.ArtifactChecksumSha256 = ModelArtifactChecksum.Compute(
            version.Algorithm, version.DatasetVersion, version.FeatureSchemaVersion,
            version.RandomSeed, version.HyperparametersJson, version.MetricsJson);
        _db.Add(version);

        var forecasts = new List<PriceForecast>();
        foreach (var ((product, region), values) in series)
            foreach (var horizon in horizons)
            {
                var target = asOf.AddDays(horizon);
                var feature = PriceFeatureBuilder.TryBuildFeature(values, target, asOf);
                var point = feature is null ? values.Where(x => x.Date <= asOf).TakeLast(7).DefaultIfEmpty().Average(x => x?.PricePerPound ?? 0m) : (decimal)predict(feature);
                point = Math.Max(0, point);
                var half = Math.Max(.005m, point * .12m);
                forecasts.Add(new PriceForecast { Id = Guid.NewGuid(), ModelVersionId = version.Id, GenerationId = generation.Id, AggregationLevel = ForecastAggregationLevel.Region, RegionCode = region, ProductCode = product, AsOfDate = asOf, TargetDate = target, HorizonDays = horizon, PointEstimatePricePerPound = Round(point), LowerBoundPricePerPound = Round(Math.Max(0, point - half)), UpperBoundPricePerPound = Round(point + half), DataClassification = "Forecast" });
                if (feature is not null) _db.Add(new PriceFeatureSnapshot { Id = Guid.NewGuid(), ModelVersionId = version.Id, GenerationId = generation.Id, RegionCode = region, ProductCode = product, AsOfDate = asOf, FeatureDate = target, FeatureJson = JsonSerializer.Serialize(feature) });
            }
        _db.AddRange(forecasts);
        await _db.SaveChangesAsync(cancellationToken);
        return version;
    }

    public async Task<IReadOnlyList<PriceForecast>> GetForecastsAsync(Guid generationId, string? productCode = null, string? regionCode = null, CancellationToken cancellationToken = default)
    {
        var model = await GetLatestModelAsync(generationId, cancellationToken);
        if (model is null) return [];
        var query = _db.PriceForecasts.Where(x => x.ModelVersionId == model.Id);
        if (!string.IsNullOrWhiteSpace(productCode)) query = query.Where(x => x.ProductCode == productCode);
        if (!string.IsNullOrWhiteSpace(regionCode)) query = query.Where(x => x.RegionCode == regionCode);
        return await query.OrderBy(x => x.TargetDate).ThenBy(x => x.ProductCode).ToListAsync(cancellationToken);
    }
    public async Task<PriceModelVersion?> GetLatestModelAsync(Guid generationId, CancellationToken cancellationToken = default)
    {
        var published = await _db.PriceModelVersions
            .Where(x => x.GenerationId == generationId && x.LifecycleStatus == ModelLifecycleStatus.Published)
            .OrderByDescending(x => x.PublishedAt).ThenByDescending(x => x.TrainedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (published is not null) return published;

        return await _db.PriceModelVersions
            .Where(x => x.GenerationId == generationId && x.LifecycleStatus != ModelLifecycleStatus.Retired)
            .OrderByDescending(x => x.TrainedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<(DateOnly Date, decimal ActualPricePerPound)>> GetActualsAsync(Guid generationId, string productCode, CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.Where(x => x.GenerationId == generationId && x.Code == productCode).Select(x => x.Id).ToListAsync(cancellationToken);
        return (await _db.MarketPrices.Where(x => x.GenerationId == generationId && products.Contains(x.ProductId)).GroupBy(x => x.EffectiveDate).Select(g => new { Date = g.Key, Price = g.Average(x => x.PricePerPound) }).OrderBy(x => x.Date).ToListAsync(cancellationToken)).Select(x => (x.Date, x.Price)).ToList();
    }
    public async Task<OptimizationPriceBundle> GetOptimizationBundleAsync(Guid generationId, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var rows = await GetForecastsAsync(generationId, cancellationToken: cancellationToken);
        return new OptimizationPriceBundle { GenerationId = generationId, AsOfDate = asOfDate, Items = rows.Where(x => x.AsOfDate == asOfDate).Select(x => new OptimizationPricePoint(x.ProductCode, x.RegionCode, x.TargetDate, x.HorizonDays, x.PointEstimatePricePerPound, x.LowerBoundPricePerPound, x.UpperBoundPricePerPound)).ToList() };
    }
    private static ForecastMetrics Evaluate(IEnumerable<PriceFeatureBuilder.FeatureRow> rows, Func<PriceFeatureBuilder.FeatureRow, float> predict)
    {
        decimal mae = 0, baseline = 0, actuals = 0, square = 0, bias = 0; var n = 0; var covered = 0;
        foreach (var row in rows) { var point = Math.Max(0m, (decimal)predict(row)); var actual = (decimal)row.Label; var error = point - actual; var half = Math.Max(.005m, point * .12m); mae += Math.Abs(error); baseline += Math.Abs((decimal)row.Lag1 - actual); actuals += Math.Abs(actual); square += error * error; bias += error; n++; if (actual >= point - half && actual <= point + half) covered++; }
        var wape = actuals == 0 ? 1m : mae / actuals; var baseWape = actuals == 0 ? 1m : baseline / actuals;
        return new ForecastMetrics { ModelWape7 = Round(wape), BaselineWape7 = Round(baseWape), WapeImprovementPct = Round(baseWape <= 0 ? 0 : (baseWape - wape) / baseWape * 100m), Mae = n == 0 ? 0 : Round(mae / n), Rmse = n == 0 ? 0 : Round((decimal)Math.Sqrt((double)(square / n))), Bias = n == 0 ? 0 : Round(bias / n / Math.Max(.0001m, actuals / n)), IntervalCoverage = n == 0 ? 0 : Round((decimal)covered / n), MeetsAcceptanceBar = wape <= baseWape };
    }
    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
    private static MlRow ToMl(PriceFeatureBuilder.FeatureRow row) => new() { SinDoy = row.SinDoy, CosDoy = row.CosDoy, Lag1 = row.Lag1, Lag7 = row.Lag7, RollingMean7 = row.RollingMean7, Label = row.Label };
    private sealed class MlRow { public float SinDoy { get; set; } public float CosDoy { get; set; } public float Lag1 { get; set; } public float Lag7 { get; set; } public float RollingMean7 { get; set; } public float Label { get; set; } }
    private sealed class MlPrediction { [ColumnName("Score")] public float Score { get; set; } }
}
