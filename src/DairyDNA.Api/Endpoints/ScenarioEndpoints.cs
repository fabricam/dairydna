using DairyDNA.Application.Scenarios;

namespace DairyDNA.Api.Endpoints;

public static class ScenarioEndpoints
{
    public static RouteGroupBuilder MapScenarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scenarios");
        group.MapGet("/", async (Guid? generationId, IScenarioService service, CancellationToken ct) =>
            Results.Ok(await service.ListDefinitions(generationId, ct)));
        group.MapPost("/", async (CreateScenarioDefinitionRequest body, IScenarioService service, CancellationToken ct) =>
        {
            try { return Results.Created($"/api/scenarios", await service.CreateDefinition(body, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
        group.MapPost("/flagship-pack", async (Guid generationId, IScenarioService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ApplyFlagshipPack(generationId, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        group.MapPost("/{id:guid}/runs", async (Guid id, ScenarioRunRequest? body, IScenarioService service, CancellationToken ct) =>
        {
            try
            {
                var run = await service.RunScenario(id, body?.BaseOptimizationRunId, ct);
                return run is null ? Results.NotFound() : Results.Ok(new
                {
                    run.Id,
                    run.ScenarioDefinitionId,
                    run.BaseOptimizationRunId,
                    run.OptimizationRunId,
                    status = run.Status.ToString(),
                    run.CreatedAt,
                    run.FuelPriceOverride,
                    run.Notes
                });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
        group.MapGet("/compare", async (Guid baseRunId, Guid scenarioRunId, IScenarioService service, CancellationToken ct) =>
        {
            try
            {
                var comparison = await service.Compare(baseRunId, scenarioRunId, ct);
                return comparison is null ? Results.NotFound() : Results.Ok(new
                {
                    @base = ToRun(comparison.Base),
                    scenario = ToRun(comparison.Scenario),
                    comparison.MovementDiffs,
                    comparison.DataClassification,
                    comparison.HonestyLabel
                });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
        return group;
    }
    private static object ToRun(ScenarioComparisonRun run) => new
    {
        run.RunId,
        status = run.Status.ToString(),
        run.ObjectiveValue,
        run.UnservedCount,
        run.UnusedCount,
        run.IsRecommended
    };
}

public sealed class ScenarioRunRequest
{
    public Guid? BaseOptimizationRunId { get; set; }
}
