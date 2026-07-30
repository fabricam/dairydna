using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DairyDNA.Application.Abstractions;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class ReplayApiTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ReplayApiTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generate_replay_run_get_and_regret_report_flow()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        genResponse.EnsureSuccessStatusCode();
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();

        var detail = await _client.GetFromJsonAsync<JsonElement>($"/api/generation-runs/{gen!.id}");
        var startDate = detail.GetProperty("startDate").GetString();

        var runResponse = await _client.PostAsJsonAsync("/api/replay/runs", new { generationId = gen.id, asOfDate = startDate });
        runResponse.EnsureSuccessStatusCode();
        var run = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        var replayId = run.GetProperty("id").GetGuid();
        var optimizationRunId = run.GetProperty("optimizationRunId").GetGuid();
        run.GetProperty("priceMode").GetString().Should().Be("Spot");
        run.GetProperty("leakagePassed").GetBoolean().Should().BeTrue();
        run.GetProperty("dataClassification").GetString().Should().Be("Synthetic");
        run.GetProperty("costingModelVersion").GetString().Should().Be("transport-cost-v2");

        // Assert OptimizationRun is linked
        var optRun = await _client.GetFromJsonAsync<JsonElement>($"/api/optimization-runs/{optimizationRunId}");
        optRun.GetProperty("generationId").GetGuid().Should().Be(gen.id);

        var listed = await _client.GetFromJsonAsync<JsonElement>($"/api/replay/runs?generationId={gen.id}&asOfDate={startDate}");
        listed.EnumerateArray().Should().Contain(r => r.GetProperty("id").GetGuid() == replayId);

        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/replay/runs/{replayId}");
        fetched.GetProperty("optimizationRunId").GetGuid().Should().Be(optimizationRunId);

        var startDay = DateOnly.Parse(startDate!);
        var endDay = startDay.AddDays(2);
        var reportResponse = await _client.PostAsJsonAsync("/api/replay/reports/regret",
            new { generationId = gen.id, startDate = startDay, endDate = endDay });
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = report.GetProperty("id").GetGuid();
        var days = report.GetProperty("days").EnumerateArray().ToList();
        days.Should().HaveCount(3);
        foreach (var day in days)
        {
            var baselines = day.GetProperty("baselines").EnumerateArray().ToList();
            baselines.Count.Should().BeGreaterThanOrEqualTo(2);
            baselines.Select(b => b.GetProperty("policyName").GetString())
                .Should().Contain(["NearestCustomerGreedy", "HighestPriceFirst"]);
            day.GetProperty("optimizer").GetProperty("objectiveValue").ValueKind.Should().Be(JsonValueKind.Number);
        }
        report.GetProperty("summary").GetProperty("totalDays").GetInt32().Should().Be(3);

        var fetchedReport = await _client.GetFromJsonAsync<JsonElement>($"/api/replay/reports/{reportId}");
        fetchedReport.GetProperty("days").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task AsOfDate_outside_dataset_window_returns_bad_request()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();

        var runResponse = await _client.PostAsJsonAsync("/api/replay/runs", new { generationId = gen!.id, asOfDate = "2020-01-01" });

        runResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_generation_returns_not_found()
    {
        var runResponse = await _client.PostAsJsonAsync("/api/replay/runs", new { generationId = Guid.NewGuid(), asOfDate = "2025-10-01" });

        runResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record GenDto(Guid id);
}
