using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class OrToolsOptimizationApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;
    public OrToolsOptimizationApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Default_optimize_uses_ortools()
    {
        var gen = await (await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest())).Content.ReadFromJsonAsync<GenDto>();
        var opt = await (await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = gen!.id })).Content.ReadFromJsonAsync<JsonElement>();
        opt.GetProperty("optimizerVersion").GetString().Should().Be("ortools-cm-v1");
        opt.GetProperty("status").GetString().Should().BeOneOf("Feasible", "Infeasible", "Failed");
    }

    [Fact]
    public async Task Explicit_naive_version_still_available()
    {
        var gen = await (await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest())).Content.ReadFromJsonAsync<GenDto>();
        var opt = await (await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = gen!.id, optimizerVersion = "naive-cm-v1" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        opt.GetProperty("optimizerVersion").GetString().Should().Be("naive-cm-v1");
    }

    [Fact]
    public async Task Flagship_pack_runs_and_compares_a_scenario()
    {
        var gen = await (await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest())).Content.ReadFromJsonAsync<GenDto>();
        var baseRun = await (await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = gen!.id }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var baseRunId = baseRun.GetProperty("id").GetGuid();

        var pack = await _client.PostAsync($"/api/scenarios/flagship-pack?generationId={gen.id}", null);
        pack.EnsureSuccessStatusCode();
        var definitions = await _client.GetFromJsonAsync<JsonElement>($"/api/scenarios?generationId={gen.id}");
        var scenarioId = definitions.EnumerateArray().First().GetProperty("id").GetGuid();
        var scenarioRunResponse = await _client.PostAsJsonAsync($"/api/scenarios/{scenarioId}/runs", new { baseOptimizationRunId = baseRunId });
        scenarioRunResponse.EnsureSuccessStatusCode();
        var scenarioRun = await scenarioRunResponse.Content.ReadFromJsonAsync<JsonElement>();

        var comparison = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/scenarios/compare?baseRunId={baseRunId}&scenarioRunId={scenarioRun.GetProperty("id").GetGuid()}");

        comparison.GetProperty("base").GetProperty("runId").GetGuid().Should().Be(baseRunId);
        var scenario = comparison.GetProperty("scenario");
        if (scenario.GetProperty("status").GetString() == "Failed")
            scenario.GetProperty("isRecommended").GetBoolean().Should().BeFalse();
        comparison.GetProperty("honestyLabel").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private sealed record GenDto(Guid id);
}
