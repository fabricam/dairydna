using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Optimization;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Scenarios;

public interface IScenarioService
{
    Task<IReadOnlyList<ScenarioDefinition>> ListDefinitions(Guid? generationId, CancellationToken ct = default);
    Task<ScenarioDefinition> CreateDefinition(CreateScenarioDefinitionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ScenarioDefinition>> ApplyFlagshipPack(Guid generationId, CancellationToken ct = default);
    Task<ScenarioRun?> RunScenario(Guid scenarioId, Guid? baseOptimizationRunId, CancellationToken ct = default);
    Task<ScenarioComparison?> Compare(Guid baseRunId, Guid scenarioRunId, CancellationToken ct = default);
}

public sealed class ScenarioService : IScenarioService
{
    private readonly IDairyDnaDbContext _db;
    private readonly CreateOptimizationRunHandler _createOptimizationRun;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ScenarioService(IDairyDnaDbContext db, CreateOptimizationRunHandler createOptimizationRun)
    {
        _db = db;
        _createOptimizationRun = createOptimizationRun;
    }

    public async Task<IReadOnlyList<ScenarioDefinition>> ListDefinitions(Guid? generationId, CancellationToken ct = default) =>
        await _db.ScenarioDefinitions
            .Where(s => generationId == null || s.GenerationId == generationId)
            .OrderBy(s => s.Name).ThenByDescending(s => s.Version)
            .ToListAsync(ct);

    public async Task<ScenarioDefinition> CreateDefinition(CreateScenarioDefinitionRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ValidateOverrides(request.Overrides);
        if (!await _db.GenerationManifests.AnyAsync(g => g.Id == request.GenerationId, ct))
            throw new KeyNotFoundException("Generation was not found.");

        var version = (await _db.ScenarioDefinitions
            .Where(s => s.GenerationId == request.GenerationId && s.Name == request.Name)
            .Select(s => (int?)s.Version).MaxAsync(ct) ?? 0) + 1;
        var scenario = new ScenarioDefinition
        {
            Id = Guid.NewGuid(),
            GenerationId = request.GenerationId,
            Name = request.Name.Trim(),
            Version = version,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            OverridesJson = JsonSerializer.Serialize(request.Overrides, JsonOptions)
        };
        _db.Add(scenario);
        await _db.SaveChangesAsync(ct);
        return scenario;
    }

    public async Task<IReadOnlyList<ScenarioDefinition>> ApplyFlagshipPack(Guid generationId, CancellationToken ct = default)
    {
        if (!await _db.GenerationManifests.AnyAsync(g => g.Id == generationId, ct))
            throw new KeyNotFoundException("Generation was not found.");

        var pack = new[]
        {
            ("diesel-rise", "Diesel price rises to $5.25 per gallon.", new ScenarioOverrides { FuelPricePerGallon = 5.25m }),
            ("distant-high-price", "Distant customer offers receive a $0.18/lb price bump.", new ScenarioOverrides { DistantCustomerPriceBump = .18m }),
            ("capacity-loss", "Available inventory is reduced by 25% to model capacity loss.", new ScenarioOverrides { CapacityScaleFactor = .75m }),
            ("demand-spike", "Open demand increases by 30%.", new ScenarioOverrides { DemandScaleFactor = 1.30m })
        };
        var existingNames = await _db.ScenarioDefinitions
            .Where(s => s.GenerationId == generationId && s.IsFlagshipPackMember)
            .Select(s => s.Name).ToListAsync(ct);
        var created = new List<ScenarioDefinition>();
        foreach (var (name, description, overrides) in pack.Where(p => !existingNames.Contains(p.Item1, StringComparer.OrdinalIgnoreCase)))
        {
            var definition = new ScenarioDefinition
            {
                Id = Guid.NewGuid(),
                GenerationId = generationId,
                Name = name,
                Version = 1,
                Description = description,
                CreatedAt = DateTimeOffset.UtcNow,
                OverridesJson = JsonSerializer.Serialize(overrides, JsonOptions),
                IsFlagshipPackMember = true
            };
            _db.Add(definition);
            created.Add(definition);
        }
        await _db.SaveChangesAsync(ct);
        return created;
    }

