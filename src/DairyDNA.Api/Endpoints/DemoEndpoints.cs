using DairyDNA.Application.Demo;

namespace DairyDNA.Api.Endpoints;

public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/demo/summary", async (Guid generationId, DateOnly? asOfDate, GetDemoSummaryHandler handler, CancellationToken ct) =>
        {
            var summary = await handler.HandleAsync(generationId, asOfDate, ct);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });

        // One-shot demo bring-up (spec 013 FR-001): generate the DemoSeedPack dataset, apply the
        // flagship scenario pack, and run one optimization. Convenience only — the same outcome is
        // reachable via /api/generation-runs -> /api/scenarios/flagship-pack -> /api/optimization-runs.
        app.MapPost("/api/demo/bootstrap", async (DemoBootstrapRequest? body, DemoBootstrapHandler handler, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DairyDNA.Demo");
            var request = body ?? new DemoBootstrapRequest();
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await handler.HandleAsync(request, ct);
                sw.Stop();
                logger.LogInformation(
                    "Demo bootstrap completed in {ElapsedMs}ms generationId={GenerationId} status={Status} objective={Objective}",
                    sw.ElapsedMilliseconds, result.GenerationId, result.GenerationStatus, result.ObjectiveValue);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["profile"] = [ex.Message] });
            }
        });
        return app;
    }
}
