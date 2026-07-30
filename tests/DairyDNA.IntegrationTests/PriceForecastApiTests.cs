using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class PriceForecastApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;
    public PriceForecastApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Train_and_query_price_forecasts_and_optimization_bundle()
    {
        var generation = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        var gen = await generation.Content.ReadFromJsonAsync<GenDto>();
        var train = await _client.PostAsJsonAsync("/api/forecasts/price/runs", new { generationId = gen!.id, randomSeed = 104729 });
        train.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await train.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("dataClassification").GetString().Should().Be("Forecast");
        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/forecasts/price?generationId={gen.id}&productCode=RAW_MILK");
        list.GetProperty("disclaimer").GetString().Should().Contain("not trade quotes");
        list.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        var asOf = DateOnly.Parse(list.GetProperty("items")[0].GetProperty("asOfDate").GetString()!);
        var bundle = await _client.GetFromJsonAsync<JsonElement>($"/api/forecasts/price/optimization-bundle?generationId={gen.id}&asOfDate={asOf:yyyy-MM-dd}");
        bundle.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }
    private sealed record GenDto(Guid id);
}
