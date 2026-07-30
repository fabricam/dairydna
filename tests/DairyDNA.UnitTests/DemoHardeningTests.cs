using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Demo;
using FluentAssertions;

namespace DairyDNA.UnitTests;

/// <summary>
/// Spec 013 (Demo Hardening) guardrails: the versioned seed pack constants and the demo docs it is
/// described in must stay in sync, and the presenter script must exercise the required visuals
/// (FR-005a) rather than a tables-only walkthrough.
/// </summary>
public class DemoHardeningTests
{
    [Fact]
    public void Seed_pack_uses_the_documented_thin_slice_profile_and_seed()
    {
        DemoSeedPack.ProfileName.Should().Be(GenerationProfileCatalog.ThinSlice);
        DemoSeedPack.ProfileName.Should().Be("thin-slice");
        DemoSeedPack.RandomSeed.Should().Be(104729);
        DemoSeedPack.DataClassification.Should().Be("Synthetic");
    }

    [Fact]
    public void Seed_pack_flagship_scenarios_match_the_010_flagship_pack()
    {
        DemoSeedPack.FlagshipScenarioNames.Should().BeEquivalentTo(
            "diesel-rise", "distant-high-price", "capacity-loss", "demand-spike");
    }

    [Fact]
    public void Seed_pack_routes_resolve_to_existing_web_pages()
    {
        DemoSeedPack.Routes.Demo.Should().Be("/demo");
        DemoSeedPack.Routes.Dashboard.Should().Be("/dashboard");
        DemoSeedPack.Routes.Network.Should().Be("/network");
        DemoSeedPack.Routes.Recommendations.Should().Be("/recommendations");
        DemoSeedPack.Routes.Scenarios.Should().Be("/scenarios");
        DemoSeedPack.Routes.Replay.Should().Be("/replay");
        DemoSeedPack.Routes.Models.Should().Be("/models");
    }

    [Fact]
    public void Demo_doc_files_referenced_by_the_seed_pack_exist()
    {
        var root = FindRepoRoot();

        File.Exists(Path.Combine(root, DemoSeedPack.DocPaths.PresenterScript)).Should().BeTrue(
            $"{DemoSeedPack.DocPaths.PresenterScript} should exist under the repo root");
        File.Exists(Path.Combine(root, DemoSeedPack.DocPaths.SeedPack)).Should().BeTrue(
            $"{DemoSeedPack.DocPaths.SeedPack} should exist under the repo root");
        File.Exists(Path.Combine(root, DemoSeedPack.DocPaths.HardeningNotes)).Should().BeTrue(
            $"{DemoSeedPack.DocPaths.HardeningNotes} should exist under the repo root");
        File.Exists(Path.Combine(root, DemoSeedPack.DocPaths.HonestyBoundary)).Should().BeTrue(
            $"{DemoSeedPack.DocPaths.HonestyBoundary} should exist under the repo root");
    }

    [Fact]
    public void Presenter_script_exercises_network_map_a_chart_and_recommendations()
    {
        var root = FindRepoRoot();
        var text = File.ReadAllText(Path.Combine(root, DemoSeedPack.DocPaths.PresenterScript));

        text.Should().ContainEquivalentOf("network map", "presenter script must exercise the network map (FR-005a)");
        text.Should().ContainEquivalentOf("recommendations", "presenter script must exercise recommendations (FR-005a)");
        (text.Contains("inventory age", StringComparison.OrdinalIgnoreCase)
            || text.Contains("forecast band", StringComparison.OrdinalIgnoreCase)
            || text.Contains("margin", StringComparison.OrdinalIgnoreCase)
            || text.Contains("regret", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("presenter script must exercise at least one chart (FR-005a)");
        text.Should().ContainEquivalentOf(
            "104729", "presenter script must reference the versioned demo seed");
    }

    [Fact]
    public void Demo_start_script_exists()
    {
        var root = FindRepoRoot();
        File.Exists(Path.Combine(root, "scripts", "demo-start.ps1")).Should().BeTrue();
    }

    /// <summary>Resolves the repo root from this file's compile-time path (works regardless of the
    /// test run's working directory or --output folder).</summary>
    private static string FindRepoRoot([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var dir = Path.GetDirectoryName(sourceFile) ?? AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "DairyDNA.sln")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        throw new DirectoryNotFoundException($"Could not locate the repo root (DairyDNA.sln) above '{sourceFile}'.");
    }
}
