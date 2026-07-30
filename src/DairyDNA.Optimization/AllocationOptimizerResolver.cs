using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Optimization;

namespace DairyDNA.Optimization;

public sealed class AllocationOptimizerResolver : IAllocationOptimizerResolver
{
    private readonly OrToolsContributionMarginOptimizer _orTools;
    private readonly NaiveContributionMarginOptimizer _naive;

    public AllocationOptimizerResolver(
        OrToolsContributionMarginOptimizer orTools,
        NaiveContributionMarginOptimizer naive)
    {
        _orTools = orTools;
        _naive = naive;
    }

    public IAllocationOptimizer Resolve(string? version)
    {
        if (string.Equals(version, "naive-cm-v1", StringComparison.OrdinalIgnoreCase))
            return _naive;
        return _orTools;
    }
}
