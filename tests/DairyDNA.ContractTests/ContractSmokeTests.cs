using System.Net.Http.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.IntegrationTests;
using FluentAssertions;

namespace DairyDNA.ContractTests;

public class ContractSmokeTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ContractSmokeTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generation_demo_and_optimization_endpoints_match_contract_shapes()
    {
        var gen = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        gen.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var payload = await gen.Content.ReadFromJsonAsync<GenDto>();
        payload.Should().NotBeNull();
        payload!.id.Should().NotBeEmpty();

        var list = await _client.GetAsync("/api/generation-runs");
        list.EnsureSuccessStatusCode();

        var detail = await _client.GetAsync($"/api/generation-runs/{payload.id}");
        detail.EnsureSuccessStatusCode();

        var summary = await _client.GetAsync($"/api/demo/summary?generationId={payload.id}");
        summary.EnsureSuccessStatusCode();

        var opt = await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = payload.id });
        opt.EnsureSuccessStatusCode();
        var optBody = await opt.Content.ReadFromJsonAsync<OptDto>();
        optBody.Should().NotBeNull();
        optBody!.id.Should().NotBeEmpty();
        optBody.status.Should().NotBeNullOrWhiteSpace();
        optBody.optimizerVersion.Should().Be("ortools-cm-v1");

        var getOpt = await _client.GetAsync($"/api/optimization-runs/{optBody.id}");
        getOpt.EnsureSuccessStatusCode();
    }

    private sealed record GenDto(Guid id);
    private sealed record OptDto(Guid id, string status, string optimizerVersion);
}
