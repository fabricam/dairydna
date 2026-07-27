using System.Net.Http.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class ReproducibilityIntegrationTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ReproducibilityIntegrationTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generate_then_optimize_twice_is_reproducible()
    {
        var body = new ThinSliceGenerationRequest { RandomSeed = 104729 };
        var g1 = await (await _client.PostAsJsonAsync("/api/generation-runs", body)).Content.ReadFromJsonAsync<GenDto>();
        var o1 = await (await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = g1!.id })).Content.ReadFromJsonAsync<OptDto>();
        var o2 = await (await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = g1.id })).Content.ReadFromJsonAsync<OptDto>();

        o1!.objectiveValue.Should().Be(o2!.objectiveValue);
        o1.movements.Select(m => m.quantityPounds).Should().Equal(o2.movements.Select(m => m.quantityPounds));
        for (var i = 0; i < o1.movements.Count; i++)
            Math.Abs(o1.movements[i].transportationCost - o2.movements[i].transportationCost).Should().BeLessThanOrEqualTo(0.01m);
    }

    private sealed record GenDto(Guid id);
    private sealed record OptDto(decimal objectiveValue, List<MoveDto> movements);
    private sealed record MoveDto(decimal quantityPounds, decimal transportationCost);
}
