using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Diagnostics;
using DairyDNA.Application.Generation;
using DairyDNA.Application.Optimization;
using DairyDNA.Application.Scenarios;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Application.Demo;

/// <summary>Optional overrides; defaults reproduce the versioned <see cref="DemoSeedPack"/>.</summary>
public sealed class DemoBootstrapRequest
{
    public string ProfileName { get; set; } = DemoSeedPack.ProfileName;
    public int RandomSeed { get; set; } = DemoSeedPack.RandomSeed;
    public bool ApplyFlagshipPack { get; set; } = true;
}

public sealed record DemoBootstrapResult(
    Guid GenerationId,
    string ProfileName,
    int RandomSeed,
    DateOnly PlanningDate,
    string GenerationStatus,
    Guid? OptimizationRunId,
    string? OptimizationStatus,
    decimal? ObjectiveValue,
    IReadOnlyList<string> FlagshipScenarioNames,
    string DataClassification);

/// <summary>
/// One-shot demo bring-up (spec 013 FR-001): generate the fixed <see cref="DemoSeedPack"/> dataset,
/// apply the flagship scenario pack (010), and run one optimization — so a presenter or CI check can
/// reach a ready demo state with a single call instead of three. Thin composition over existing
/// handlers; no new business logic.
/// </summary>
public sealed class DemoBootstrapHandler
{
    private readonly CreateGenerationRunHandler _generation;
    private readonly CreateOptimizationRunHandler _optimization;
    private readonly IScenarioService _scenarios;

    public DemoBootstrapHandler(
        CreateGenerationRunHandler generation,
        CreateOptimizationRunHandler optimization,
        IScenarioService scenarios)
    {
        _generation = generation;
        _optimization = optimization;
        _scenarios = scenarios;
    }

    public async Task<DemoBootstrapResult> HandleAsync(DemoBootstrapRequest request, CancellationToken ct = default)
    {
        using var activity = DairyDnaTelemetry.Source.StartActivity("DairyDNA.Demo.Bootstrap");

        var manifest = await _generation.HandleAsync(new SyntheticGenerationRequest
        {
            ProfileName = request.ProfileName,
            RandomSeed = request.RandomSeed
        }, ct);

        if (manifest.Status != GenerationRunStatus.Completed)
        {
            return new DemoBootstrapResult(
                manifest.Id, manifest.ProfileName, manifest.RandomSeed, manifest.PlanningDate,
                manifest.Status.ToString(), null, null, null, [], DemoSeedPack.DataClassification);
        }

        IReadOnlyList<string> flagshipNames = [];
        if (request.ApplyFlagshipPack)
        {
            var created = await _scenarios.ApplyFlagshipPack(manifest.Id, ct);
            flagshipNames = created.Count > 0 ? created.Select(s => s.Name).ToList() : DemoSeedPack.FlagshipScenarioNames;
        }

        var run = await _optimization.HandleAsync(new CreateOptimizationRunRequest { GenerationId = manifest.Id }, ct);

        return new DemoBootstrapResult(
            manifest.Id,
            manifest.ProfileName,
            manifest.RandomSeed,
            manifest.PlanningDate,
            manifest.Status.ToString(),
            run?.Id,
            run?.Status.ToString(),
            run?.ObjectiveValue,
            flagshipNames,
            DemoSeedPack.DataClassification);
    }
}
