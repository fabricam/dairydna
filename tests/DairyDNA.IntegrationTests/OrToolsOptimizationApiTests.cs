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

    private sealed record GenDto(Guid id);
}
