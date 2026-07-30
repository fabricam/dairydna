using System.Net.Http.Json;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

/// <summary>Spec 013 User Story 2: two independent bootstraps of the same seed pack must reach the
/// same objective/quantities (within the 0.01 reproducibility tolerance used elsewhere).</summary>
public class DemoBootstrapApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public DemoBootstrapApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Bootstrap_twice_produces_the_same_objective_and_quantities()
    {
        var r1 = await (await _client.PostAsJsonAsync("/api/demo/bootstrap", new { })).Content.ReadFromJsonAsync<BootstrapDto>();
        var r2 = await (await _client.PostAsJsonAsync("/api/demo/bootstrap", new { })).Content.ReadFromJsonAsync<BootstrapDto>();

        r1.Should().NotBeNull();
        r2.Should().NotBeNull();
        r1!.generationStatus.Should().Be("Completed");
        r2!.generationStatus.Should().Be("Completed");
        r1.profileName.Should().Be("thin-slice");
        r1.randomSeed.Should().Be(104729);
        r1.dataClassification.Should().Be("Synthetic");
        r1.flagshipScenarioNames.Should().BeEquivalentTo(r2.flagshipScenarioNames);

        r1.optimizationRunId.Should().NotBeNull();
        r2.optimizationRunId.Should().NotBeNull();
        r1.objectiveValue.Should().NotBeNull();
        r2.objectiveValue.Should().NotBeNull();
        Math.Abs(r1.objectiveValue!.Value - r2.objectiveValue!.Value).Should().BeLessThanOrEqualTo(0.01m);

        var m1 = await _client.GetFromJsonAsync<OptDto>($"/api/optimization-runs/{r1.optimizationRunId}");
        var m2 = await _client.GetFromJsonAsync<OptDto>($"/api/optimization-runs/{r2.optimizationRunId}");
        m1!.movements.Select(m => m.quantityPounds).Should().Equal(m2!.movements.Select(m => m.quantityPounds));
    }

    private sealed record BootstrapDto(
        Guid generationId,
        string profileName,
        int randomSeed,
        string generationStatus,
        Guid? optimizationRunId,
        string? optimizationStatus,
        decimal? objectiveValue,
        List<string> flagshipScenarioNames,
        string dataClassification);

    private sealed record OptDto(List<MoveDto> movements);
    private sealed record MoveDto(decimal quantityPounds);
}
