using System.Text.Json;
using DairyDNA.Application.Forecasting;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Api.Endpoints;

public static class ForecastEndpoints
{
    public static IEndpointRouteBuilder MapForecastEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forecasts/supply");

        group.MapPost("/runs", async (SupplyForecastRequest body, ISupplyForecastService service, CancellationToken ct) =>
        {
            try
            {
                var model = await service.TrainAndPublishAsync(body, ct);
                return Results.Accepted($"/api/forecasts/supply/models/{model.Id}", ToModelDto(model));
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["generationId"] = [ex.Message] });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/models/latest", async (Guid generationId, ISupplyForecastService service, CancellationToken ct) =>
        {
            var model = await service.GetLatestModelAsync(generationId, ct);
            return model is null ? Results.NotFound() : Results.Ok(ToModelDto(model));
        });

        group.MapGet("/", async (Guid generationId, Guid? facilityId, string? regionCode, ISupplyForecastService service, CancellationToken ct) =>
        {
            var rows = await service.GetForecastsAsync(generationId, facilityId, regionCode, ct);
            return Results.Ok(new
            {
                dataClassification = "Forecast",
                disclaimer = "Supply forecasts are estimates with uncertainty bands — not guaranteed volumes.",
                items = rows.Select(f => new
                {
                    id = f.Id,
                    modelVersionId = f.ModelVersionId,
                    aggregationLevel = f.AggregationLevel.ToString(),
                    facilityId = f.FacilityId,
                    regionCode = f.RegionCode,
                    productCode = f.ProductCode,
                    asOfDate = f.AsOfDate,
                    targetDate = f.TargetDate,
                    horizonDays = f.HorizonDays,
                    pointEstimatePounds = f.PointEstimatePounds,
                    lowerBoundPounds = f.LowerBoundPounds,
                    upperBoundPounds = f.UpperBoundPounds,
                    coldStart = f.ColdStart,
                    dataClassification = f.DataClassification
                })
            });
        });

        group.MapGet("/actuals", async (Guid generationId, Guid facilityId, ISupplyForecastService service, CancellationToken ct) =>
        {
            var actuals = await service.GetActualsAsync(generationId, facilityId, ct);
            return Results.Ok(new
            {
                dataClassification = "Synthetic",
                items = actuals.Select(a => new { date = a.Date, actualPounds = a.ActualPounds })
            });
        });

