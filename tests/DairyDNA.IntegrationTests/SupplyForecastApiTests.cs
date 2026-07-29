using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class SupplyForecastApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public SupplyForecastApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Train_and_query_supply_forecasts()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        genResponse.EnsureSuccessStatusCode();
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();

        var train = await _client.PostAsJsonAsync("/api/forecasts/supply/runs", new { generationId = gen!.id, randomSeed = 104729 });
        train.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var model = await train.Content.ReadFromJsonAsync<JsonElement>();
        model.GetProperty("dataClassification").GetString().Should().Be("Forecast");

        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/forecasts/supply?generationId={gen.id}");
        list.GetProperty("dataClassification").GetString().Should().Be("Forecast");
        list.GetProperty("disclaimer").GetString().Should().Contain("not guaranteed");
        list.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    private sealed record GenDto(Guid id);
}
