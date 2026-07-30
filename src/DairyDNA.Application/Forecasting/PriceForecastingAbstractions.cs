using DairyDNA.Domain.Entities;

namespace DairyDNA.Application.Forecasting;

public sealed class PriceForecastRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly? AsOfDate { get; set; }
    public int RandomSeed { get; set; } = 104729;
    public int[] Horizons { get; set; } = [1, 7, 14, 28];
}

public sealed record OptimizationPricePoint(
    string ProductCode,
    string? RegionCode,
    DateOnly TargetDate,
    int HorizonDays,
    decimal PointPricePerPound,
    decimal LowerPricePerPound,
    decimal UpperPricePerPound);

public sealed class OptimizationPriceBundle
{
    public Guid GenerationId { get; init; }
    public DateOnly AsOfDate { get; init; }
    public IReadOnlyList<OptimizationPricePoint> Items { get; init; } = [];
    public string DataClassification { get; init; } = "Forecast";
    public string Disclaimer { get; init; } = "Price forecasts are estimates with uncertainty bands — not trade quotes or guaranteed clearing prices.";
}

public interface IPriceForecastService
{
    Task<PriceModelVersion> TrainAndPublishAsync(PriceForecastRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceForecast>> GetForecastsAsync(Guid generationId, string? productCode = null, string? regionCode = null, CancellationToken cancellationToken = default);
    Task<PriceModelVersion?> GetLatestModelAsync(Guid generationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateOnly Date, decimal ActualPricePerPound)>> GetActualsAsync(Guid generationId, string productCode, CancellationToken cancellationToken = default);
    Task<OptimizationPriceBundle> GetOptimizationBundleAsync(Guid generationId, DateOnly asOfDate, CancellationToken cancellationToken = default);
}

/// <summary>Builds price features from observations known at the requested as-of date.</summary>
public static class PriceFeatureBuilder
{
    public sealed record DailyPrice(string ProductCode, string RegionCode, DateOnly Date, decimal PricePerPound);
    public sealed record FeatureRow(string ProductCode, string RegionCode, DateOnly FeatureDate, DateOnly AsOfDate,
        float SinDoy, float CosDoy, float Lag1, float Lag7, float RollingMean7, float Label);

    public static FeatureRow? TryBuildFeature(IReadOnlyList<DailyPrice> series, DateOnly featureDate, DateOnly asOfDate)
    {
        var history = series.Where(x => x.Date <= asOfDate && x.Date < featureDate).OrderBy(x => x.Date).ToList();
        if (asOfDate >= featureDate)
            history = series.Where(x => x.Date < featureDate).OrderBy(x => x.Date).ToList();
        if (history.Count < 7) return null;
        if (history.Any(x => x.Date > asOfDate) && asOfDate < featureDate)
            throw new InvalidOperationException("Feature leakage: history after as-of.");

        var byDate = history.ToDictionary(x => x.Date);
        float Lag(int days) => byDate.TryGetValue(featureDate.AddDays(-days), out var row)
            ? (float)row.PricePerPound
            : (float)history.TakeLast(7).Average(x => x.PricePerPound);
        var last = history[^1];
        var doy = featureDate.DayOfYear;
        var label = series.FirstOrDefault(x => x.Date == featureDate)?.PricePerPound ?? 0m;
        return new FeatureRow(last.ProductCode, last.RegionCode, featureDate, asOfDate,
            (float)Math.Sin(2 * Math.PI * doy / 365d), (float)Math.Cos(2 * Math.PI * doy / 365d),
            Lag(1), Lag(7), (float)history.TakeLast(7).Average(x => x.PricePerPound), (float)label);
    }
}
