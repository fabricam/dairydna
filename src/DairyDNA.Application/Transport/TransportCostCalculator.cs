using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Rules;

namespace DairyDNA.Application.Transport;

public sealed class TransportCostCalculator : ITransportCostCalculator
{
    private const decimal AverageMph = 45m;
    private const decimal FuelPerMile = 0.55m;
    private const decimal LoadUnloadHours = 1.0m;

    public TransportCostBreakdown Calculate(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon,
        decimal costPerMile,
        decimal costPerHour,
        decimal quantityPounds)
    {
        var miles = HaversineMiles((double)originLat, (double)originLon, (double)destLat, (double)destLon);
        // Empty-return approximation: round trip miles for operating base
        var billedMiles = miles * 2m;
        var hours = (miles / AverageMph) + LoadUnloadHours;
        var fuel = DomainInvariants.Money(billedMiles * FuelPerMile);
        var operating = DomainInvariants.Money((billedMiles * costPerMile) + (hours * costPerHour));
        var total = DomainInvariants.Money(fuel + operating);

        return new TransportCostBreakdown
        {
            DistanceMiles = DomainInvariants.Money(miles),
            FuelCost = fuel,
            OperatingCost = operating,
            TotalEstimatedCost = total
        };
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
