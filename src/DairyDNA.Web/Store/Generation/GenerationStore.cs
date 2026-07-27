using Fluxor;
using DairyDNA.Web.Store.Demo;

namespace DairyDNA.Web.Store.Generation;

[FeatureState]
public sealed record GenerationState
{
    public bool Loading { get; init; }
    public Guid? GenerationId { get; init; }
    public string? Status { get; init; }
    public DateOnly? PlanningDate { get; init; }
    public int? RandomSeed { get; init; }
    public string? Error { get; init; }
}

public sealed record GenerateThinSliceAction(int RandomSeed = 104729);
public sealed record GenerationSucceededAction(Guid Id, string Status, DateOnly PlanningDate, int RandomSeed);
public sealed record GenerationFailedAction(string Error);

public static class GenerationReducers
{
    [ReducerMethod]
    public static GenerationState OnGenerate(GenerationState state, GenerateThinSliceAction _)
        => state with { Loading = true, Error = null };

    [ReducerMethod]
    public static GenerationState OnSuccess(GenerationState state, GenerationSucceededAction action)
        => state with { Loading = false, GenerationId = action.Id, Status = action.Status, PlanningDate = action.PlanningDate, RandomSeed = action.RandomSeed };

    [ReducerMethod]
    public static GenerationState OnFail(GenerationState state, GenerationFailedAction action)
        => state with { Loading = false, Error = action.Error };
}

public sealed class GenerationEffects
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GenerationEffects(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [EffectMethod]
    public async Task HandleGenerate(GenerateThinSliceAction action, IDispatcher dispatcher)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DairyDNA.Api");
            var response = await client.PostAsJsonAsync("api/generation-runs", new
            {
                scenarioName = "thin-vertical-slice",
                schemaVersion = "dairydna.thin-slice.v1",
                randomSeed = action.RandomSeed,
                startDate = "2025-10-01",
                endDate = "2025-12-29",
                farmCount = 5,
                facilityCount = 2,
                customerCount = 5,
                truckCount = 3
            });
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<GenerationDto>();
            if (payload is null) throw new InvalidOperationException("Empty generation response");
            dispatcher.Dispatch(new GenerationSucceededAction(payload.id, payload.status, DateOnly.Parse(payload.planningDate!), payload.randomSeed));
            dispatcher.Dispatch(new LoadDemoSummaryAction(payload.id));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new GenerationFailedAction(ex.Message));
        }
    }

    private sealed record GenerationDto(Guid id, string status, string? planningDate, int randomSeed);
}
