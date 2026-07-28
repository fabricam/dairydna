using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Demo;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Reference;

public sealed class ReferenceDataHandlers
{
    private readonly IDairyDnaDbContext _db;

    public ReferenceDataHandlers(IDairyDnaDbContext db) => _db = db;

    public async Task EnsureGenerationAsync(Guid generationId, CancellationToken ct)
    {
        var exists = await _db.GenerationManifests.AnyAsync(x => x.Id == generationId, ct);
        if (!exists) throw new KeyNotFoundException($"Generation {generationId} not found.");
    }

    public async Task<IReadOnlyList<NetworkMapPoint>> GetNetworkAsync(Guid generationId, bool activeOnly, CancellationToken ct)
    {
        await EnsureGenerationAsync(generationId, ct);
        var farms = await _db.Farms.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).ToListAsync(ct);
        var facilities = await _db.Facilities.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).ToListAsync(ct);
        var customers = await _db.Customers.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).ToListAsync(ct);
        return farms.Select(f => new NetworkMapPoint(f.Id, "Farm", f.Name, f.Latitude, f.Longitude))
            .Concat(facilities.Select(f => new NetworkMapPoint(f.Id, "Facility", f.Name, f.Latitude, f.Longitude)))
            .Concat(customers.Select(c => new NetworkMapPoint(c.Id, "Customer", c.Name, c.Latitude, c.Longitude)))
            .ToList();
    }

    public Task<List<Farm>> ListFarmsAsync(Guid generationId, bool activeOnly, CancellationToken ct) =>
        _db.Farms.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).OrderBy(x => x.Name).ToListAsync(ct);

    public Task<Farm?> GetFarmAsync(Guid id, CancellationToken ct) =>
        _db.Farms.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Farm> CreateFarmAsync(Farm farm, CancellationToken ct)
    {
        await EnsureGenerationAsync(farm.GenerationId, ct);
        DomainInvariants.ValidateFarm(farm);
        farm.Id = farm.Id == Guid.Empty ? Guid.NewGuid() : farm.Id;
        _db.Add(farm);
        await _db.SaveChangesAsync(ct);
        return farm;
    }

    public async Task<Farm?> DeactivateFarmAsync(Guid id, CancellationToken ct)
    {
        var farm = await GetFarmAsync(id, ct);
        if (farm is null) return null;
        farm.Active = false;
        await _db.SaveChangesAsync(ct);
        return farm;
    }

    public Task<List<Facility>> ListFacilitiesAsync(Guid generationId, bool activeOnly, FacilityType? type, CancellationToken ct)
    {
        var q = _db.Facilities.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active));
        if (type is not null) q = q.Where(x => x.FacilityType == type);
        return q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Facility?> GetFacilityAsync(Guid id, CancellationToken ct) =>
        _db.Facilities.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Facility> CreateFacilityAsync(Facility facility, CancellationToken ct)
    {
        await EnsureGenerationAsync(facility.GenerationId, ct);
        DomainInvariants.ValidateFacility(facility);
        facility.Id = facility.Id == Guid.Empty ? Guid.NewGuid() : facility.Id;
        _db.Add(facility);
        await _db.SaveChangesAsync(ct);
        return facility;
    }

    public async Task<(Facility? Facility, string? Warning)> DeactivateFacilityAsync(Guid id, CancellationToken ct)
    {
        var facility = await GetFacilityAsync(id, ct);
        if (facility is null) return (null, null);
        var hasInventory = await _db.InventoryLots.AnyAsync(
            x => x.FacilityId == id && x.Status == InventoryLotStatus.Available, ct);
        facility.Active = false;
        await _db.SaveChangesAsync(ct);
        return (facility, hasInventory ? "Facility deactivated while available inventory remains." : null);
    }

    public Task<List<Customer>> ListCustomersAsync(Guid generationId, bool activeOnly, CancellationToken ct) =>
        _db.Customers.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).OrderBy(x => x.Name).ToListAsync(ct);

    public Task<Customer?> GetCustomerAsync(Guid id, CancellationToken ct) =>
        _db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken ct)
    {
        await EnsureGenerationAsync(customer.GenerationId, ct);
        DomainInvariants.ValidateCustomer(customer);
        customer.Id = customer.Id == Guid.Empty ? Guid.NewGuid() : customer.Id;
        _db.Add(customer);
        await _db.SaveChangesAsync(ct);
        return customer;
    }

    public async Task<Customer?> DeactivateCustomerAsync(Guid id, CancellationToken ct)
    {
        var customer = await GetCustomerAsync(id, ct);
        if (customer is null) return null;
        customer.Active = false;
        await _db.SaveChangesAsync(ct);
        return customer;
    }

    public Task<List<Product>> ListProductsAsync(Guid generationId, bool activeOnly, CancellationToken ct) =>
        _db.Products.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).OrderBy(x => x.Code).ToListAsync(ct);

    public Task<Product?> GetProductAsync(Guid id, CancellationToken ct) =>
        _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Product> CreateProductAsync(Product product, CancellationToken ct)
    {
        await EnsureGenerationAsync(product.GenerationId, ct);
        DomainInvariants.ValidateProduct(product);
        product.Id = product.Id == Guid.Empty ? Guid.NewGuid() : product.Id;
        _db.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<Product?> DeactivateProductAsync(Guid id, CancellationToken ct)
    {
        var product = await GetProductAsync(id, ct);
        if (product is null) return null;
        product.Active = false;
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public Task<List<Truck>> ListTrucksAsync(Guid generationId, bool activeOnly, CancellationToken ct) =>
        _db.Trucks.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).OrderBy(x => x.Id).ToListAsync(ct);

    public Task<Truck?> GetTruckAsync(Guid id, CancellationToken ct) =>
        _db.Trucks.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Truck> CreateTruckAsync(Truck truck, CancellationToken ct)
    {
        await EnsureGenerationAsync(truck.GenerationId, ct);
        DomainInvariants.ValidateTruck(truck);
        truck.Id = truck.Id == Guid.Empty ? Guid.NewGuid() : truck.Id;
        _db.Add(truck);
        await _db.SaveChangesAsync(ct);
        return truck;
    }

    public async Task<Truck?> DeactivateTruckAsync(Guid id, CancellationToken ct)
    {
        var truck = await GetTruckAsync(id, ct);
        if (truck is null) return null;
        truck.Active = false;
        await _db.SaveChangesAsync(ct);
        return truck;
    }

    public Task<List<Contract>> ListContractsAsync(Guid generationId, bool activeOnly, CancellationToken ct) =>
        _db.Contracts.Where(x => x.GenerationId == generationId && (!activeOnly || x.Active)).OrderBy(x => x.StartDate).ToListAsync(ct);

    public Task<Contract?> GetContractAsync(Guid id, CancellationToken ct) =>
        _db.Contracts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Contract> CreateContractAsync(Contract contract, CancellationToken ct)
    {
        await EnsureGenerationAsync(contract.GenerationId, ct);
        DomainInvariants.ValidateContract(contract);
        contract.Id = contract.Id == Guid.Empty ? Guid.NewGuid() : contract.Id;
        _db.Add(contract);
        await _db.SaveChangesAsync(ct);
        return contract;
    }

    public async Task<Contract?> DeactivateContractAsync(Guid id, CancellationToken ct)
    {
        var contract = await GetContractAsync(id, ct);
        if (contract is null) return null;
        contract.Active = false;
        await _db.SaveChangesAsync(ct);
        return contract;
    }

    public Task<List<Shipment>> ListShipmentsAsync(Guid generationId, CancellationToken ct) =>
        _db.Shipments.Where(x => x.GenerationId == generationId).OrderByDescending(x => x.DepartedAt).ToListAsync(ct);

    public Task<Shipment?> GetShipmentAsync(Guid id, CancellationToken ct) =>
        _db.Shipments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<InventoryLot> CreateInventoryLotAsync(InventoryLot lot, CancellationToken ct)
    {
        await EnsureGenerationAsync(lot.GenerationId, ct);
        DomainInvariants.ValidateInventoryLot(lot);
        lot.Id = lot.Id == Guid.Empty ? Guid.NewGuid() : lot.Id;
        _db.Add(lot);
        await _db.SaveChangesAsync(ct);
        return lot;
    }

    public async Task<Order> CreateOrderAsync(Order order, CancellationToken ct)
    {
        await EnsureGenerationAsync(order.GenerationId, ct);
        DomainInvariants.ValidateOrder(order);
        order.Id = order.Id == Guid.Empty ? Guid.NewGuid() : order.Id;
        _db.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }
}
