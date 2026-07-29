using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Domain.Rules;

namespace DairyDNA.DataGenerator;

public sealed class SyntheticDataGenerator : ISyntheticDataGenerator, IThinSliceGenerator
{
    private readonly IDairyDnaDbContext _db;

    public SyntheticDataGenerator(IDairyDnaDbContext db) => _db = db;

    public Task<GenerationManifest> GenerateAsync(ThinSliceGenerationRequest request, CancellationToken cancellationToken = default)
        => GenerateAsync(request.ToSynthetic(), cancellationToken);

    public async Task<GenerationManifest> GenerateAsync(SyntheticGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var resolved = Resolve(request);
        ValidateOrThrow(resolved);

        var rng = new Random(resolved.RandomSeed);
        var id = Guid.NewGuid();
        var planningDate = resolved.EndDate;
        var configPayload = new
        {
            resolved.ProfileName,
            resolved.RandomSeed,
            resolved.StartDate,
            resolved.EndDate,
            resolved.FarmCount,
            resolved.FacilityCount,
            resolved.CustomerCount,
            resolved.TruckCount,
            resolved.ProductSet,
            resolved.MissingnessRate,
            resolved.DenseHistoryDays,
            resolved.SparseCadenceDays,
            GenerationProfileCatalog.GeneratorVersion
        };
        var configHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(configPayload))));

        var manifest = new GenerationManifest
        {
            Id = id,
            ScenarioName = resolved.ScenarioName,
            SchemaVersion = resolved.SchemaVersion,
            RandomSeed = resolved.RandomSeed,
            StartDate = resolved.StartDate,
            EndDate = resolved.EndDate,
            PlanningDate = planningDate,
            FarmCount = resolved.FarmCount,
            FacilityCount = resolved.FacilityCount,
            CustomerCount = resolved.CustomerCount,
            TruckCount = resolved.TruckCount,
            ConfigurationHash = configHash,
            GeneratedAt = DateTimeOffset.UtcNow,
            Status = GenerationRunStatus.Running,
            IsSynthetic = true,
            GeneratorVersion = GenerationProfileCatalog.GeneratorVersion,
            ProfileName = resolved.ProfileName
        };
        _db.Add(manifest);

        try
        {
            var products = BuildProducts(id, resolved.RandomSeed, resolved.ProductSet);
            _db.AddRange(products);

            var farms = Enumerable.Range(0, resolved.FarmCount).Select(i => new Farm
            {
                Id = GuidFor(id, resolved.RandomSeed, $"farm-{i}"),
                GenerationId = id,
                Name = $"Synthetic Farm {i + 1}",
                RegionCode = $"R{(i % 3) + 1}",
                Latitude = 42.0m + (decimal)(rng.NextDouble() * 2),
                Longitude = -90.0m - (decimal)(rng.NextDouble() * 3),
                HerdSize = 100 + rng.Next(0, 400),
                Active = true
            }).ToList();
            _db.AddRange(farms);

            var facilities = Enumerable.Range(0, resolved.FacilityCount).Select(i => new Facility
            {
                Id = GuidFor(id, resolved.RandomSeed, $"facility-{i}"),
                GenerationId = id,
                Name = $"Facility {i + 1}",
                FacilityType = i % 4 == 0 ? FacilityType.Receiving : i % 4 == 1 ? FacilityType.Separation : i % 4 == 2 ? FacilityType.Processing : FacilityType.Storage,
                RegionCode = $"R{(i % 3) + 1}",
                Latitude = 43.0m + i * 0.15m,
                Longitude = -89.0m - i * 0.2m,
                MilkStorageCapacityPounds = 200_000 + i * 10_000,
                CreamStorageCapacityPounds = 80_000 + i * 5_000,
                Active = true
            }).ToList();
            _db.AddRange(facilities);

            var customers = Enumerable.Range(0, resolved.CustomerCount).Select(i => new Customer
            {
                Id = GuidFor(id, resolved.RandomSeed, $"customer-{i}"),
                GenerationId = id,
                Name = $"Customer {i + 1}",
                RegionCode = $"R{(i % 3) + 1}",
                Latitude = 43.2m + (decimal)(rng.NextDouble()),
                Longitude = -88.5m - (decimal)(rng.NextDouble() * 2),
                Active = true
            }).ToList();
            _db.AddRange(customers);

            var trucks = Enumerable.Range(0, resolved.TruckCount).Select(i => new Truck
            {
                Id = GuidFor(id, resolved.RandomSeed, $"truck-{i}"),
                GenerationId = id,
                HomeFacilityId = facilities[i % facilities.Count].Id,
                MaximumCapacityPounds = 45_000 + i * 1_000,
                CompatibleProductCodes = string.Join(",", products.Select(p => p.Code)),
                CostPerMile = 1.25m + (i % 5) * 0.1m,
                CostPerHour = 55m + (i % 5) * 2m,
                AvailableFrom = planningDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                AvailableUntil = planningDate.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc),
                Status = TruckStatus.Available,
                Active = true
            }).ToList();
            _db.AddRange(trucks);

            var milk = products.First(p => p.Code == "RAW_MILK");
            var cream = products.First(p => p.Code == "CREAM");
            var contracts = new List<Contract>();
            for (var i = 0; i < Math.Min(Math.Max(3, resolved.CustomerCount / 10), customers.Count); i++)
            {
                var product = products[i % products.Count];
                contracts.Add(new Contract
                {
                    Id = GuidFor(id, resolved.RandomSeed, $"contract-{i}"),
                    GenerationId = id,
                    CustomerId = customers[i].Id,
                    ProductId = product.Id,
                    StartDate = resolved.StartDate,
                    EndDate = resolved.EndDate,
                    MinimumQuantityPounds = 5_000,
                    MaximumQuantityPounds = 80_000,
                    PricePerPound = product.Code.Contains("CREAM", StringComparison.OrdinalIgnoreCase) ? 0.90m : 0.20m,
                    ShortfallPenaltyPerPound = 0.05m,
                    Active = true
                });
            }
            _db.AddRange(contracts);

            var dayCount = resolved.EndDate.DayNumber - resolved.StartDate.DayNumber + 1;
            var denseStart = Math.Max(0, dayCount - resolved.DenseHistoryDays);
            var lots = new List<InventoryLot>();
            var orders = new List<Order>();
            var prices = new List<MarketPrice>();
            var weather = new List<WeatherObservation>();
            var shipments = new List<Shipment>();
            var plannedSlots = 0;
            var missingSlots = 0;
            var milkQtysSample = new List<decimal>();
            decimal prevMilkPrice = 0.18m;
            decimal prevCreamPrice = 0.95m;

            for (var d = 0; d < dayCount; d++)
            {
                var inDense = d >= denseStart;
                if (!inDense && resolved.SparseCadenceDays > 1 && d % resolved.SparseCadenceDays != 0)
                    continue;

                var day = resolved.StartDate.AddDays(d);
                var seasonal = 1.0m + 0.12m * (decimal)Math.Sin(2 * Math.PI * d / 365.0);
                var heat = (decimal)(0.5 + 0.5 * Math.Sin(2 * Math.PI * (d - 180) / 365.0));
                var heatStress = DomainInvariants.Money(Math.Max(0, heat - 0.55m) * 10m);

                foreach (var region in new[] { "R1", "R2", "R3" })
                {
                    weather.Add(new WeatherObservation
                    {
                        Id = GuidFor(id, resolved.RandomSeed, $"wx-{region}-{d}"),
                        GenerationId = id,
                        RegionCode = region,
                        ObservationDate = day,
                        TemperatureF = DomainInvariants.Money(55m + heat * 35m + (decimal)(rng.NextDouble() * 4 - 2)),
                        HeatStressIndex = heatStress
                    });
                }

                prevMilkPrice = DomainInvariants.Money(prevMilkPrice * 0.85m + 0.18m * seasonal * 0.15m + (decimal)(rng.NextDouble() * 0.01 - 0.005));
                prevCreamPrice = DomainInvariants.Money(prevCreamPrice * 0.85m + 0.95m * seasonal * 0.15m + (decimal)(rng.NextDouble() * 0.02 - 0.01));
                if (d > 0 && d % 90 == 0)
                    prevMilkPrice = DomainInvariants.Money(prevMilkPrice * 1.08m);

                foreach (var product in products)
                {
                    var basePrice = product.Code switch
                    {
                        "CREAM" => prevCreamPrice,
                        "RAW_MILK" => prevMilkPrice,
                        _ => 0.25m * seasonal
                    };
                    prices.Add(new MarketPrice
                    {
                        Id = GuidFor(id, resolved.RandomSeed, $"price-{product.Code}-{d}"),
                        GenerationId = id,
                        ProductId = product.Id,
                        EffectiveDate = day,
                        PricePerPound = DomainInvariants.Money(basePrice),
                        PriceType = MarketPriceType.StaticSpot,
                        Source = "synthetic-static"
                    });
                }

                foreach (var facility in facilities)
                {
                    plannedSlots++;
                    if (rng.NextDouble() < (double)resolved.MissingnessRate)
                    {
                        missingSlots++;
                        continue;
                    }

                    var supplyShock = 1.0m - heatStress * 0.02m;
                    var milkQty = DomainInvariants.Money(8_000m * seasonal * supplyShock + rng.Next(0, 500));
                    milkQtysSample.Add(milkQty);
                    var creamQty = DomainInvariants.Money(milkQty * (0.10m + 0.04m * seasonal));
                    var produced = new DateTimeOffset(day.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc));

                    lots.Add(MakeLot(id, resolved.RandomSeed, $"lot-milk-{facility.Id}-{d}", facility.Id, milk.Id, milkQty, 3.7m, produced, milk.MaximumAgeHours, day));
                    lots.Add(MakeLot(id, resolved.RandomSeed, $"lot-cream-{facility.Id}-{d}", facility.Id, cream.Id, creamQty, 36m, produced, cream.MaximumAgeHours, day));

                    if (products.Count > 2 && inDense)
                    {
                        var extra = products[2 + (d + facilities.IndexOf(facility)) % Math.Max(1, products.Count - 2)];
                        lots.Add(MakeLot(id, resolved.RandomSeed, $"lot-{extra.Code}-{facility.Id}-{d}", facility.Id, extra.Id,
                            DomainInvariants.Money(milkQty * 0.05m), 2.5m, produced, extra.MaximumAgeHours, day));
                    }
                }

                foreach (var customer in customers)
                {
                    if (!inDense && rng.NextDouble() < 0.5) continue;
                    var product = products[(d + customers.IndexOf(customer)) % products.Count];
                    var price = product.Code == "CREAM" ? 1.05m : product.Code == "RAW_MILK" ? 0.22m : 0.40m;
                    if (customer.Name.EndsWith("5", StringComparison.Ordinal)) price += 0.35m;
                    var demandSeason = 1.0m + 0.1m * (decimal)Math.Sin(2 * Math.PI * (d + 30) / 365.0);
                    var qty = DomainInvariants.Money((2_000m + rng.Next(0, 1500)) * demandSeason);
                    var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc));
                    var end = new DateTimeOffset(day.ToDateTime(new TimeOnly(20, 0), DateTimeKind.Utc));
                    orders.Add(new Order
                    {
                        Id = GuidFor(id, resolved.RandomSeed, $"order-{customer.Id}-{d}-{product.Code}"),
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

                if (inDense && d % 5 == 0 && trucks.Count > 0)
                {
                    var truck = trucks[d % trucks.Count];
                    var dest = customers[d % customers.Count];
                    shipments.Add(new Shipment
                    {
                        Id = GuidFor(id, resolved.RandomSeed, $"ship-{d}"),
                        GenerationId = id,
                        OriginFacilityId = facilities[d % facilities.Count].Id,
                        DestinationType = DestinationType.Customer,
                        DestinationId = dest.Id,
                        ProductId = milk.Id,
                        QuantityPounds = 10_000,
                        TruckId = truck.Id,
                        DepartedAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc)),
                        ArrivedAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc)),
                        Status = ShipmentStatus.Completed
                    });
                }
            }

            _db.AddRange(prices);
            _db.AddRange(lots);
            _db.AddRange(orders);
            _db.AddRange(weather);
            _db.AddRange(shipments);

            var observedMissing = plannedSlots == 0 ? 0 : (decimal)missingSlots / plannedSlots;
            var seasonalDetected = DetectSeasonality(milkQtysSample);
            var report = BuildValidationReport(farms, facilities, customers, products, lots, orders, contracts, trucks, observedMissing, seasonalDetected, resolved.MissingnessRate);
            if (!report.Passed)
            {
                manifest.Status = GenerationRunStatus.Failed;
                manifest.FailureMessage = "Validation report failed critical checks.";
                manifest.ValidationReportJson = JsonSerializer.Serialize(report);
                manifest.EntityCountsJson = "{}";
                await _db.SaveChangesAsync(cancellationToken);
                return manifest;
            }

            var counts = new Dictionary<string, int>
            {
                ["farms"] = farms.Count,
                ["facilities"] = facilities.Count,
                ["customers"] = customers.Count,
                ["trucks"] = trucks.Count,
                ["products"] = products.Count,
                ["contracts"] = contracts.Count,
                ["inventoryLots"] = lots.Count,
                ["orders"] = orders.Count,
                ["marketPrices"] = prices.Count,
                ["weatherObservations"] = weather.Count,
                ["shipments"] = shipments.Count
            };
            manifest.EntityCountsJson = JsonSerializer.Serialize(counts);
            manifest.ValidationReportJson = JsonSerializer.Serialize(report);
            manifest.Status = GenerationRunStatus.Completed;
            await _db.SaveChangesAsync(cancellationToken);
            return manifest;
        }
        catch (Exception ex)
        {
            manifest.Status = GenerationRunStatus.Failed;
            manifest.FailureMessage = ex.Message;
            manifest.ValidationReportJson = JsonSerializer.Serialize(new ValidationReport
            {
                Passed = false,
                Checks = [new ValidationCheck { Name = "exception", Passed = false, Severity = "Critical", Message = ex.Message }]
            });
            await _db.SaveChangesAsync(cancellationToken);
            return manifest;
        }
    }

    private static ResolvedProfile Resolve(SyntheticGenerationRequest request)
    {
        var profileName = string.IsNullOrWhiteSpace(request.ProfileName) ? GenerationProfileCatalog.ThinSlice : request.ProfileName;
        GenerationProfileDefinition? named = null;
        if (!profileName.Equals(GenerationProfileCatalog.Custom, StringComparison.OrdinalIgnoreCase))
            named = GenerationProfileCatalog.Find(profileName);

        if (named is null && !profileName.Equals(GenerationProfileCatalog.Custom, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown profile '{profileName}'.");

        return new ResolvedProfile(
            ProfileName: named?.Name ?? GenerationProfileCatalog.Custom,
            ScenarioName: request.ScenarioName ?? named?.Name ?? "custom",
            SchemaVersion: string.IsNullOrWhiteSpace(request.SchemaVersion) ? GenerationProfileCatalog.SchemaVersion : request.SchemaVersion,
            RandomSeed: request.RandomSeed,
            StartDate: request.StartDate ?? named?.StartDate ?? new DateOnly(2025, 10, 1),
            EndDate: request.EndDate ?? named?.EndDate ?? new DateOnly(2025, 12, 29),
            FarmCount: request.FarmCount ?? named?.FarmCount ?? 5,
            FacilityCount: request.FacilityCount ?? named?.FacilityCount ?? 2,
            CustomerCount: request.CustomerCount ?? named?.CustomerCount ?? 5,
            TruckCount: request.TruckCount ?? named?.TruckCount ?? 3,
            ProductSet: request.ProductSet ?? named?.ProductSet ?? "milk-cream",
            MissingnessRate: request.MissingnessRate ?? named?.MissingnessRate ?? 0.02m,
            DenseHistoryDays: request.DenseHistoryDays ?? named?.DenseHistoryDays ?? 90,
            SparseCadenceDays: request.SparseCadenceDays ?? named?.SparseCadenceDays ?? 1);
    }

    private static void ValidateOrThrow(ResolvedProfile p)
    {
        var limits = GenerationProfileCatalog.Limits;
        if (p.FarmCount <= 0) throw new ArgumentException("FarmCount must be > 0.");
        if (p.FacilityCount <= 0) throw new ArgumentException("FacilityCount must be > 0.");
        if (p.CustomerCount <= 0) throw new ArgumentException("CustomerCount must be > 0.");
        if (p.TruckCount <= 0) throw new ArgumentException("TruckCount must be > 0.");
        if (p.EndDate < p.StartDate) throw new ArgumentException("EndDate must be >= StartDate.");
        var days = p.EndDate.DayNumber - p.StartDate.DayNumber + 1;
        if (days > limits.MaxDaySpan) throw new ArgumentException($"Date range exceeds max {limits.MaxDaySpan} days.");
        if (p.FarmCount > limits.MaxFarms || p.FacilityCount > limits.MaxFacilities ||
            p.CustomerCount > limits.MaxCustomers || p.TruckCount > limits.MaxTrucks)
            throw new ArgumentException("Profile exceeds documented maximum entity bounds.");
        if (p.MissingnessRate < 0 || p.MissingnessRate > 0.5m)
            throw new ArgumentException("MissingnessRate must be between 0 and 0.5.");
    }

    private static List<Product> BuildProducts(Guid generationId, int seed, string productSet)
    {
        var defs = productSet.Equals("standard-six", StringComparison.OrdinalIgnoreCase)
            ? new (string Code, string Name, int Age)[]
            {
                ("RAW_MILK", "Raw Milk", 72),
                ("CREAM", "Cream", 48),
                ("SKIM_MILK", "Skim Milk", 72),
                ("CLASS_I_FLUID", "Class I Fluid Milk", 96),
                ("CLASS_II_CREAM", "Class II Cream Product", 72),
                ("CHEESE_MILK", "Cheese Milk", 72)
            }
            : new (string Code, string Name, int Age)[]
            {
                ("RAW_MILK", "Raw Milk", 72),
                ("CREAM", "Cream", 48)
            };

        return defs.Select(d => new Product
        {
            Id = GuidFor(generationId, seed, d.Code),
            GenerationId = generationId,
            Code = d.Code,
            Name = d.Name,
            MaximumAgeHours = d.Age,
            UnitOfMeasure = "lb",
            Active = true
        }).ToList();
    }

    private static InventoryLot MakeLot(Guid genId, int seed, string key, Guid facilityId, Guid productId, decimal qty, decimal bf, DateTimeOffset produced, int maxAge, DateOnly asOf) => new()
    {
        Id = GuidFor(genId, seed, key),
        GenerationId = genId,
        FacilityId = facilityId,
        ProductId = productId,
        QuantityPounds = qty,
        ButterfatPercent = bf,
        ProducedAt = produced,
        ExpiresAt = produced.AddHours(maxAge),
        QualityGrade = "A",
        Status = InventoryLotStatus.Available,
        AsOfDate = asOf
    };

    private static bool DetectSeasonality(List<decimal> samples)
    {
        if (samples.Count < 20) return samples.Count >= 2 && samples.Max() - samples.Min() > samples.Average() * 0.05m;
        var first = samples.Take(samples.Count / 2).Average();
        var second = samples.Skip(samples.Count / 2).Average();
        return Math.Abs(first - second) > 50m || samples.Max() - samples.Min() > samples.Average() * 0.08m;
    }

    private static ValidationReport BuildValidationReport(
        List<Farm> farms, List<Facility> facilities, List<Customer> customers, List<Product> products,
        List<InventoryLot> lots, List<Order> orders, List<Contract> contracts, List<Truck> trucks,
        decimal observedMissing, bool seasonal, decimal configuredMissing)
    {
        var checks = new List<ValidationCheck>
        {
            new() { Name = "farms-present", Passed = farms.Count > 0, Severity = "Critical", Message = $"{farms.Count} farms" },
            new() { Name = "facilities-present", Passed = facilities.Count > 0, Severity = "Critical", Message = $"{facilities.Count} facilities" },
            new() { Name = "referential-lots", Passed = lots.All(l => facilities.Any(f => f.Id == l.FacilityId) && products.Any(p => p.Id == l.ProductId)), Severity = "Critical", Message = "Lot FKs valid" },
            new() { Name = "referential-orders", Passed = orders.All(o => customers.Any(c => c.Id == o.CustomerId) && products.Any(p => p.Id == o.ProductId)), Severity = "Critical", Message = "Order FKs valid" },
            new() { Name = "lot-invariants", Passed = lots.All(l => { try { DomainInvariants.ValidateInventoryLot(l); return true; } catch { return false; } }), Severity = "Critical", Message = "Inventory lot invariants" },
            new() { Name = "order-invariants", Passed = orders.All(o => { try { DomainInvariants.ValidateOrder(o); return true; } catch { return false; } }), Severity = "Critical", Message = "Order invariants" },
            new() { Name = "contract-invariants", Passed = contracts.All(c => { try { DomainInvariants.ValidateContract(c); return true; } catch { return false; } }), Severity = "Critical", Message = "Contract invariants" },
            new() { Name = "truck-home-facility", Passed = trucks.All(t => facilities.Any(f => f.Id == t.HomeFacilityId)), Severity = "Critical", Message = "Truck homes valid" },
            new() { Name = "synthetic-names", Passed = farms.All(f => f.Name.StartsWith("Synthetic", StringComparison.Ordinal)), Severity = "Info", Message = "Synthetic farm naming" },
            new() { Name = "missingness-band", Passed = Math.Abs(observedMissing - configuredMissing) <= 0.05m || lots.Count < 10, Severity = "Info", Message = $"Observed missingness {observedMissing:P1} vs configured {configuredMissing:P1}" },
            new() { Name = "seasonal-variation", Passed = seasonal, Severity = "Info", Message = seasonal ? "Seasonal variation detected" : "Insufficient variation (small sample)" }
        };

        return new ValidationReport
        {
            Passed = checks.Where(c => c.Severity == "Critical").All(c => c.Passed),
            Checks = checks,
            ObservedMissingnessRate = DomainInvariants.Money(observedMissing),
            SeasonalVariationDetected = seasonal
        };
    }

    private static Guid GuidFor(Guid generationId, int seed, string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{generationId:N}:{seed}:{key}"));
        Span<byte> g = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(g);
        return new Guid(g);
    }

    private sealed record ResolvedProfile(
        string ProfileName, string ScenarioName, string SchemaVersion, int RandomSeed,
        DateOnly StartDate, DateOnly EndDate, int FarmCount, int FacilityCount, int CustomerCount, int TruckCount,
        string ProductSet, decimal MissingnessRate, int DenseHistoryDays, int SparseCadenceDays);
}

/// <summary>Backward-compatible alias for tests and older call sites.</summary>
public sealed class ThinSliceGenerator : IThinSliceGenerator
{
    private readonly SyntheticDataGenerator _inner;
    public ThinSliceGenerator(IDairyDnaDbContext db) => _inner = new SyntheticDataGenerator(db);
    public ThinSliceGenerator(SyntheticDataGenerator inner) => _inner = inner;
    public Task<GenerationManifest> GenerateAsync(ThinSliceGenerationRequest request, CancellationToken cancellationToken = default)
        => _inner.GenerateAsync(request, cancellationToken);
}
