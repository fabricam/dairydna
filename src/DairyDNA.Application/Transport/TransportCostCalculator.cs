using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Rules;

namespace DairyDNA.Application.Transport;

public sealed class TransportCostCalculator : ITransportCostCalculator
{
    public const string CostingModelVersion = "transport-cost-v2";
    public const decimal DefaultFuelPricePerGallon = 3.50m;
    public const decimal DefaultMpg = 6.5m;
    public const decimal AverageMph = 45m;
    public const decimal DefaultLoadUnloadHours = 1.0m;
    public const string Assumptions =
        "Average speed is 45 mph; load/unload time is 1.0 hour; empty-return policy bills round-trip miles when included.";

    public TransportCostBreakdown Calculate(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon,
        decimal costPerMile,
        decimal costPerHour,
        decimal quantityPounds)
    {
        return Calculate(new TransportCostRequest
        {
            OriginLat = originLat,
            OriginLon = originLon,
            DestLat = destLat,
            DestLon = destLon,
            CostPerMile = costPerMile,
            CostPerHour = costPerHour,
            QuantityPounds = quantityPounds,
            IncludeEmptyReturn = true
        });
    }

    public TransportCostBreakdown Calculate(TransportCostRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var oneWayMiles = HaversineMiles(
            (double)request.OriginLat, (double)request.OriginLon,
            (double)request.DestLat, (double)request.DestLon);
        var includeEmptyReturn = request.IncludeEmptyReturn ?? true;
        var billedMiles = oneWayMiles * (includeEmptyReturn ? 2m : 1m);
        var fuelPrice = request.FuelPricePerGallon ?? DefaultFuelPricePerGallon;
        var mpg = request.Mpg ?? DefaultMpg;
        var tripHours = (oneWayMiles / AverageMph) + DefaultLoadUnloadHours;
        var fuel = DomainInvariants.Money((billedMiles / mpg) * fuelPrice);
        var operating = DomainInvariants.Money((billedMiles * request.CostPerMile) + (tripHours * request.CostPerHour));
        var total = DomainInvariants.Money(fuel + operating);

        return new TransportCostBreakdown
        {
            CostingModelVersion = CostingModelVersion,
            EmptyReturnIncluded = includeEmptyReturn,
            FuelPricePerGallon = DomainInvariants.Money(fuelPrice),
            Assumptions = Assumptions,
            OneWayMiles = DomainInvariants.Money(oneWayMiles),
            BilledMiles = DomainInvariants.Money(billedMiles),
            LoadUnloadHours = DefaultLoadUnloadHours,
            AverageSpeedMph = AverageMph,
            DistanceMiles = DomainInvariants.Money(oneWayMiles),
            FuelCost = fuel,
            OperatingCost = operating,
            TotalEstimatedCost = total
        };
    }

    private static void Validate(TransportCostRequest request)
    {
        EnsureCoordinates(request.OriginLat, request.OriginLon, "origin");
        EnsureCoordinates(request.DestLat, request.DestLon, "destination");
        DomainInvariants.EnsureNonNegative(request.CostPerMile, nameof(request.CostPerMile));
        DomainInvariants.EnsureNonNegative(request.CostPerHour, nameof(request.CostPerHour));
        DomainInvariants.EnsurePositiveQuantity(request.QuantityPounds, nameof(request.QuantityPounds));

        if (request.FuelPricePerGallon is < 0)
            throw new ArgumentOutOfRangeException(nameof(request.FuelPricePerGallon), "FuelPricePerGallon must be >= 0.");
        if (request.Mpg is <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Mpg), "Mpg must be > 0.");

        if (!string.IsNullOrWhiteSpace(request.CompatibleProductCodes))
        {
            if (string.IsNullOrWhiteSpace(request.ProductCode))
                throw new ArgumentException("ProductCode is required when CompatibleProductCodes is supplied.", nameof(request.ProductCode));

            var compatible = request.CompatibleProductCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!compatible.Contains(request.ProductCode, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Product '{request.ProductCode}' is not compatible with this transport request.", nameof(request.ProductCode));
        }
    }

    private static void EnsureCoordinates(decimal latitude, decimal longitude, string location)
    {
        if (latitude == 0m && longitude == 0m)
            throw new ArgumentException($"{location} latitude and longitude cannot both be zero.", location);
        if (latitude is < -90m or > 90m)
            throw new ArgumentOutOfRangeException($"{location}Lat", $"{location} latitude must be between -90 and 90.");
        if (longitude is < -180m or > 180m)
            throw new ArgumentOutOfRangeException($"{location}Lon", $"{location} longitude must be between -180 and 180.");
    }

    internal static decimal HaversineMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3958.7613; // Earth radius miles
        static double ToRad(double d) => d * Math.PI / 180.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(R * c);
    }
}
