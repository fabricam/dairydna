using Fluxor;
using DairyDNA.Web.Store.Shared;

namespace DairyDNA.Web.Store.Demo;

[FeatureState]
public sealed record DemoState
{
    public bool Loading { get; init; }
    public DemoSummaryDto? Summary { get; init; }
    public string? Error { get; init; }
}

public sealed record LoadDemoSummaryAction(Guid GenerationId);
public sealed record DemoSummaryLoadedAction(DemoSummaryDto Summary);
public sealed record DemoSummaryFailedAction(string Error);

public sealed record DemoSummaryDto(
    Guid generationId,
    DateOnly asOfDate,
    string dataClassification,
    List<InventoryRow> inventory,
    List<DemandRow> demand,
    List<PriceRow> prices,
    List<TruckRow> fleet,
    List<NetworkPointDto> network);

public sealed record InventoryRow(Guid facilityId, string facilityName, string productCode, decimal quantityPounds, DateTimeOffset? oldestExpiresAt);
public sealed record DemandRow(Guid orderId, string customerName, string productCode, decimal requestedQuantityPounds, decimal offeredPricePerPound);
public sealed record PriceRow(string productCode, decimal pricePerPound, string priceType);
public sealed record TruckRow(Guid truckId, decimal maximumCapacityPounds, string status);

public static class DemoReducers
{
    [ReducerMethod]
    public static DemoState OnLoad(DemoState state, LoadDemoSummaryAction _)
        => state with { Loading = true, Error = null };

    [ReducerMethod]
    public static DemoState OnLoaded(DemoState state, DemoSummaryLoadedAction action)
        => state with { Loading = false, Summary = action.Summary };

    [ReducerMethod]
    public static DemoState OnFail(DemoState state, DemoSummaryFailedAction action)
        => state with { Loading = false, Error = action.Error };
}

public sealed class DemoEffects
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DemoEffects(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [EffectMethod]
    public async Task HandleLoad(LoadDemoSummaryAction action, IDispatcher dispatcher)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DairyDNA.Api");
            var summary = await client.GetFromJsonAsync<DemoSummaryDto>($"api/demo/summary?generationId={action.GenerationId}");
            if (summary is null) throw new InvalidOperationException("Empty demo summary");
            dispatcher.Dispatch(new DemoSummaryLoadedAction(summary));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new DemoSummaryFailedAction(ex.Message));
        }
    }
}
