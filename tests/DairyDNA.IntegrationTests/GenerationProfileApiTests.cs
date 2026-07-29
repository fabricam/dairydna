using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class GenerationProfileApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public GenerationProfileApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Lists_generation_profiles()
    {
        var profiles = await _client.GetFromJsonAsync<List<ProfileDto>>("/api/generation-profiles");
        profiles.Should().NotBeNull();
        profiles!.Select(p => p.name).Should().Contain(["thin-slice", "standard-demo"]);
    }

    [Fact]
    public async Task Generate_by_profile_name_returns_validation_report()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new
        {
            profileName = "thin-slice",
            randomSeed = 104729
        });
        genResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();
        gen.Should().NotBeNull();
        gen!.profileName.Should().Be("thin-slice");
        gen.status.Should().Be("Completed");
        gen.generatorVersion.Should().Be("synthetic-gen-v2");

        var report = await _client.GetFromJsonAsync<ReportDto>($"/api/generation-runs/{gen.id}/validation-report");
        report.Should().NotBeNull();
        report!.passed.Should().BeTrue();
        report.checks.Should().NotBeEmpty();

        var detail = await _client.GetFromJsonAsync<JsonElement>($"/api/generation-runs/{gen.id}");
        detail.GetProperty("configurationHash").GetString().Should().NotBeNullOrWhiteSpace();
        detail.GetProperty("entityCounts").GetProperty("farms").GetInt32().Should().Be(5);
        detail.GetProperty("entityCounts").GetProperty("products").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Zero_farms_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/generation-runs", new
        {
            profileName = "custom",
            farmCount = 0,
            facilityCount = 1,
            customerCount = 1,
            truckCount = 1,
            randomSeed = 1
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ProfileDto(string name, string description);
    private sealed record GenDto(Guid id, string status, string profileName, string generatorVersion);
    private sealed record ReportDto(bool passed, List<object> checks);
}