    public async Task<ScenarioRun?> RunScenario(Guid scenarioId, Guid? baseOptimizationRunId, CancellationToken ct = default)
    {
        var scenario = await _db.ScenarioDefinitions.FirstOrDefaultAsync(s => s.Id == scenarioId, ct);
        if (scenario is null) return null;
        var overrides = JsonSerializer.Deserialize<ScenarioOverrides>(scenario.OverridesJson, JsonOptions)
            ?? throw new InvalidOperationException("Scenario overrides are invalid.");
        ValidateOverrides(overrides);

        if (baseOptimizationRunId is { } baseId)
        {
            var baseRun = await _db.OptimizationRuns.FirstOrDefaultAsync(r => r.Id == baseId, ct);
            if (baseRun is null || baseRun.GenerationId != scenario.GenerationId)
                throw new ArgumentException("Base optimization run must belong to the scenario generation.", nameof(baseOptimizationRunId));
        }

        var optimizationRun = await _createOptimizationRun.HandleAsync(new CreateOptimizationRunRequest
        {
            GenerationId = scenario.GenerationId,
            ScenarioOverrides = overrides
        }, ct);
        if (optimizationRun is null) return null;

        var scenarioRun = new ScenarioRun
        {
            Id = Guid.NewGuid(),
            ScenarioDefinitionId = scenario.Id,
            BaseOptimizationRunId = baseOptimizationRunId,
            OptimizationRunId = optimizationRun.Id,
            Status = optimizationRun.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            FuelPriceOverride = overrides.FuelPricePerGallon,
            Notes = overrides.HeatFlag == true ? "Heat flag recorded; no separate weather forecast overlay was applied." : null
        };
        _db.Add(scenarioRun);
        await _db.SaveChangesAsync(ct);
        return scenarioRun;
    }

    public async Task<ScenarioComparison?> Compare(Guid baseRunId, Guid scenarioRunId, CancellationToken ct = default)
    {
        var baseRun = await _db.OptimizationRuns.FirstOrDefaultAsync(r => r.Id == baseRunId, ct);
        var scenario = await _db.ScenarioRuns.FirstOrDefaultAsync(r => r.Id == scenarioRunId, ct);
        if (baseRun is null || scenario is null) return null;
        var scenarioOptimization = await _db.OptimizationRuns.FirstOrDefaultAsync(r => r.Id == scenario.OptimizationRunId, ct);
        if (scenarioOptimization is null || scenarioOptimization.GenerationId != baseRun.GenerationId)
            throw new ArgumentException("Runs must use the same generation.");

        var baseMoves = await _db.RecommendedMovements.Where(m => m.OptimizationRunId == baseRun.Id).ToListAsync(ct);
        var scenarioMoves = await _db.RecommendedMovements.Where(m => m.OptimizationRunId == scenarioOptimization.Id).ToListAsync(ct);
        var moves = baseMoves.Concat(scenarioMoves)
            .GroupBy(m => (m.OriginFacilityId, m.DestinationCustomerId, m.ProductId))
            .Select(g => new ScenarioMovementDiff(g.Key.OriginFacilityId, g.Key.DestinationCustomerId, g.Key.ProductId,
                g.Where(m => m.OptimizationRunId == baseRun.Id).Sum(m => m.QuantityPounds),
                g.Where(m => m.OptimizationRunId == scenarioOptimization.Id).Sum(m => m.QuantityPounds)))
            .Where(d => d.BaseQuantityPounds != d.ScenarioQuantityPounds)
            .ToList();
        return new ScenarioComparison(
            ToComparisonRun(baseRun),
            ToComparisonRun(scenarioOptimization),
            moves,
            "Scenario simulation",
            scenarioOptimization.Status == OptimizationRunStatus.Feasible
                ? "Scenario result — review before operational use."
                : "Scenario solve was not feasible; it is not a recommendation.");
    }

    private static ScenarioComparisonRun ToComparisonRun(OptimizationRun run) =>
        new(run.Id, run.Status, run.ObjectiveValue, JsonDocument.Parse(run.UnservedDemandJson).RootElement.GetArrayLength(),
            JsonDocument.Parse(run.UnusedInventoryJson).RootElement.GetArrayLength(), run.Status == OptimizationRunStatus.Feasible);

    private static void ValidateOverrides(ScenarioOverrides overrides)
    {
        if (overrides.FuelPricePerGallon is < 0 ||
            overrides.CapacityScaleFactor is <= 0 or > 10 ||
            overrides.DemandScaleFactor is <= 0 or > 10 ||
            overrides.DistantCustomerPriceBump is < 0)
            throw new ArgumentOutOfRangeException(nameof(overrides), "Scenario values must be non-negative and scale factors must be greater than zero.");
    }
}
