using DairyDNA.Domain.Entities;

namespace DairyDNA.Domain.Rules;

public static class DomainInvariants
{
    public static void EnsurePositiveQuantity(decimal quantity, string name)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(name, quantity, $"{name} must be > 0.");
    }

    public static void EnsureNonNegative(decimal value, string name)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be >= 0.");
    }

    public static void EnsureExpiresAfterProduced(DateTimeOffset producedAt, DateTimeOffset expiresAt)
    {
        if (expiresAt <= producedAt)
            throw new ArgumentException("ExpiresAt must be greater than ProducedAt.");
    }

    public static void EnsureDeliveryWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw new ArgumentException("RequestedDeliveryEnd must be >= RequestedDeliveryStart.");
    }

    public static bool TruckCompatible(Truck truck, string productCode)
    {
        var codes = truck.CompatibleProductCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return codes.Contains(productCode, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsExpired(InventoryLot lot, DateOnly asOf)
    {
        var asOfInstant = asOf.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return lot.ExpiresAt <= new DateTimeOffset(asOfInstant, TimeSpan.Zero) || lot.Status == Enums.InventoryLotStatus.Expired;
    }

    public static void ValidateInventoryLot(InventoryLot lot)
    {
        EnsurePositiveQuantity(lot.QuantityPounds, nameof(lot.QuantityPounds));
        EnsureExpiresAfterProduced(lot.ProducedAt, lot.ExpiresAt);
    }

    public static void ValidateOrder(Order order)
    {
        EnsurePositiveQuantity(order.RequestedQuantityPounds, nameof(order.RequestedQuantityPounds));
        EnsurePositiveQuantity(order.MinimumAcceptableQuantityPounds, nameof(order.MinimumAcceptableQuantityPounds));
        if (order.MinimumAcceptableQuantityPounds > order.RequestedQuantityPounds)
            throw new ArgumentException("MinimumAcceptableQuantityPounds must be <= RequestedQuantityPounds.");
        EnsureDeliveryWindow(order.RequestedDeliveryStart, order.RequestedDeliveryEnd);
        EnsureNonNegative(order.OfferedPricePerPound, nameof(order.OfferedPricePerPound));
    }

    public static void ValidateFacility(Facility facility)
    {
        EnsureNonNegative(facility.MilkStorageCapacityPounds, nameof(facility.MilkStorageCapacityPounds));
        EnsureNonNegative(facility.CreamStorageCapacityPounds, nameof(facility.CreamStorageCapacityPounds));
    }

    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
