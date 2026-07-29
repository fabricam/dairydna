using DairyDNA.Application.Dashboard;

namespace DairyDNA.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (
            Guid generationId,
            DateOnly? asOfDate,
            bool? includeInactive,
            GetDashboardHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleDetailedAsync(generationId, asOfDate, includeInactive ?? false, ct);
            return result.Status switch
            {
                DashboardQueryStatus.NotFound => Results.NotFound(new { error = result.Error, dataClassification = "Synthetic" }),
                DashboardQueryStatus.BadRequest => Results.BadRequest(new { error = result.Error, dataClassification = "Synthetic" }),
                _ => Results.Ok(result.Model)
            };
        });

        app.MapGet("/api/dashboard/facilities/{facilityId:guid}", async (
            Guid facilityId,
            Guid generationId,
            DateOnly? asOfDate,
            GetDashboardHandler handler,
            CancellationToken ct) =>
        {
            var detail = await handler.GetFacilityAsync(generationId, facilityId, asOfDate, ct);
            return detail is null ? Results.NotFound(new { error = "Facility not found for dataset." }) : Results.Ok(detail);
        });

        return app;
    }
}
