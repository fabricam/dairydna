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
    public string? ProfileName { get; init; }
    public string? Error { get; init; }
}

public sealed record GenerateSyntheticAction(
    string ProfileName = "thin-slice",
    int RandomSeed = 104729,
    int? FarmCount = null,
    int? FacilityCount = null,
    int? CustomerCount = null,
    int? TruckCount = null);

/// <summary>Backward-compatible alias.</summary>
public sealed record GenerateThinSliceAction(int RandomSeed = 104729);

public sealed record GenerationSucceededAction(Guid Id, string Status, DateOnly PlanningDate, int RandomSeed, string? ProfileName);
public sealed record GenerationFailedAction(string Error);

public static class GenerationReducers
{
    [ReducerMethod]
    public static GenerationState OnGenerate(GenerationState state, GenerateSyntheticAction _)
        => state with { Loading = true, Error = null };

    [ReducerMethod]
    public static GenerationState OnGenerateLegacy(GenerationState state, GenerateThinSliceAction _)
        => state with { Loading = true, Error = null };

    [ReducerMethod]
    public static GenerationState OnSuccess(GenerationState state, GenerationSucceededAction action)
        => state with
        {
            Loading = false,
            GenerationId = action.Id,
            Status = action.Status,
            PlanningDate = action.PlanningDate,
            RandomSeed = action.RandomSeed,
            ProfileName = action.ProfileName
        };

    [ReducerMethod]
    public static GenerationState OnFail(GenerationState state, GenerationFailedAction action)
        => state with { Loading = false, Error = action.Error };
}

public sealed class GenerationEffects
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GenerationEffects(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [EffectMethod]
    public Task HandleLegacyGenerate(GenerateThinSliceAction action, IDispatcher dispatcher)
    {
        dispatcher.Dispatch(new GenerateSyntheticAction("thin-slice", action.RandomSeed));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleGenerate(GenerateSyntheticAction action, IDispatcher dispatcher)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DairyDNA.Api");
            var body = new Dictionary<string, object?>
            {
                ["profileName"] = action.ProfileName,
                ["randomSeed"] = action.RandomSeed,
                ["scenarioName"] = action.ProfileName
            };
            if (action.FarmCount is int f) body["farmCount"] = f;
            if (action.FacilityCount is int fac) body["facilityCount"] = fac;
            if (action.CustomerCount is int c) body["customerCount"] = c;
            if (action.TruckCount is int t) body["truckCount"] = t;

            var response = await client.PostAsJsonAsync("api/generation-runs", body);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Generation failed ({(int)response.StatusCode}): {err}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GenerationDto>();
            if (payload is null) throw new InvalidOperationException("Empty generation response");
            dispatcher.Dispatch(new GenerationSucceededAction(
                payload.id, payload.status, DateOnly.Parse(payload.planningDate!), payload.randomSeed, payload.profileName));
            dispatcher.Dispatch(new LoadDemoSummaryAction(payload.id));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new GenerationFailedAction(ex.Message));
        }
    }

    private sealed record GenerationDto(Guid id, string status, string? planningDate, int randomSeed, string? profileName);
}
