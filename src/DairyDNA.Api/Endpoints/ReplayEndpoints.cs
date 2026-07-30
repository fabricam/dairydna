using DairyDNA.Application.Replay;

namespace DairyDNA.Api.Endpoints;

public static class ReplayEndpoints
{
    public static RouteGroupBuilder MapReplayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/replay");

        group.MapPost("/runs", async (RunReplayRequest body, IReplayService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.RunAsync(body.GenerationId, body.AsOfDate, body.PriceMode, ct));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapGet("/runs", async (Guid generationId, DateOnly? asOfDate, IReplayService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(generationId, asOfDate, ct)));

        group.MapGet("/runs/{id:guid}", async (Guid id, IReplayService service, CancellationToken ct) =>
        {
            var run = await service.GetAsync(id, ct);
            return run is null ? Results.NotFound() : Results.Ok(run);
        });

        group.MapPost("/reports/regret", async (RegretReportRequest body, IReplayService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.BuildRegretReportAsync(body.GenerationId, body.StartDate, body.EndDate, ct));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapGet("/reports/{id:guid}", async (Guid id, IReplayService service, CancellationToken ct) =>
        {
            var report = await service.GetReportAsync(id, ct);
            return report is null ? Results.NotFound() : Results.Ok(report);
        });

        return group;
    }
}
