using System.Text.Json;
using DairyDNA.Application.Demo;
using DairyDNA.Application.Optimization;
using DairyDNA.Domain.Entities;

namespace DairyDNA.Api.Endpoints;

public static class OptimizationEndpoints
{
    public static RouteGroupBuilder MapOptimizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/optimization-runs");
        group.MapPost("/", async (CreateOptimizationRunRequest body, CreateOptimizationRunHandler handler, GetOptimizationRunHandler getHandler, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DairyDNA.Optimization");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var run = await handler.HandleAsync(body, ct);
            sw.Stop();
            if (run is null) return Results.NotFound();
            logger.LogInformation("Optimization completed in {ElapsedMs}ms id={Id} status={Status} objective={Objective}",
                sw.ElapsedMilliseconds, run.Id, run.Status, run.ObjectiveValue);
            var detail = await getHandler.HandleAsync(run.Id, ct);
            return Results.Ok(ToDetail(detail!.Value.Run, detail.Value.Movements, detail.Value.Network));
        });
        group.MapGet("/{id:guid}", async (Guid id, GetOptimizationRunHandler handler, CancellationToken ct) =>
        {
            var detail = await handler.HandleAsync(id, ct);
            return detail is null
                ? Results.NotFound()
                : Results.Ok(ToDetail(detail.Value.Run, detail.Value.Movements, detail.Value.Network));
        });
        return group;
    }

    private static object ToDetail(OptimizationRun run, List<RecommendedMovement> movements, List<NetworkMapPoint> network) => new
    {
        id = run.Id,
        generationId = run.GenerationId,
        asOfDate = run.AsOfDate,
        status = run.Status.ToString(),
        objectiveValue = run.ObjectiveValue,
        optimizerVersion = run.OptimizerVersion,
        solveDurationMilliseconds = run.SolveDurationMilliseconds,
        datasetSchemaVersion = run.DatasetSchemaVersion,
        dataClassification = "Recommendation",
        network,
        movements = movements.Select(m => new
        {
            id = m.Id,
            originFacilityId = m.OriginFacilityId,
            destinationCustomerId = m.DestinationCustomerId,
            productId = m.ProductId,
            quantityPounds = m.QuantityPounds,
            truckId = m.TruckId,
            orderId = m.OrderId,
            expectedUnitPrice = m.ExpectedUnitPrice,
            expectedRevenue = m.ExpectedRevenue,
            transportationCost = m.TransportationCost,
            fuelCost = m.FuelCost,
            operatingCost = m.OperatingCost,
            distanceMiles = m.DistanceMiles,
            expectedContributionMargin = m.ExpectedContributionMargin,
            explanation = m.Explanation
        }),
        unusedInventory = JsonSerializer.Deserialize<object>(run.UnusedInventoryJson),
        unservedDemand = JsonSerializer.Deserialize<object>(run.UnservedDemandJson)
    };
}
