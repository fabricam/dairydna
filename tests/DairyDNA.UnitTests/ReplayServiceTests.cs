using System.Text.Json;
using DairyDNA.Application.Optimization;
using DairyDNA.Application.Replay;
using DairyDNA.Application.Transport;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Infrastructure.Persistence;
using DairyDNA.Optimization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.UnitTests;

public class ReplayServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Replay_run_links_optimization_run_and_records_versions()
    {
        await using var db = CreateDb();
        var generationId = Seed(db, days: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var startDate = new DateOnly(2026, 1, 1);

        var replay = await service.RunAsync(generationId, startDate, "Spot");

        replay.OptimizationRunId.Should().NotBeEmpty();
        replay.OptimizerVersion.Should().NotBeNullOrWhiteSpace();
        replay.CostingModelVersion.Should().Be(TransportCostCalculator.CostingModelVersion);
        replay.DataClassification.Should().Be("Synthetic");
        (await db.OptimizationRuns.CountAsync(r => r.Id == replay.OptimizationRunId)).Should().Be(1);
    }

    [Fact]
    public async Task AsOfDate_outside_dataset_window_is_rejected()
    {
        await using var db = CreateDb();
        var generationId = Seed(db, days: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var act = async () => await service.RunAsync(generationId, new DateOnly(2020, 1, 1), "Spot");

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Leakage_audit_passes_for_a_normal_synthetic_day()
    {
        await using var db = CreateDb();
        var generationId = Seed(db, days: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var startDate = new DateOnly(2026, 1, 1);

        var replay = await service.RunAsync(generationId, startDate, "Spot");

        replay.LeakagePassed.Should().BeTrue();
        var audit = JsonSerializer.Deserialize<LeakageAuditResult>(replay.LeakageAuditJson, JsonOptions)!;
        audit.Passed.Should().BeTrue();
        audit.InventoryLotsChecked.Should().BeGreaterThan(0);
        audit.OrdersChecked.Should().BeGreaterThan(0);
        audit.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task Dual_replay_is_deterministic()
    {
        await using var db = CreateDb();
        var generationId = Seed(db, days: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var day = new DateOnly(2026, 1, 2);

        var first = await service.RunAsync(generationId, day, "Spot");
        var second = await service.RunAsync(generationId, day, "Spot");

        first.ObjectiveValue.Should().Be(second.ObjectiveValue);

        var firstMovements = await db.RecommendedMovements.Where(m => m.OptimizationRunId == first.OptimizationRunId).OrderBy(m => m.Id).ToListAsync();
        var secondMovements = await db.RecommendedMovements.Where(m => m.OptimizationRunId == second.OptimizationRunId).OrderBy(m => m.Id).ToListAsync();
        firstMovements.Select(m => m.QuantityPounds).OrderBy(q => q).Should().Equal(secondMovements.Select(m => m.QuantityPounds).OrderBy(q => q));

        var firstCost = firstMovements.Sum(m => m.TransportationCost);
        var secondCost = secondMovements.Sum(m => m.TransportationCost);
        Math.Abs(firstCost - secondCost).Should().BeLessThanOrEqualTo(0.01m);
    }

    [Fact]
    public async Task Regret_report_includes_optimizer_and_two_or_more_baselines()
    {
        await using var db = CreateDb();
        var generationId = Seed(db, days: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var report = await service.BuildRegretReportAsync(generationId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3));

        report.Days.Should().HaveCount(3);
        foreach (var day in report.Days)
        {
            day.Baselines.Should().HaveCountGreaterThanOrEqualTo(2);
            day.Baselines.Select(b => b.PolicyName).Should().Contain(["NearestCustomerGreedy", "HighestPriceFirst"]);
            day.Optimizer.ObjectiveValue.Should().NotBe(default(decimal));
        }
        report.Summary.TotalDays.Should().Be(3);
        report.Summary.Statement.Should().NotBeNullOrWhiteSpace();

        var stored = await service.GetReportAsync(report.Id);
        stored.Should().NotBeNull();
        stored!.Days.Should().HaveCount(3);
    }

    /// <summary>Generates the checked-in regret-window fixture referenced by spec 012 (SC-002).</summary>
    [Fact]
    public async Task Regret_report_fixture_is_written_for_the_demo_window()
    {
        await using var db = CreateDb();
        var generationId = Seed(db, days: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var report = await service.BuildRegretReportAsync(generationId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3));

        var fixturesDir = FindFixturesDirectory();
        Directory.CreateDirectory(fixturesDir);
        var path = Path.Combine(fixturesDir, "regret-window-sample.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));

        File.Exists(path).Should().BeTrue();
    }

    /// <summary>Resolves the repo-root fixtures directory from this file's compile-time path so the
    /// fixture lands under source control even when tests build/run with a custom --output directory.</summary>
    private static string FindFixturesDirectory([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var dir = Path.GetDirectoryName(sourceFile) ?? AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "specs")))
                return Path.Combine(dir, "specs", "012-historical-replay", "fixtures");
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        throw new DirectoryNotFoundException($"Could not locate the repo 'specs' directory above '{sourceFile}'.");
    }

    private static ReplayService CreateService(DairyDnaDbContext db) =>
        new(db, new CreateOptimizationRunHandler(db, new AllocationOptimizerResolver(new OrToolsContributionMarginOptimizer(), new NaiveContributionMarginOptimizer()), new TransportCostCalculator()),
            new TransportCostCalculator());

    private static DairyDnaDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Guid Seed(DairyDnaDbContext db, int days)
    {
        var generationId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = startDate.AddDays(days - 1);

        db.Add(new GenerationManifest
        {
            Id = generationId,
            ScenarioName = "replay-tests",
            StartDate = startDate,
            EndDate = endDate,
            PlanningDate = endDate,
            Status = GenerationRunStatus.Completed,
            GeneratedAt = DateTimeOffset.UtcNow
        });

        var facilityId = Guid.NewGuid();
        var nearCustomerId = Guid.NewGuid();
        var farCustomerId = Guid.NewGuid();
        var milkId = Guid.NewGuid();
        var truckId = Guid.NewGuid();

        db.Add(new Facility { Id = facilityId, GenerationId = generationId, Name = "F1", Latitude = 43m, Longitude = -89m, Active = true });
        db.Add(new Product { Id = milkId, GenerationId = generationId, Code = "RAW_MILK", Name = "Milk", MaximumAgeHours = 72 });
        db.Add(new Customer { Id = nearCustomerId, GenerationId = generationId, Name = "Near Co", Latitude = 43.05m, Longitude = -89.05m, Active = true });
        db.Add(new Customer { Id = farCustomerId, GenerationId = generationId, Name = "Far Co", Latitude = 47.5m, Longitude = -93m, Active = true });
        db.Add(new Truck
        {
            Id = truckId,
            GenerationId = generationId,
            HomeFacilityId = facilityId,
            MaximumCapacityPounds = 50_000,
            CompatibleProductCodes = "RAW_MILK,CREAM",
            CostPerMile = 1.25m,
            CostPerHour = 55m,
            Status = TruckStatus.Available,
            AvailableFrom = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            AvailableUntil = endDate.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
        });

        for (var d = 0; d < days; d++)
        {
            var day = startDate.AddDays(d);
            var produced = new DateTimeOffset(day.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc));
            db.Add(new InventoryLot
            {
                Id = Guid.NewGuid(),
                GenerationId = generationId,
                FacilityId = facilityId,
                ProductId = milkId,
                QuantityPounds = 9_000,
                ButterfatPercent = 3.7m,
                ProducedAt = produced,
                ExpiresAt = produced.AddHours(60),
                Status = InventoryLotStatus.Available,
                AsOfDate = day
            });
            db.Add(new Order
            {
                Id = Guid.NewGuid(),
                GenerationId = generationId,
                CustomerId = nearCustomerId,
                ProductId = milkId,
                RequestedQuantityPounds = 2_500,
                MinimumAcceptableQuantityPounds = 500,
                RequestedDeliveryStart = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc)),
                RequestedDeliveryEnd = new DateTimeOffset(day.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc)),
                OfferedPricePerPound = 0.22m,
                Status = OrderStatus.Open,
                RequestDate = day
            });
            db.Add(new Order
            {
                Id = Guid.NewGuid(),
                GenerationId = generationId,
                CustomerId = farCustomerId,
                ProductId = milkId,
                RequestedQuantityPounds = 2_500,
                MinimumAcceptableQuantityPounds = 500,
                RequestedDeliveryStart = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc)),
                RequestedDeliveryEnd = new DateTimeOffset(day.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc)),
                OfferedPricePerPound = 0.45m,
                Status = OrderStatus.Open,
                RequestDate = day
            });
        }

        return generationId;
    }
}
