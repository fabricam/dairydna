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

    public static void EnsureContractDates(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("Contract EndDate must be >= StartDate.");
    }

    public static void EnsureNonEmptyName(string? name, string fieldName = "Name")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{fieldName} must not be empty.");
    }

    public static bool TruckCompatible(Truck truck, string productCode)
    {
        var codes = truck.CompatibleProductCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return codes.Contains(productCode, StringComparer.OrdinalIgnoreCase);
    }

    public static void EnsureTruckProductCompatible(Truck truck, string productCode)
    {
        if (!TruckCompatible(truck, productCode))
            throw new ArgumentException($"Truck is not compatible with product '{productCode}'.");
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
        EnsureNonEmptyName(facility.Name);
        EnsureNonNegative(facility.MilkStorageCapacityPounds, nameof(facility.MilkStorageCapacityPounds));
        EnsureNonNegative(facility.CreamStorageCapacityPounds, nameof(facility.CreamStorageCapacityPounds));
    }

    public static void ValidateFarm(Farm farm)
    {
        EnsureNonEmptyName(farm.Name);
        if (farm.HerdSize < 0)
            throw new ArgumentOutOfRangeException(nameof(farm.HerdSize), "HerdSize must be >= 0.");
    }

    public static void ValidateCustomer(Customer customer) => EnsureNonEmptyName(customer.Name);

    public static void ValidateProduct(Product product)
    {
        EnsureNonEmptyName(product.Name);
        EnsureNonEmptyName(product.Code, nameof(product.Code));
        if (product.MaximumAgeHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(product.MaximumAgeHours), "MaximumAgeHours must be > 0.");
    }

    public static void ValidateTruck(Truck truck)
    {
        EnsurePositiveQuantity(truck.MaximumCapacityPounds, nameof(truck.MaximumCapacityPounds));
        EnsureNonNegative(truck.CostPerMile, nameof(truck.CostPerMile));
        EnsureNonNegative(truck.CostPerHour, nameof(truck.CostPerHour));
        if (truck.AvailableUntil < truck.AvailableFrom)
            throw new ArgumentException("AvailableUntil must be >= AvailableFrom.");
    }

    public static void ValidateContract(Contract contract)
    {
        EnsureContractDates(contract.StartDate, contract.EndDate);
        EnsureNonNegative(contract.MinimumQuantityPounds, nameof(contract.MinimumQuantityPounds));
        EnsureNonNegative(contract.MaximumQuantityPounds, nameof(contract.MaximumQuantityPounds));
        if (contract.MaximumQuantityPounds < contract.MinimumQuantityPounds)
            throw new ArgumentException("MaximumQuantityPounds must be >= MinimumQuantityPounds.");
        EnsureNonNegative(contract.PricePerPound, nameof(contract.PricePerPound));
        EnsureNonNegative(contract.ShortfallPenaltyPerPound, nameof(contract.ShortfallPenaltyPerPound));
    }

    public static void ValidateShipment(Shipment shipment)
    {
        EnsurePositiveQuantity(shipment.QuantityPounds, nameof(shipment.QuantityPounds));
    }

    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
