using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Forecasting;
using DairyDNA.Application.Governance;
using DairyDNA.DataGenerator;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Forecasting;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.UnitTests;

public class ModelGovernanceServiceTests
{
    [Fact]
    public async Task Publish_without_checksum_throws()
    {
        await using var db = CreateDb();
        var model = new SupplyModelVersion
        {
            Id = Guid.NewGuid(),
            GenerationId = Guid.NewGuid(),
            MeetsAcceptanceBar = true,
            Status = ForecastRunStatus.Completed,
            TrainedAt = DateTimeOffset.UtcNow,
            ArtifactChecksumSha256 = null
        };
        db.Add(model);
        await db.SaveChangesAsync();

        var service = new ModelGovernanceService(db);
        var act = async () => await service.PublishAsync(model.Id, "tester", "Ready for demo", overrideQualityGate: false);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Publish_sets_lifecycle_and_writes_audit_event()
    {
        await using var db = CreateDb();
        var model = new SupplyModelVersion
        {
            Id = Guid.NewGuid(),
            GenerationId = Guid.NewGuid(),
            MeetsAcceptanceBar = true,
            Status = ForecastRunStatus.Completed,
            TrainedAt = DateTimeOffset.UtcNow,
            ArtifactChecksumSha256 = "abc123"
        };
        db.Add(model);
        await db.SaveChangesAsync();

        var service = new ModelGovernanceService(db);
        var published = await service.PublishAsync(model.Id, "tester", "Ready for demo", overrideQualityGate: false);

        published.LifecycleStatus.Should().Be(ModelLifecycleStatus.Published);
        published.PublishedAt.Should().NotBeNull();

        var audit = await db.GovernanceAuditEvents.Where(a => a.ModelVersionId == model.Id).ToListAsync();
        audit.Should().ContainSingle();
        audit[0].Action.Should().Be("Publish");
        audit[0].Actor.Should().Be("tester");
        audit[0].Reason.Should().Be("Ready for demo");
    }

    [Fact]
    public async Task Publish_below_bar_requires_override_and_is_audited_as_override()
    {
        await using var db = CreateDb();
        var model = new SupplyModelVersion
        {
            Id = Guid.NewGuid(),
            GenerationId = Guid.NewGuid(),
            MeetsAcceptanceBar = false,
            Status = ForecastRunStatus.CompletedBelowBar,
            TrainedAt = DateTimeOffset.UtcNow,
            ArtifactChecksumSha256 = "abc123"
        };
        db.Add(model);
        await db.SaveChangesAsync();

        var service = new ModelGovernanceService(db);
        var act = async () => await service.PublishAsync(model.Id, "tester", "Try anyway", overrideQualityGate: false);
        await act.Should().ThrowAsync<InvalidOperationException>();

        var published = await service.PublishAsync(model.Id, "tester", "Try anyway", overrideQualityGate: true);
        published.LifecycleStatus.Should().Be(ModelLifecycleStatus.Published);

        var audit = await db.GovernanceAuditEvents.Where(a => a.ModelVersionId == model.Id).ToListAsync();
        audit.Should().ContainSingle(a => a.Action == "Override");
    }

    [Fact]
    public async Task Retiring_a_published_model_removes_it_from_default_inference_selection()
    {
        await using var db = CreateDb();
        var generator = new SyntheticDataGenerator(db);
        var gen = await generator.GenerateAsync(new ThinSliceGenerationRequest { RandomSeed = 104729 });

        var forecastService = new MlNetSupplyForecastService(db);
        var trained = await forecastService.TrainAndPublishAsync(new SupplyForecastRequest { GenerationId = gen.Id, RandomSeed = 104729 });
        trained.ArtifactChecksumSha256.Should().NotBeNullOrWhiteSpace();
        trained.LifecycleStatus.Should().Be(ModelLifecycleStatus.Candidate);

        var governance = new ModelGovernanceService(db);
        await governance.PublishAsync(trained.Id, "tester", "Promote to published", overrideQualityGate: true);

        var preferred = await forecastService.GetLatestModelAsync(gen.Id);
        preferred.Should().NotBeNull();
        preferred!.Id.Should().Be(trained.Id);
        preferred.LifecycleStatus.Should().Be(ModelLifecycleStatus.Published);

        await governance.RetireAsync(trained.Id, "tester", "Superseded by newer training run");

        var afterRetirement = await forecastService.GetLatestModelAsync(gen.Id);
        (afterRetirement is null || afterRetirement.Id != trained.Id).Should().BeTrue(
            "a retired model must not be the default selection for new inference");
    }

    [Fact]
    public async Task List_filters_by_family()
    {
        await using var db = CreateDb();
        var generationId = Guid.NewGuid();
        db.Add(new SupplyModelVersion { Id = Guid.NewGuid(), GenerationId = generationId, ArtifactChecksumSha256 = "s1", TrainedAt = DateTimeOffset.UtcNow });
        db.Add(new DemandModelVersion { Id = Guid.NewGuid(), GenerationId = generationId, ArtifactChecksumSha256 = "d1", TrainedAt = DateTimeOffset.UtcNow });
        db.Add(new PriceModelVersion { Id = Guid.NewGuid(), GenerationId = generationId, ArtifactChecksumSha256 = "p1", TrainedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new ModelGovernanceService(db);
        var all = await service.ListAsync(null);
        all.Should().HaveCount(3);

        var supplyOnly = await service.ListAsync("supply");
        supplyOnly.Should().ContainSingle();
        supplyOnly[0].ModelFamily.Should().Be("supply");

        var demandOnly = await service.ListAsync("Demand");
        demandOnly.Should().ContainSingle();
        demandOnly[0].ModelFamily.Should().Be("demand");
    }

    [Fact]
    public async Task Card_includes_limitation_text_and_baseline_metrics()
    {
        await using var db = CreateDb();
        var metrics = new ForecastMetrics { ModelWape7 = 0.10m, BaselineWape7 = 0.20m, WapeImprovementPct = 50m, MeetsAcceptanceBar = true };
        var model = new SupplyModelVersion
        {
            Id = Guid.NewGuid(),
            GenerationId = Guid.NewGuid(),
            MeetsAcceptanceBar = true,
            Status = ForecastRunStatus.Completed,
            TrainedAt = DateTimeOffset.UtcNow,
            ArtifactChecksumSha256 = "abc123",
            MetricsJson = JsonSerializer.Serialize(metrics)
        };
        db.Add(model);
        await db.SaveChangesAsync();

        var service = new ModelGovernanceService(db);
        var card = await service.GetCardAsync(model.Id);

        card.Should().NotBeNull();
        card!.Limitations.Should().Contain("not production advice");
        card.LeakageControlStatement.Should().NotBeNullOrWhiteSpace();
        card.Metrics.Should().NotBeNull();
        card.Metrics!.ModelWape7.Should().Be(0.10m);
        card.Metrics.BaselineWape7.Should().Be(0.20m);
    }

    private static DairyDnaDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
