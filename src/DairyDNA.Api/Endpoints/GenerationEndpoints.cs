using System.Text.Json;
using DairyDNA.Application.Generation;
using DairyDNA.Domain.Entities;

namespace DairyDNA.Api.Endpoints;

public static class GenerationEndpoints
{
    public static RouteGroupBuilder MapGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/generation-runs");
        group.MapPost("/", async (ThinSliceGenerationRequestBody body, CreateGenerationRunHandler handler, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DairyDNA.Generation");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await handler.HandleAsync(new Application.Abstractions.ThinSliceGenerationRequest
            {
                ScenarioName = body.ScenarioName ?? "thin-vertical-slice",
                SchemaVersion = body.SchemaVersion ?? "dairydna.thin-slice.v1",
                RandomSeed = body.RandomSeed ?? 104729,
                StartDate = body.StartDate ?? new DateOnly(2025, 10, 1),
                EndDate = body.EndDate ?? new DateOnly(2025, 12, 29),
                FarmCount = body.FarmCount ?? 5,
                FacilityCount = body.FacilityCount ?? 2,
                CustomerCount = body.CustomerCount ?? 5,
                TruckCount = body.TruckCount ?? 3
            }, ct);
            sw.Stop();
            logger.LogInformation("Generation completed in {ElapsedMs}ms id={Id} seed={Seed}", sw.ElapsedMilliseconds, result.Id, result.RandomSeed);
            return Results.Accepted($"/api/generation-runs/{result.Id}", ToSummary(result));
        });
        group.MapGet("/", async (ListGenerationRunsHandler handler, CancellationToken ct) =>
            Results.Ok((await handler.HandleAsync(ct)).Select(ToSummary)));
        group.MapGet("/{id:guid}", async (Guid id, GetGenerationRunHandler handler, CancellationToken ct) =>
        {
            var item = await handler.HandleAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });
        return group;
    }

    private static object ToSummary(GenerationManifest m) => new
    {
        id = m.Id,
        scenarioName = m.ScenarioName,
        randomSeed = m.RandomSeed,
        status = m.Status.ToString(),
        planningDate = m.PlanningDate,
        generatedAt = m.GeneratedAt,
        schemaVersion = m.SchemaVersion
    };

    private static object ToDetail(GenerationManifest m) => new
    {
        id = m.Id,
        scenarioName = m.ScenarioName,
        randomSeed = m.RandomSeed,
        status = m.Status.ToString(),
        planningDate = m.PlanningDate,
        generatedAt = m.GeneratedAt,
        schemaVersion = m.SchemaVersion,
        configurationHash = m.ConfigurationHash,
        entityCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(m.EntityCountsJson) ?? new(),
        failureMessage = m.FailureMessage
    };
}

public sealed class ThinSliceGenerationRequestBody
{
    public string? ScenarioName { get; set; }
    public string? SchemaVersion { get; set; }
    public int? RandomSeed { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? FarmCount { get; set; }
    public int? FacilityCount { get; set; }
    public int? CustomerCount { get; set; }
    public int? TruckCount { get; set; }
}
