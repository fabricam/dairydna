using DairyDNA.Domain.Entities;

namespace DairyDNA.Application.Forecasting;

public sealed class DemandForecastRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly? AsOfDate { get; set; }
    public int RandomSeed { get; set; } = 104729;
    public int[] Horizons { get; set; } = [1, 7, 14, 28];
}

public interface IDemandForecastService
{
    Task<DemandModelVersion> TrainAndPublishAsync(DemandForecastRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DemandForecast>> GetForecastsAsync(Guid generationId, Guid? customerId = null, string? regionCode = null, CancellationToken cancellationToken = default);
    Task<DemandModelVersion?> GetLatestModelAsync(Guid generationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateOnly Date, decimal ActualPounds)>> GetActualsAsync(Guid generationId, Guid customerId, CancellationToken cancellationToken = default);
}

/// <summary>Builds customer demand features using only order history known on or before the as-of date.</summary>
public static class DemandFeatureBuilder
{
    public sealed record DailyDemand(Guid CustomerId, string RegionCode, string ProductCode, DateOnly Date, decimal Pounds);
    public sealed record FeatureRow(Guid CustomerId, string RegionCode, string ProductCode, DateOnly FeatureDate, DateOnly AsOfDate,
        float SinDoy, float CosDoy, float Lag7, float Lag14, float RollingMean7, float Label);

    public static List<DailyDemand> BuildDailySeries(IEnumerable<(Guid CustomerId, string RegionCode, string ProductCode, DateOnly Date, decimal Qty)> orders) =>
        orders.GroupBy(x => (x.CustomerId, x.RegionCode, x.ProductCode, x.Date))
            .Select(g => new DailyDemand(g.Key.CustomerId, g.Key.RegionCode, g.Key.ProductCode, g.Key.Date, g.Sum(x => x.Qty)))
            .OrderBy(x => x.CustomerId).ThenBy(x => x.ProductCode).ThenBy(x => x.Date).ToList();

    public static FeatureRow? TryBuildFeature(IReadOnlyList<DailyDemand> series, DateOnly featureDate, DateOnly asOfDate)
    {
        var history = (asOfDate >= featureDate
                ? series.Where(x => x.Date < featureDate)
                : series.Where(x => x.Date <= asOfDate && x.Date < featureDate))
            .OrderBy(x => x.Date).ToList();
        if (history.Count < 7) return null;
        if (history.Any(x => x.Date > asOfDate) && asOfDate < featureDate)
            throw new InvalidOperationException("Feature leakage: history after as-of.");

        var byDate = history.ToDictionary(x => x.Date);
        float Lag(int days) => byDate.TryGetValue(featureDate.AddDays(-days), out var row)
            ? (float)row.Pounds
            : (float)history.TakeLast(7).Average(x => x.Pounds);
        var last = history[^1];
        var doy = featureDate.DayOfYear;
        var label = series.FirstOrDefault(x => x.Date == featureDate)?.Pounds ?? 0m;
        return new FeatureRow(last.CustomerId, last.RegionCode, last.ProductCode, featureDate, asOfDate,
            (float)Math.Sin(2 * Math.PI * doy / 365d), (float)Math.Cos(2 * Math.PI * doy / 365d),
            Lag(7), Lag(14), (float)history.TakeLast(7).Average(x => x.Pounds), (float)label);
    }
}
