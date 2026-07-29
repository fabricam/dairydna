using DairyDNA.Domain.Entities;

namespace DairyDNA.Application.Forecasting;

public sealed class SupplyForecastRequest
{
    public Guid GenerationId { get; set; }
    public DateOnly? AsOfDate { get; set; }
    public int RandomSeed { get; set; } = 104729;
    public int[] Horizons { get; set; } = [1, 7, 14, 28];
}

public sealed class ForecastMetrics
{
    public decimal ModelWape7 { get; set; }
    public decimal BaselineWape7 { get; set; }
    public decimal WapeImprovementPct { get; set; }
    public decimal Mae { get; set; }
    public decimal Rmse { get; set; }
    public decimal Bias { get; set; }
    public decimal IntervalCoverage { get; set; }
    public decimal FacilityCoveragePct { get; set; }
    public bool MeetsAcceptanceBar { get; set; }
}

public interface ISupplyForecastService
{
    Task<SupplyModelVersion> TrainAndPublishAsync(SupplyForecastRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplyForecast>> GetForecastsAsync(Guid generationId, Guid? facilityId = null, string? regionCode = null, CancellationToken cancellationToken = default);
    Task<SupplyModelVersion?> GetLatestModelAsync(Guid generationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateOnly Date, decimal ActualPounds)>> GetActualsAsync(Guid generationId, Guid facilityId, CancellationToken cancellationToken = default);
}

/// <summary>Builds as-of-safe feature rows; used by leakage tests.</summary>
public static class SupplyFeatureBuilder
{
    public sealed record DailySupply(Guid FacilityId, string RegionCode, DateOnly Date, decimal MilkPounds, decimal HeatStress);

    public sealed record FeatureRow(
        Guid FacilityId,
        string RegionCode,
        DateOnly FeatureDate,
        DateOnly AsOfDate,
        float SinDoy,
        float CosDoy,
        float Lag7,
        float Lag14,
        float RollingMean7,
        float HeatStress,
        float Label);

    public static List<DailySupply> BuildDailySeries(
        IEnumerable<(Guid FacilityId, string RegionCode, DateOnly Date, decimal Qty)> lots,
        IEnumerable<(string RegionCode, DateOnly Date, decimal Heat)> weather)
    {
        var heat = weather
            .GroupBy(w => (w.RegionCode, w.Date))
            .ToDictionary(g => g.Key, g => g.Average(x => x.Heat));

        return lots
            .GroupBy(x => (x.FacilityId, x.RegionCode, x.Date))
            .Select(g =>
            {
                heat.TryGetValue((g.Key.RegionCode, g.Key.Date), out var h);
                return new DailySupply(g.Key.FacilityId, g.Key.RegionCode, g.Key.Date, g.Sum(x => x.Qty), h);
            })
            .OrderBy(x => x.FacilityId).ThenBy(x => x.Date)
            .ToList();
    }

    /// <summary>
    /// Features for FeatureDate may only use history with Date &lt;= AsOfDate and Date &lt; FeatureDate for labels pairing.
    /// For training, AsOfDate == FeatureDate - 1 day conceptually: lags computed from series strictly before FeatureDate.
    /// </summary>
    public static FeatureRow? TryBuildFeature(IReadOnlyList<DailySupply> facilitySeries, DateOnly featureDate, DateOnly asOfDate)
    {
        if (featureDate > asOfDate.AddDays(1) && featureDate > asOfDate)
        {
            // Forecasting into future: asOf is last known day; featureDate is target.
        }

        var history = facilitySeries.Where(x => x.Date <= asOfDate && x.Date < featureDate).OrderBy(x => x.Date).ToList();
        // For same-day training label at featureDate when asOfDate >= featureDate (backtest):
        if (asOfDate >= featureDate)
            history = facilitySeries.Where(x => x.Date < featureDate).OrderBy(x => x.Date).ToList();

        if (history.Count < 7) return null;

        var byDate = history.ToDictionary(x => x.Date, x => x);
        float Lag(int days)
        {
            var d = featureDate.AddDays(-days);
            return byDate.TryGetValue(d, out var row) ? (float)row.MilkPounds : (float)history.TakeLast(Math.Min(7, history.Count)).Average(x => x.MilkPounds);
        }

        var last = history[^1];
        var roll = history.TakeLast(7).Average(x => x.MilkPounds);
        var doy = featureDate.DayOfYear;
        var labelRow = facilitySeries.FirstOrDefault(x => x.Date == featureDate);
        var label = labelRow?.MilkPounds ?? 0m;

        // Leakage guard: no history date after asOfDate
        if (history.Any(h => h.Date > asOfDate))
            throw new InvalidOperationException("Feature leakage: history after as-of.");

        return new FeatureRow(
            last.FacilityId,
            last.RegionCode,
            featureDate,
            asOfDate,
            (float)Math.Sin(2 * Math.PI * doy / 365.0),
            (float)Math.Cos(2 * Math.PI * doy / 365.0),
            Lag(7),
            Lag(14),
            (float)roll,
            (float)last.HeatStress,
            (float)label);
    }
}
