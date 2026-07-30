using DairyDNA.Application.Abstractions;

namespace DairyDNA.Application.Scenarios;

internal sealed class FuelOverrideTransportCostCalculator : ITransportCostCalculator
{
    private readonly ITransportCostCalculator _inner;
    private readonly decimal _fuelPricePerGallon;

    public FuelOverrideTransportCostCalculator(ITransportCostCalculator inner, decimal fuelPricePerGallon)
    {
        _inner = inner;
        _fuelPricePerGallon = fuelPricePerGallon;
    }

    public TransportCostBreakdown Calculate(TransportCostRequest request) =>
        _inner.Calculate(new TransportCostRequest
        {
            OriginLat = request.OriginLat,
            OriginLon = request.OriginLon,
            DestLat = request.DestLat,
            DestLon = request.DestLon,
            CostPerMile = request.CostPerMile,
            CostPerHour = request.CostPerHour,
            QuantityPounds = request.QuantityPounds,
            FuelPricePerGallon = _fuelPricePerGallon,
            Mpg = request.Mpg,
            IncludeEmptyReturn = request.IncludeEmptyReturn,
            ProductCode = request.ProductCode,
            CompatibleProductCodes = request.CompatibleProductCodes
        });

    public TransportCostBreakdown Calculate(
        decimal originLat, decimal originLon, decimal destLat, decimal destLon,
        decimal costPerMile, decimal costPerHour, decimal quantityPounds) =>
        Calculate(new TransportCostRequest
        {
            OriginLat = originLat,
            OriginLon = originLon,
            DestLat = destLat,
            DestLon = destLon,
            CostPerMile = costPerMile,
            CostPerHour = costPerHour,
            QuantityPounds = quantityPounds
        });
}
