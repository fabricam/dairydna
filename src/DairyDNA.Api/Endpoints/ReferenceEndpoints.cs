using DairyDNA.Application.Reference;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static IEndpointRouteBuilder MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/network", async (Guid generationId, bool? activeOnly, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                var points = await handler.GetNetworkAsync(generationId, activeOnly ?? true, ct);
                return Results.Ok(new { dataClassification = "Synthetic", points });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        MapCrud(app, "farms",
            list: (h, g, a, ct) => h.ListFarmsAsync(g, a, ct),
            get: (h, id, ct) => h.GetFarmAsync(id, ct),
            create: async (h, body, ct) => Results.Created($"/api/farms/{body.Id}", await h.CreateFarmAsync(body, ct)),
            deactivate: async (h, id, ct) =>
            {
                var item = await h.DeactivateFarmAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

        app.MapGet("/api/facilities", async (Guid generationId, bool? activeOnly, FacilityType? facilityType, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                await handler.EnsureGenerationAsync(generationId, ct);
                return Results.Ok(await handler.ListFacilitiesAsync(generationId, activeOnly ?? true, facilityType, ct));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        app.MapGet("/api/facilities/{id:guid}", async (Guid id, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            var item = await handler.GetFacilityAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        app.MapPost("/api/facilities", async (Facility body, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                var created = await handler.CreateFacilityAsync(body, ct);
                return Results.Created($"/api/facilities/{created.Id}", created);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["facility"] = [ex.Message] }); }
        });
        app.MapPost("/api/facilities/{id:guid}/deactivate", async (Guid id, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            var (facility, warning) = await handler.DeactivateFacilityAsync(id, ct);
            if (facility is null) return Results.NotFound();
            return Results.Ok(new { facility, warning });
        });

        MapCrud(app, "customers",
            list: (h, g, a, ct) => h.ListCustomersAsync(g, a, ct),
            get: (h, id, ct) => h.GetCustomerAsync(id, ct),
            create: async (h, body, ct) => Results.Created($"/api/customers/{body.Id}", await h.CreateCustomerAsync(body, ct)),
            deactivate: async (h, id, ct) =>
            {
                var item = await h.DeactivateCustomerAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

        MapCrud(app, "products",
            list: (h, g, a, ct) => h.ListProductsAsync(g, a, ct),
            get: (h, id, ct) => h.GetProductAsync(id, ct),
            create: async (h, body, ct) => Results.Created($"/api/products/{body.Id}", await h.CreateProductAsync(body, ct)),
            deactivate: async (h, id, ct) =>
            {
                var item = await h.DeactivateProductAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

        MapCrud(app, "trucks",
            list: (h, g, a, ct) => h.ListTrucksAsync(g, a, ct),
            get: (h, id, ct) => h.GetTruckAsync(id, ct),
            create: async (h, body, ct) => Results.Created($"/api/trucks/{body.Id}", await h.CreateTruckAsync(body, ct)),
            deactivate: async (h, id, ct) =>
            {
                var item = await h.DeactivateTruckAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

        MapCrud(app, "contracts",
            list: (h, g, a, ct) => h.ListContractsAsync(g, a, ct),
            get: (h, id, ct) => h.GetContractAsync(id, ct),
            create: async (h, body, ct) => Results.Created($"/api/contracts/{body.Id}", await h.CreateContractAsync(body, ct)),
            deactivate: async (h, id, ct) =>
            {
                var item = await h.DeactivateContractAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

        app.MapGet("/api/shipments", async (Guid generationId, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                await handler.EnsureGenerationAsync(generationId, ct);
                return Results.Ok(await handler.ListShipmentsAsync(generationId, ct));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        app.MapGet("/api/shipments/{id:guid}", async (Guid id, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            var item = await handler.GetShipmentAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        app.MapPost("/api/inventory-lots", async (InventoryLot body, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                var created = await handler.CreateInventoryLotAsync(body, ct);
                return Results.Created($"/api/inventory-lots/{created.Id}", created);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["inventoryLot"] = [ex.Message] }); }
        });

        app.MapPost("/api/orders", async (Order body, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                var created = await handler.CreateOrderAsync(body, ct);
                return Results.Created($"/api/orders/{created.Id}", created);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [ex.Message] }); }
        });

        return app;
    }

    private static void MapCrud<T>(
        IEndpointRouteBuilder app,
        string route,
        Func<ReferenceDataHandlers, Guid, bool, CancellationToken, Task<List<T>>> list,
        Func<ReferenceDataHandlers, Guid, CancellationToken, Task<T?>> get,
        Func<ReferenceDataHandlers, T, CancellationToken, Task<IResult>> create,
        Func<ReferenceDataHandlers, Guid, CancellationToken, Task<IResult>> deactivate)
        where T : class
    {
        app.MapGet($"/api/{route}", async (Guid generationId, bool? activeOnly, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try
            {
                await handler.EnsureGenerationAsync(generationId, ct);
                return Results.Ok(await list(handler, generationId, activeOnly ?? true, ct));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        app.MapGet($"/api/{route}/{{id:guid}}", async (Guid id, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            var item = await get(handler, id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        app.MapPost($"/api/{route}", async (T body, ReferenceDataHandlers handler, CancellationToken ct) =>
        {
            try { return await create(handler, body, ct); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { [route] = [ex.Message] }); }
        });
        app.MapPost($"/api/{route}/{{id:guid}}/deactivate", async (Guid id, ReferenceDataHandlers handler, CancellationToken ct) =>
            await deactivate(handler, id, ct));
    }
}
