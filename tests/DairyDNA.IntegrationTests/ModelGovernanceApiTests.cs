using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class ModelGovernanceApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ModelGovernanceApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generate_train_list_publish_card_retire_flow()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        genResponse.EnsureSuccessStatusCode();
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();

        var train = await _client.PostAsJsonAsync("/api/forecasts/supply/runs", new { generationId = gen!.id, randomSeed = 104729 });
        train.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var trained = await train.Content.ReadFromJsonAsync<JsonElement>();
        var modelId = trained.GetProperty("id").GetGuid();

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/models?family=supply");
        var items = list.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(i => i.GetProperty("id").GetGuid() == modelId);
        var listed = items.First(i => i.GetProperty("id").GetGuid() == modelId);
        listed.GetProperty("lifecycleStatus").GetString().Should().Be("Candidate");
        listed.GetProperty("artifactChecksumSha256").GetString().Should().NotBeNullOrWhiteSpace();

        var publish = await _client.PostAsJsonAsync($"/api/models/{modelId}/publish",
            new { actor = "qa-tester", reason = "Meets acceptance bar for demo", overrideQualityGate = true });
        publish.EnsureSuccessStatusCode();
        var published = await publish.Content.ReadFromJsonAsync<JsonElement>();
        published.GetProperty("lifecycleStatus").GetString().Should().Be("Published");

        var card = await _client.GetFromJsonAsync<JsonElement>($"/api/models/{modelId}/card");
        card.GetProperty("limitations").GetString().Should().Contain("not production advice");
        card.GetProperty("leakageControlStatement").GetString().Should().NotBeNullOrWhiteSpace();
        card.GetProperty("auditTrail").GetArrayLength().Should().BeGreaterThan(0);

        var latestSupplyModel = await _client.GetFromJsonAsync<JsonElement>($"/api/forecasts/supply/models/latest?generationId={gen.id}");
        latestSupplyModel.GetProperty("id").GetGuid().Should().Be(modelId);

        var retire = await _client.PostAsJsonAsync($"/api/models/{modelId}/retire", new { actor = "qa-tester", reason = "Superseded" });
        retire.EnsureSuccessStatusCode();
        var retired = await retire.Content.ReadFromJsonAsync<JsonElement>();
        retired.GetProperty("lifecycleStatus").GetString().Should().Be("Retired");

        var optimizers = await _client.GetFromJsonAsync<JsonElement>("/api/models/optimizers");
        optimizers.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("version").GetString())
            .Should().Contain(["ortools-cm-v1", "naive-cm-v1", "transport-cost-v2"]);
    }

    [Fact]
    public async Task Publish_without_reason_is_rejected()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();
        var train = await _client.PostAsJsonAsync("/api/forecasts/supply/runs", new { generationId = gen!.id, randomSeed = 104729 });
        var trained = await train.Content.ReadFromJsonAsync<JsonElement>();
        var modelId = trained.GetProperty("id").GetGuid();

        var publish = await _client.PostAsJsonAsync($"/api/models/{modelId}/publish", new { reason = "" });
        publish.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record GenDto(Guid id);
}
