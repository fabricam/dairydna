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
        return app;
    }
}
