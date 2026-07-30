using DairyDNA.Application.Scenarios;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.UnitTests;

public class ScenarioServiceTests
{
    [Fact]
    public async Task Flagship_pack_creates_documented_scenarios_once()
    {
        await using var db = CreateDb();
        db.Add(new GenerationManifest { Id = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var generationId = db.GenerationManifests.Single().Id;
        var service = new ScenarioService(db, null!);

        var created = await service.ApplyFlagshipPack(generationId);
        var repeat = await service.ApplyFlagshipPack(generationId);

        created.Select(s => s.Name).Should().BeEquivalentTo("diesel-rise", "distant-high-price", "capacity-loss", "demand-spike");
        created.Should().OnlyContain(s => s.IsFlagshipPackMember && s.OverridesJson != "{}");
        repeat.Should().BeEmpty();
    }

    [Fact]
    public async Task Compare_marks_failed_scenario_as_not_recommended()
    {
        await using var db = CreateDb();
        var generationId = Guid.NewGuid();
        var baseRun = new OptimizationRun { Id = Guid.NewGuid(), GenerationId = generationId, Status = OptimizationRunStatus.Feasible, UnservedDemandJson = "[]", UnusedInventoryJson = "[]" };
        var scenarioOptimization = new OptimizationRun { Id = Guid.NewGuid(), GenerationId = generationId, Status = OptimizationRunStatus.Failed, UnservedDemandJson = "[]", UnusedInventoryJson = "[]" };
        db.AddRange(baseRun, scenarioOptimization);
        db.Add(new ScenarioRun { Id = Guid.NewGuid(), OptimizationRunId = scenarioOptimization.Id, Status = OptimizationRunStatus.Failed });
        await db.SaveChangesAsync();

        var comparison = await new ScenarioService(db, null!).Compare(baseRun.Id, db.ScenarioRuns.Single().Id);

        comparison!.Scenario.IsRecommended.Should().BeFalse();
        comparison.HonestyLabel.Should().Contain("not a recommendation");
    }

    private static DairyDnaDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<DairyDnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
