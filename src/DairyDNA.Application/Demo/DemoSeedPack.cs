using DairyDNA.Application.Abstractions;

namespace DairyDNA.Application.Demo;

/// <summary>
/// The single, versioned demo dataset/scenario combination presenters should use so two runs of
/// the flagship story produce the same logical outcomes (spec 013, User Story 2). Values here are
/// the source of truth referenced by <c>docs/demo/seed-pack.md</c> and by
/// <see cref="DemoBootstrapHandler"/>; keep the doc and this class in sync.
/// </summary>
public static class DemoSeedPack
{
    /// <summary>Fixed generation profile for the flagship demo story.</summary>
    public const string ProfileName = GenerationProfileCatalog.ThinSlice;

    /// <summary>Fixed random seed so generated data is reproducible run-to-run.</summary>
    public const int RandomSeed = 104729;

    /// <summary>Label every synthetic/demo artifact carries; there is no real trade or dispatch data.</summary>
    public const string DataClassification = "Synthetic";

    /// <summary>Scenario names created by <c>IScenarioService.ApplyFlagshipPack</c> (010), in presenter order.</summary>
    public static readonly IReadOnlyList<string> FlagshipScenarioNames =
    [
        "diesel-rise",
        "distant-high-price",
        "capacity-loss",
        "demand-spike"
    ];

    /// <summary>Web routes the presenter script walks through, in order.</summary>
    public static class Routes
    {
        public const string Demo = "/demo";
        public const string Dashboard = "/dashboard";
        public const string Network = "/network";
        public const string Recommendations = "/recommendations";
        public const string Scenarios = "/scenarios";
        public const string Replay = "/replay";
        public const string Models = "/models";
    }

    /// <summary>Doc paths (repo-root relative) that back this seed pack; asserted by DemoHardeningTests.</summary>
    public static class DocPaths
    {
        public const string PresenterScript = "docs/demo/presenter-script.md";
        public const string SeedPack = "docs/demo/seed-pack.md";
        public const string HardeningNotes = "docs/demo/hardening-notes.md";
        public const string HonestyBoundary = "docs/demo/honesty-boundary.md";
    }
}
