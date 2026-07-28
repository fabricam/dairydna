using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;

namespace DairyDNA.DataGenerator;

public sealed class ThinSliceGenerator : IThinSliceGenerator
{
    private readonly IDairyDnaDbContext _db;

    public ThinSliceGenerator(IDairyDnaDbContext db) => _db = db;

    public async Task<GenerationManifest> GenerateAsync(ThinSliceGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var rng = new Random(request.RandomSeed);
        var id = Guid.NewGuid();
        var planningDate = request.EndDate;
        var configHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));

        var manifest = new GenerationManifest
        {
            Id = id,
            ScenarioName = request.ScenarioName,
            SchemaVersion = request.SchemaVersion,
            RandomSeed = request.RandomSeed,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlanningDate = planningDate,
            FarmCount = request.FarmCount,
            FacilityCount = request.FacilityCount,
            CustomerCount = request.CustomerCount,
            TruckCount = request.TruckCount,
            ConfigurationHash = configHash,
            GeneratedAt = DateTimeOffset.UtcNow,
            Status = GenerationRunStatus.Running,
            IsSynthetic = true
        };
        _db.Add(manifest);

        var products = new[]
        {
            new Product { Id = CreateDeterministicGuid(id, request.RandomSeed, "RAW_MILK"), GenerationId = id, Code = "RAW_MILK", Name = "Raw Milk", MaximumAgeHours = 72, UnitOfMeasure = "lb" },
            new Product { Id = CreateDeterministicGuid(id, request.RandomSeed, "CREAM"), GenerationId = id, Code = "CREAM", Name = "Cream", MaximumAgeHours = 48, UnitOfMeasure = "lb" }
        };
        _db.AddRange(products);

        var farms = new List<Farm>();
        for (var i = 0; i < request.FarmCount; i++)
        {
            farms.Add(new Farm
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"farm-{i}"),
                GenerationId = id,
                Name = $"Synthetic Farm {i + 1}",
                RegionCode = $"R{(i % 3) + 1}",
                Latitude = 42.0m + (decimal)(rng.NextDouble() * 2),
                Longitude = -90.0m - (decimal)(rng.NextDouble() * 3),
                HerdSize = 100 + rng.Next(0, 400),
                Active = true
            });
        }
        _db.AddRange(farms);

        var facilities = new List<Facility>();
        for (var i = 0; i < request.FacilityCount; i++)
        {
            facilities.Add(new Facility
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"facility-{i}"),
                GenerationId = id,
                Name = $"Facility {i + 1}",
                FacilityType = i == 0 ? FacilityType.Receiving : FacilityType.Storage,
                RegionCode = $"R{(i % 3) + 1}",
                Latitude = 43.0m + i * 0.4m,
                Longitude = -89.0m - i * 0.5m,
                MilkStorageCapacityPounds = 200_000,
                CreamStorageCapacityPounds = 80_000,
                Active = true
            });
        }
        _db.AddRange(facilities);

        var customers = new List<Customer>();
        for (var i = 0; i < request.CustomerCount; i++)
        {
            customers.Add(new Customer
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"customer-{i}"),
                GenerationId = id,
                Name = $"Customer {i + 1}",
                RegionCode = $"R{(i % 3) + 1}",
                Latitude = 43.2m + (decimal)(rng.NextDouble()),
                Longitude = -88.5m - (decimal)(rng.NextDouble() * 2),
                Active = true
            });
        }
        _db.AddRange(customers);

        var trucks = new List<Truck>();
        for (var i = 0; i < request.TruckCount; i++)
        {
            trucks.Add(new Truck
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"truck-{i}"),
                GenerationId = id,
                HomeFacilityId = facilities[i % facilities.Count].Id,
                MaximumCapacityPounds = 45_000 + i * 5_000,
                CompatibleProductCodes = "RAW_MILK,CREAM",
                CostPerMile = 1.25m + i * 0.1m,
                CostPerHour = 55m + i * 2m,
                AvailableFrom = planningDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                AvailableUntil = planningDate.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc),
                Status = TruckStatus.Available,
                Active = true
            });
        }
        _db.AddRange(trucks);

        var milk = products[0];
        var cream = products[1];
        var contracts = new List<Contract>();
        for (var i = 0; i < Math.Min(3, customers.Count); i++)
        {
            contracts.Add(new Contract
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"contract-{i}"),
                GenerationId = id,
                CustomerId = customers[i].Id,
                ProductId = i % 2 == 0 ? milk.Id : cream.Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MinimumQuantityPounds = 5_000,
                MaximumQuantityPounds = 50_000,
                PricePerPound = i % 2 == 0 ? 0.20m : 0.90m,
                ShortfallPenaltyPerPound = 0.05m,
                Active = true
            });
        }
        _db.AddRange(contracts);

        var dayCount = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        var lots = new List<InventoryLot>();
        var orders = new List<Order>();
        var prices = new List<MarketPrice>();

        for (var d = 0; d < dayCount; d++)
        {
            var day = request.StartDate.AddDays(d);
            var seasonal = 1.0m + 0.08m * (decimal)Math.Sin(d / 20.0);

            prices.Add(new MarketPrice
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"price-milk-{d}"),
                GenerationId = id,
                ProductId = milk.Id,
                EffectiveDate = day,
                PricePerPound = DomainInvariants.Money(0.18m * seasonal),
                PriceType = MarketPriceType.StaticSpot,
                Source = "synthetic-static"
            });
            prices.Add(new MarketPrice
            {
                Id = CreateDeterministicGuid(id, request.RandomSeed, $"price-cream-{d}"),
                GenerationId = id,
                ProductId = cream.Id,
                EffectiveDate = day,
                PricePerPound = DomainInvariants.Money(0.95m * seasonal),
                PriceType = MarketPriceType.StaticSpot,
                Source = "synthetic-static"
            });

            foreach (var facility in facilities)
            {
                var milkQty = DomainInvariants.Money(8_000m * seasonal + rng.Next(0, 500));
                var creamQty = DomainInvariants.Money(milkQty * 0.12m);
                var produced = new DateTimeOffset(day.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc));

                lots.Add(new InventoryLot
                {
                    Id = CreateDeterministicGuid(id, request.RandomSeed, $"lot-milk-{facility.Id}-{d}"),
                    GenerationId = id,
                    FacilityId = facility.Id,
                    ProductId = milk.Id,
                    QuantityPounds = milkQty,
                    ButterfatPercent = 3.7m,
                    ProducedAt = produced,
                    ExpiresAt = produced.AddHours(milk.MaximumAgeHours),
                    QualityGrade = "A",
                    Status = InventoryLotStatus.Available,
                    AsOfDate = day
                });
                lots.Add(new InventoryLot
                {
                    Id = CreateDeterministicGuid(id, request.RandomSeed, $"lot-cream-{facility.Id}-{d}"),
                    GenerationId = id,
                    FacilityId = facility.Id,
                    ProductId = cream.Id,
                    QuantityPounds = creamQty,
                    ButterfatPercent = 36m,
                    ProducedAt = produced,
                    ExpiresAt = produced.AddHours(cream.MaximumAgeHours),
                    QualityGrade = "A",
                    Status = InventoryLotStatus.Available,
                    AsOfDate = day
                });
            }

            foreach (var customer in customers)
            {
                var product = (d + customers.IndexOf(customer)) % 2 == 0 ? milk : cream;
                var price = product.Code == "CREAM" ? 1.05m : 0.22m;
                // Make distant high-price customer attractive but not always best after transport
                if (customer.Name.EndsWith("5"))
                    price += 0.35m;

                var qty = DomainInvariants.Money(2_000m + rng.Next(0, 1500));
                var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc));
                var end = new DateTimeOffset(day.ToDateTime(new TimeOnly(20, 0), DateTimeKind.Utc));

                orders.Add(new Order
                {
                    Id = CreateDeterministicGuid(id, request.RandomSeed, $"order-{customer.Id}-{d}-{product.Code}"),
                    GenerationId = id,
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    RequestedQuantityPounds = qty,
                    MinimumAcceptableQuantityPounds = DomainInvariants.Money(qty * 0.5m),
                    RequestedDeliveryStart = start,
                    RequestedDeliveryEnd = end,
                    OfferedPricePerPound = DomainInvariants.Money(price * seasonal),
                    OrderType = OrderType.Spot,
                    Status = OrderStatus.Open,
                    RequestDate = day
                });
            }
        }

        _db.AddRange(prices);
        _db.AddRange(lots);
        _db.AddRange(orders);

        var counts = new Dictionary<string, int>
        {
            ["farms"] = farms.Count,
            ["facilities"] = facilities.Count,
            ["customers"] = customers.Count,
            ["trucks"] = trucks.Count,
            ["products"] = products.Length,
            ["contracts"] = contracts.Count,
            ["inventoryLots"] = lots.Count,
            ["orders"] = orders.Count,
            ["marketPrices"] = prices.Count
        };
        manifest.EntityCountsJson = JsonSerializer.Serialize(counts);
        manifest.Status = GenerationRunStatus.Completed;

        await _db.SaveChangesAsync(cancellationToken);
        return manifest;
    }

    private static Guid CreateDeterministicGuid(Guid generationId, int seed, string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{generationId:N}:{seed}:{key}"));
        Span<byte> g = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(g);
        return new Guid(g);
    }
}
