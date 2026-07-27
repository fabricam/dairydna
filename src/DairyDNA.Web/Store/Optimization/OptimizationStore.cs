using Fluxor;

namespace DairyDNA.Web.Store.Optimization;

[FeatureState]
public sealed record OptimizationState
{
    public bool Loading { get; init; }
    public OptimizationDetailDto? Detail { get; init; }
    public string? Error { get; init; }
}

public sealed record RunOptimizationAction(Guid GenerationId);
public sealed record OptimizationSucceededAction(OptimizationDetailDto Detail);
public sealed record OptimizationFailedAction(string Error);

public sealed record OptimizationDetailDto(
    Guid id,
    Guid generationId,
    DateOnly asOfDate,
    string status,
    decimal objectiveValue,
    string optimizerVersion,
    int solveDurationMilliseconds,
    List<MovementDto> movements);

public sealed record MovementDto(
    Guid id,
    Guid originFacilityId,
    Guid destinationCustomerId,
    Guid productId,
    decimal quantityPounds,
    Guid truckId,
    Guid? orderId,
    decimal expectedUnitPrice,
    decimal expectedRevenue,
    decimal transportationCost,
    decimal expectedContributionMargin,
    string explanation);

public static class OptimizationReducers
{
    [ReducerMethod]
    public static OptimizationState OnRun(OptimizationState state, RunOptimizationAction _)
        => state with { Loading = true, Error = null };

    [ReducerMethod]
    public static OptimizationState OnSuccess(OptimizationState state, OptimizationSucceededAction action)
        => state with { Loading = false, Detail = action.Detail };

    [ReducerMethod]
    public static OptimizationState OnFail(OptimizationState state, OptimizationFailedAction action)
        => state with { Loading = false, Error = action.Error };
}

public sealed class OptimizationEffects
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OptimizationEffects(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [EffectMethod]
    public async Task HandleRun(RunOptimizationAction action, IDispatcher dispatcher)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("DairyDNA.Api");
            var response = await client.PostAsJsonAsync("api/optimization-runs", new { generationId = action.GenerationId, optimizerVersion = "naive-cm-v1" });
            response.EnsureSuccessStatusCode();
            var detail = await response.Content.ReadFromJsonAsync<OptimizationDetailDto>();
            if (detail is null) throw new InvalidOperationException("Empty optimization response");
            dispatcher.Dispatch(new OptimizationSucceededAction(detail));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new OptimizationFailedAction(ex.Message));
        }
    }
}
