using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Generation;
using DairyDNA.Domain.Entities;

namespace DairyDNA.Api.Endpoints;

public static class GenerationEndpoints
{
    public static RouteGroupBuilder MapGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/generation-profiles", () =>
            Results.Ok(GenerationProfileCatalog.All.Select(p => new
            {
                name = p.Name,
                description = p.Description,
                farmCount = p.FarmCount,
                facilityCount = p.FacilityCount,
                customerCount = p.CustomerCount,
                truckCount = p.TruckCount,
                productSet = p.ProductSet,
                startDate = p.StartDate,
                endDate = p.EndDate,
                missingnessRate = p.MissingnessRate
            })));

        var group = app.MapGroup("/api/generation-runs");
        group.MapPost("/", async (GenerationRequestBody body, CreateGenerationRunHandler handler, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DairyDNA.Generation");
            try
            {
                var request = body.ToRequest();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await handler.HandleAsync(request, ct);
                sw.Stop();
                logger.LogInformation("Generation completed in {ElapsedMs}ms id={Id} profile={Profile} status={Status}",
                    sw.ElapsedMilliseconds, result.Id, result.ProfileName, result.Status);
                return Results.Accepted($"/api/generation-runs/{result.Id}", ToSummary(result));
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["profile"] = [ex.Message] });
            }
        });
        group.MapGet("/", async (ListGenerationRunsHandler handler, CancellationToken ct) =>
            Results.Ok((await handler.HandleAsync(ct)).Select(ToSummary)));
        group.MapGet("/{id:guid}", async (Guid id, GetGenerationRunHandler handler, CancellationToken ct) =>
        {
            var item = await handler.HandleAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });
        group.MapGet("/{id:guid}/validation-report", async (Guid id, GetValidationReportHandler handler, CancellationToken ct) =>
        {
            var report = await handler.HandleAsync(id, ct);
            return report is null ? Results.NotFound() : Results.Ok(report);
        });
        return group;
    }

    private static object ToSummary(GenerationManifest m) => new
    {
        id = m.Id,
        scenarioName = m.ScenarioName,
        profileName = m.ProfileName,
        randomSeed = m.RandomSeed,
        status = m.Status.ToString(),
        planningDate = m.PlanningDate,
        generatedAt = m.GeneratedAt,
        schemaVersion = m.SchemaVersion,
        generatorVersion = m.GeneratorVersion
    };

    private static object ToDetail(GenerationManifest m) => new
    {
        id = m.Id,
        scenarioName = m.ScenarioName,
        profileName = m.ProfileName,
        randomSeed = m.RandomSeed,
        status = m.Status.ToString(),
        planningDate = m.PlanningDate,
        generatedAt = m.GeneratedAt,
        schemaVersion = m.SchemaVersion,
        generatorVersion = m.GeneratorVersion,
        configurationHash = m.ConfigurationHash,
        entityCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(m.EntityCountsJson) ?? new(),
        failureMessage = m.FailureMessage
    };
}

public sealed class GenerationRequestBody
{
    public string? ProfileName { get; set; }
    public string? ScenarioName { get; set; }
    public string? SchemaVersion { get; set; }
    public int? RandomSeed { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? FarmCount { get; set; }
    public int? FacilityCount { get; set; }
    public int? CustomerCount { get; set; }
    public int? TruckCount { get; set; }
    public string? ProductSet { get; set; }
    public decimal? MissingnessRate { get; set; }
    public int? DenseHistoryDays { get; set; }
    public int? SparseCadenceDays { get; set; }

    public SyntheticGenerationRequest ToRequest()
    {
        // Legacy 000 clients omit profileName but send farmCount etc.
        var profile = ProfileName;
        if (string.IsNullOrWhiteSpace(profile))
        {
            profile = FarmCount is null && FacilityCount is null
                ? GenerationProfileCatalog.ThinSlice
                : GenerationProfileCatalog.Custom;
            if (profile == GenerationProfileCatalog.Custom && FarmCount is null)
                profile = GenerationProfileCatalog.ThinSlice;
        }

        return new SyntheticGenerationRequest
        {
            ProfileName = profile!,
            ScenarioName = ScenarioName,
            SchemaVersion = SchemaVersion ?? GenerationProfileCatalog.SchemaVersion,
            RandomSeed = RandomSeed ?? 104729,
            StartDate = StartDate,
            EndDate = EndDate,
            FarmCount = FarmCount,
            FacilityCount = FacilityCount,
            CustomerCount = CustomerCount,
            TruckCount = TruckCount,
            ProductSet = ProductSet,
            MissingnessRate = MissingnessRate,
            DenseHistoryDays = DenseHistoryDays,
            SparseCadenceDays = SparseCadenceDays
        };
    }
}