        var demand = app.MapGroup("/api/forecasts/demand");
        demand.MapPost("/runs", async (DemandForecastRequest body, IDemandForecastService service, CancellationToken ct) =>
        {
            try
            {
                var model = await service.TrainAndPublishAsync(body, ct);
                return Results.Accepted($"/api/forecasts/demand/models/{model.Id}", ToDemandModelDto(model));
            }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["generationId"] = [ex.Message] }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
        demand.MapGet("/models/latest", async (Guid generationId, IDemandForecastService service, CancellationToken ct) =>
        {
            var model = await service.GetLatestModelAsync(generationId, ct);
            return model is null ? Results.NotFound() : Results.Ok(ToDemandModelDto(model));
        });
        demand.MapGet("/", async (Guid generationId, Guid? customerId, string? regionCode, IDemandForecastService service, CancellationToken ct) =>
        {
            var rows = await service.GetForecastsAsync(generationId, customerId, regionCode, ct);
            return Results.Ok(new
            {
                dataClassification = "Forecast",
                disclaimer = "Demand forecasts are estimates with uncertainty bands — not open orders or guaranteed demand.",
                items = rows.Select(f => new { id = f.Id, modelVersionId = f.ModelVersionId, aggregationLevel = f.AggregationLevel.ToString(), customerId = f.CustomerId, regionCode = f.RegionCode, productCode = f.ProductCode, asOfDate = f.AsOfDate, targetDate = f.TargetDate, horizonDays = f.HorizonDays, pointEstimatePounds = f.PointEstimatePounds, lowerBoundPounds = f.LowerBoundPounds, upperBoundPounds = f.UpperBoundPounds, coldStart = f.ColdStart, dataClassification = f.DataClassification })
            });
        });
        demand.MapGet("/actuals", async (Guid generationId, Guid customerId, IDemandForecastService service, CancellationToken ct) =>
        {
            var actuals = await service.GetActualsAsync(generationId, customerId, ct);
            return Results.Ok(new { dataClassification = "Synthetic", items = actuals.Select(a => new { date = a.Date, actualPounds = a.ActualPounds }) });
        });

        var price = app.MapGroup("/api/forecasts/price");
        price.MapPost("/runs", async (PriceForecastRequest body, IPriceForecastService service, CancellationToken ct) =>
        {
            try
            {
                var model = await service.TrainAndPublishAsync(body, ct);
                return Results.Accepted($"/api/forecasts/price/models/{model.Id}", ToPriceModelDto(model));
            }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["generationId"] = [ex.Message] }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
        price.MapGet("/models/latest", async (Guid generationId, IPriceForecastService service, CancellationToken ct) =>
        {
            var model = await service.GetLatestModelAsync(generationId, ct);
            return model is null ? Results.NotFound() : Results.Ok(ToPriceModelDto(model));
        });
        price.MapGet("/", async (Guid generationId, string? productCode, string? regionCode, IPriceForecastService service, CancellationToken ct) =>
        {
            var rows = await service.GetForecastsAsync(generationId, productCode, regionCode, ct);
            return Results.Ok(new
            {
                dataClassification = "Forecast",
                disclaimer = "Price forecasts are estimates with uncertainty bands — not trade quotes or guaranteed clearing prices.",
                items = rows.Select(x => new { id = x.Id, modelVersionId = x.ModelVersionId, aggregationLevel = x.AggregationLevel.ToString(), regionCode = x.RegionCode, productCode = x.ProductCode, asOfDate = x.AsOfDate, targetDate = x.TargetDate, horizonDays = x.HorizonDays, pointEstimatePricePerPound = x.PointEstimatePricePerPound, lowerBoundPricePerPound = x.LowerBoundPricePerPound, upperBoundPricePerPound = x.UpperBoundPricePerPound, dataClassification = x.DataClassification })
            });
        });
        price.MapGet("/actuals", async (Guid generationId, string productCode, IPriceForecastService service, CancellationToken ct) =>
        {
            var actuals = await service.GetActualsAsync(generationId, productCode, ct);
            return Results.Ok(new { dataClassification = "Synthetic", items = actuals.Select(x => new { date = x.Date, actualPricePerPound = x.ActualPricePerPound }) });
        });
        price.MapGet("/optimization-bundle", async (Guid generationId, DateOnly asOfDate, IPriceForecastService service, CancellationToken ct) =>
            Results.Ok(await service.GetOptimizationBundleAsync(generationId, asOfDate, ct)));

        return app;
    }

    private static object ToModelDto(DairyDNA.Domain.Entities.SupplyModelVersion m)
    {
        object? metrics = null;
        try { metrics = JsonSerializer.Deserialize<JsonElement>(m.MetricsJson); } catch { /* ignore */ }
        return new
        {
            id = m.Id,
            generationId = m.GenerationId,
            modelFamily = m.ModelFamily,
            algorithm = m.Algorithm,
            featureSchemaVersion = m.FeatureSchemaVersion,
            datasetVersion = m.DatasetVersion,
            randomSeed = m.RandomSeed,
            status = m.Status.ToString(),
            meetsAcceptanceBar = m.MeetsAcceptanceBar,
            trainedAt = m.TrainedAt,
            notes = m.Notes,
            metrics,
            dataClassification = m.DataClassification,
            disclaimer = "Model metadata for forecasts — not guaranteed supply."
        };
    }

    private static object ToDemandModelDto(DairyDNA.Domain.Entities.DemandModelVersion m)
    {
        object? metrics = null;
        try { metrics = JsonSerializer.Deserialize<JsonElement>(m.MetricsJson); } catch { /* ignore */ }
        return new { id = m.Id, generationId = m.GenerationId, modelFamily = m.ModelFamily, algorithm = m.Algorithm, featureSchemaVersion = m.FeatureSchemaVersion, datasetVersion = m.DatasetVersion, randomSeed = m.RandomSeed, status = m.Status.ToString(), meetsAcceptanceBar = m.MeetsAcceptanceBar, trainedAt = m.TrainedAt, notes = m.Notes, metrics, dataClassification = m.DataClassification, disclaimer = "Model metadata for demand forecasts — not guaranteed demand." };
    }

    private static object ToPriceModelDto(DairyDNA.Domain.Entities.PriceModelVersion m)
    {
        object? metrics = null;
        try { metrics = JsonSerializer.Deserialize<JsonElement>(m.MetricsJson); } catch { /* ignore */ }
        return new { id = m.Id, generationId = m.GenerationId, modelFamily = m.ModelFamily, algorithm = m.Algorithm, featureSchemaVersion = m.FeatureSchemaVersion, datasetVersion = m.DatasetVersion, randomSeed = m.RandomSeed, status = m.Status.ToString(), meetsAcceptanceBar = m.MeetsAcceptanceBar, trainedAt = m.TrainedAt, notes = m.Notes, metrics, dataClassification = m.DataClassification, disclaimer = "Model metadata for forecast prices — not trade quotes or guaranteed clearing prices." };
    }
}
