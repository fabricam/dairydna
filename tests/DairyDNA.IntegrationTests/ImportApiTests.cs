using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class ImportApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ImportApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Import_sources_and_dairy_fixture_flow()
    {
        var sources = await _client.GetFromJsonAsync<List<SourceDto>>("/api/import-sources");
        sources.Should().NotBeNull();
        sources!.Select(s => s.code).Should().Contain(["fixture-dairy-prices", "fixture-weather", "fixture-fuel-prices"]);

        var response = await _client.PostAsJsonAsync("/api/import-runs", new { sourceCode = "fixture-dairy-prices" });
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var run = await response.Content.ReadFromJsonAsync<RunDto>();
        run!.status.Should().Be("Completed");
        run.dataClassification.Should().Be("Public");

        var prices = await _client.GetFromJsonAsync<PricesDto>("/api/public/market-prices?regionCode=R1");
        prices!.dataClassification.Should().Be("Public");
        prices.disclaimer.Should().Contain("not DairyDNA");
        prices.items.Should().NotBeEmpty();
    }

    private sealed record SourceDto(string code);
    private sealed record RunDto(string status, string dataClassification);
    private sealed record PricesDto(string dataClassification, string disclaimer, List<object> items);
}
