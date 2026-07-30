using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class DemandForecastApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;
    public DemandForecastApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Train_and_query_demand_forecasts()
    {
        var generation = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        var gen = await generation.Content.ReadFromJsonAsync<GenDto>();
        var train = await _client.PostAsJsonAsync("/api/forecasts/demand/runs", new { generationId = gen!.id, randomSeed = 104729 });
        train.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await train.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("dataClassification").GetString().Should().Be("Forecast");
        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/forecasts/demand?generationId={gen.id}");
        list.GetProperty("disclaimer").GetString().Should().Contain("not open orders");
        list.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }
    private sealed record GenDto(Guid id);
}
