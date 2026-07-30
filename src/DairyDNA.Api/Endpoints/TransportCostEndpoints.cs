using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Transport;

namespace DairyDNA.Api.Endpoints;

public static class TransportCostEndpoints
{
    public static IEndpointRouteBuilder MapTransportCostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transport-cost");

        group.MapPost("/", (TransportCostRequest request, ITransportCostCalculator calculator) =>
        {
            try
            {
                return Results.Ok(calculator.Calculate(request));
            }
            catch (ArgumentException ex)
            {
                var field = string.IsNullOrWhiteSpace(ex.ParamName) ? "request" : ex.ParamName;
                return Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [ex.Message] });
            }
        });

        group.MapGet("/assumptions", () => Results.Ok(new
        {
            costingModelVersion = TransportCostCalculator.CostingModelVersion,
            averageSpeedMph = TransportCostCalculator.AverageMph,
            loadUnloadHours = TransportCostCalculator.DefaultLoadUnloadHours,
            defaultFuelPricePerGallon = TransportCostCalculator.DefaultFuelPricePerGallon,
            defaultMpg = TransportCostCalculator.DefaultMpg,
            defaultEmptyReturnIncluded = true,
            assumptions = TransportCostCalculator.Assumptions
        }));

        return app;
    }
}
