using DairyDNA.Application.Governance;

namespace DairyDNA.Api.Endpoints;

public static class ModelGovernanceEndpoints
{
    public static RouteGroupBuilder MapModelGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/models");

        group.MapGet("/", async (string? family, IModelGovernanceService service, CancellationToken ct) =>
            Results.Ok(new
            {
                dataClassification = "Forecast",
                items = (await service.ListAsync(family, ct)).Select(ToDto)
            }));

        group.MapGet("/optimizers", (IModelGovernanceService service) =>
            Results.Ok(new { items = service.GetOptimizerCatalog().Select(o => new { version = o.Version, description = o.Description }) }));

        group.MapGet("/{id:guid}", async (Guid id, IModelGovernanceService service, CancellationToken ct) =>
        {
            var model = await service.GetAsync(id, ct);
            return model is null ? Results.NotFound() : Results.Ok(ToDto(model));
        });

        group.MapGet("/{id:guid}/card", async (Guid id, IModelGovernanceService service, CancellationToken ct) =>
        {
            var card = await service.GetCardAsync(id, ct);
            return card is null ? Results.NotFound() : Results.Ok(ToCardDto(card));
        });

        group.MapPost("/{id:guid}/publish", async (Guid id, PublishModelRequest body, IModelGovernanceService service, CancellationToken ct) =>
        {
            try
            {
                var model = await service.PublishAsync(id, body.Actor, body.Reason, body.OverrideQualityGate, ct);
                return Results.Ok(ToDto(model));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/retire", async (Guid id, RetireModelRequest body, IModelGovernanceService service, CancellationToken ct) =>
        {
            try
            {
                var model = await service.RetireAsync(id, body.Actor, body.Reason, ct);
                return Results.Ok(ToDto(model));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        return group;
    }

    private static object ToDto(ModelVersionListItem m) => new
    {
        id = m.Id,
        generationId = m.GenerationId,
        modelFamily = m.ModelFamily,
        algorithm = m.Algorithm,
        featureSchemaVersion = m.FeatureSchemaVersion,
        datasetVersion = m.DatasetVersion,
        randomSeed = m.RandomSeed,
        trainingStatus = m.TrainingStatus.ToString(),
        lifecycleStatus = m.LifecycleStatus.ToString(),
        meetsAcceptanceBar = m.MeetsAcceptanceBar,
        artifactChecksumSha256 = m.ArtifactChecksumSha256,
        trainedAt = m.TrainedAt,
        publishedAt = m.PublishedAt,
        retiredAt = m.RetiredAt,
        notes = m.Notes,
        dataClassification = m.DataClassification
    };

    private static object ToCardDto(ModelCard card) => new
    {
        version = ToDto(card.Version),
        intent = card.Intent,
        dataSummary = card.DataSummary,
        metrics = card.Metrics,
        limitations = card.Limitations,
        leakageControlStatement = card.LeakageControlStatement,
        auditTrail = card.AuditTrail.Select(a => new
        {
            a.Id,
            a.ModelVersionId,
            a.ModelFamily,
            a.Action,
            a.Actor,
            a.Reason,
            a.At,
            a.Notes
        })
    };
}
