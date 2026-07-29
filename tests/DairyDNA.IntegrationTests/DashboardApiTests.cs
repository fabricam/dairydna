using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class DashboardApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public DashboardApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Dashboard_returns_map_and_charts_payload()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        genResponse.EnsureSuccessStatusCode();
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();

        var dash = await _client.GetFromJsonAsync<JsonElement>($"/api/dashboard?generationId={gen!.id}");
        dash.GetProperty("dataClassification").GetString().Should().Be("Synthetic");
        dash.GetProperty("network").GetArrayLength().Should().BeGreaterThan(0);
        dash.GetProperty("inventoryAgeRisk").GetArrayLength().Should().Be(4);
        dash.GetProperty("priceSeries").GetArrayLength().Should().BeGreaterThan(0);
        dash.GetProperty("fleet").GetArrayLength().Should().BeGreaterThan(0);

        var facilityId = dash.GetProperty("network").EnumerateArray()
            .First(n => n.GetProperty("kind").GetString() == "Facility")
            .GetProperty("id").GetGuid();
        var detail = await _client.GetAsync($"/api/dashboard/facilities/{facilityId}?generationId={gen.id}");
        detail.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Unknown_generation_returns_not_found()
    {
        var response = await _client.GetAsync($"/api/dashboard?generationId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrDto>();
        body!.error.Should().Contain("Unknown");
    }

    private sealed record GenDto(Guid id);
    private sealed record ErrDto(string error);
}
