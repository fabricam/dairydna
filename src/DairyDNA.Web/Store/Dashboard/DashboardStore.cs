using Fluxor;

namespace DairyDNA.Web.Store.Dashboard;

[FeatureState]
public sealed record DashboardState
{
    public bool Loading { get; init; }
    public Guid? GenerationId { get; init; }
    public DateOnly? AsOfDate { get; init; }
    public DashboardDto? Model { get; init; }
    public string? Error { get; init; }
}

public sealed record LoadDashboardAction(Guid GenerationId, DateOnly? AsOfDate = null, bool IncludeInactive = false);
public sealed record DashboardLoadedAction(DashboardDto Model);
public sealed record DashboardFailedAction(string Error);

public sealed record DashboardDto(
    Guid generationId,
    DateOnly asOfDate,
    DateOnly datasetStart,
    DateOnly datasetEnd,
    string dataClassification,
    string? warning,
    List<InvRow> inventory,
    List<DemandRow> demand,
    List<FleetRow> fleet,
    List<AgeRow> inventoryAgeRisk,
    List<PriceRow> priceSeries,
    List<NetRow> network,
    int omittedFromMapCount);

public sealed record InvRow(Guid facilityId, string facilityName, string productCode, decimal quantityPounds, DateTimeOffset? oldestExpiresAt, int? daysToExpiry);
public sealed record DemandRow(Guid orderId, string customerName, string productCode, decimal requestedQuantityPounds, decimal offeredPricePerPound);
public sealed record FleetRow(Guid truckId, decimal maximumCapacityPounds, string status, bool active);
public sealed record AgeRow(string band, int lotCount, decimal quantityPounds, string riskLevel);
public sealed record PriceRow(DateOnly date, string productCode, decimal pricePerPound, string classification);
public sealed record NetRow(Guid id, string kind, string name, decimal latitude, decimal longitude, bool active);

public static class DashboardReducers
{
    [ReducerMethod]
    public static DashboardState OnLoad(DashboardState state, LoadDashboardAction action)
        => state with { Loading = true, Error = null, GenerationId = action.GenerationId, AsOfDate = action.AsOfDate };

    [ReducerMethod]
    public static DashboardState OnOk(DashboardState state, DashboardLoadedAction action)
        => state with { Loading = false, Model = action.Model, AsOfDate = action.Model.asOfDate, Error = null };

    [ReducerMethod]
    public static DashboardState OnFail(DashboardState state, DashboardFailedAction action)
        => state with { Loading = false, Model = null, Error = action.Error };
}

public sealed class DashboardEffects
{
    private readonly IHttpClientFactory _httpClientFactory;
    public DashboardEffects(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [EffectMethod]
    public async Task Handle(LoadDashboardAction action, IDispatcher dispatcher)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DairyDNA.Api");
            var url = $"api/dashboard?generationId={action.GenerationId}";
            if (action.AsOfDate is DateOnly d) url += $"&asOfDate={d:yyyy-MM-dd}";
            if (action.IncludeInactive) url += "&includeInactive=true";
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var err = await response.Content.ReadFromJsonAsync<ErrDto>();
                dispatcher.Dispatch(new DashboardFailedAction(err?.error ?? response.ReasonPhrase ?? "Dashboard load failed"));
                return;
            }
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadFromJsonAsync<DashboardDto>();
            if (model is null) throw new InvalidOperationException("Empty dashboard response");
            dispatcher.Dispatch(new DashboardLoadedAction(model));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new DashboardFailedAction(ex.Message));
        }
    }

    private sealed record ErrDto(string? error);
}
