using DairyDNA.Domain.Entities;

namespace DairyDNA.Application.Abstractions;

public static class GenerationProfileCatalog
{
    public const string ThinSlice = "thin-slice";
    public const string StandardDemo = "standard-demo";
    public const string Custom = "custom";
    public const string GeneratorVersion = "synthetic-gen-v2";
    public const string SchemaVersion = "dairydna.synthetic.v2";

    public static readonly GenerationLimits Limits = new(500, 50, 500, 200, 1200);

    public static IReadOnlyList<GenerationProfileDefinition> All { get; } =
    [
        new GenerationProfileDefinition(
            ThinSlice,
            "Interview thin slice (5/2/5/3, milk+cream, ~90 days)",
            FarmCount: 5, FacilityCount: 2, CustomerCount: 5, TruckCount: 3,
            ProductSet: "milk-cream",
            StartDate: new DateOnly(2025, 10, 1),
            EndDate: new DateOnly(2025, 12, 29),
            MissingnessRate: 0.02m,
            DenseHistoryDays: 90,
            SparseCadenceDays: 1),
        new GenerationProfileDefinition(
            StandardDemo,
            "Standard demo network (~150/8/75/30, 6 products, ~3 years; dense last 90d)",
            FarmCount: 150, FacilityCount: 8, CustomerCount: 75, TruckCount: 30,
            ProductSet: "standard-six",
            StartDate: new DateOnly(2023, 1, 1),
            EndDate: new DateOnly(2025, 12, 29),
            MissingnessRate: 0.03m,
            DenseHistoryDays: 90,
            SparseCadenceDays: 7)
    ];

    public static GenerationProfileDefinition? Find(string name) =>
        All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

public sealed record GenerationLimits(int MaxFarms, int MaxFacilities, int MaxCustomers, int MaxTrucks, int MaxDaySpan);

public sealed record GenerationProfileDefinition(
    string Name,
    string Description,
    int FarmCount,
    int FacilityCount,
    int CustomerCount,
    int TruckCount,
    string ProductSet,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MissingnessRate,
    int DenseHistoryDays,
    int SparseCadenceDays);

public sealed class SyntheticGenerationRequest
{
    public string ProfileName { get; set; } = GenerationProfileCatalog.ThinSlice;
    public string? ScenarioName { get; set; }
    public string SchemaVersion { get; set; } = GenerationProfileCatalog.SchemaVersion;
    public int RandomSeed { get; set; } = 104729;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? FarmCount { get; set; }
    public int? FacilityCount { get; set; }
    public int? CustomerCount { get; set; }
    public int? TruckCount { get; set; }
    public string? ProductSet { get; set; }
    public decimal? MissingnessRate { get; set; }
    public int? DenseHistoryDays { get; set; }
    public int? SparseCadenceDays { get; set; }
}

/// <summary>Backward-compatible alias used by 000 clients.</summary>
public sealed class ThinSliceGenerationRequest
{
    public string ScenarioName { get; set; } = "thin-vertical-slice";
    public string SchemaVersion { get; set; } = GenerationProfileCatalog.SchemaVersion;
    public int RandomSeed { get; set; } = 104729;
    public DateOnly StartDate { get; set; } = new(2025, 10, 1);
    public DateOnly EndDate { get; set; } = new(2025, 12, 29);
    public int FarmCount { get; set; } = 5;
    public int FacilityCount { get; set; } = 2;
    public int CustomerCount { get; set; } = 5;
    public int TruckCount { get; set; } = 3;

    public SyntheticGenerationRequest ToSynthetic() => new()
    {
        ProfileName = GenerationProfileCatalog.ThinSlice,
        ScenarioName = ScenarioName,
        SchemaVersion = SchemaVersion,
        RandomSeed = RandomSeed,
        StartDate = StartDate,
        EndDate = EndDate,
        FarmCount = FarmCount,
        FacilityCount = FacilityCount,
        CustomerCount = CustomerCount,
        TruckCount = TruckCount,
        ProductSet = "milk-cream",
        MissingnessRate = 0.02m,
        DenseHistoryDays = 90,
        SparseCadenceDays = 1
    };
}

public sealed class ValidationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Severity { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
}

public sealed class ValidationReport
{
    public bool Passed { get; set; }
    public IReadOnlyList<ValidationCheck> Checks { get; set; } = [];
    public decimal ObservedMissingnessRate { get; set; }
    public bool SeasonalVariationDetected { get; set; }
}

public interface ISyntheticDataGenerator
{
    Task<GenerationManifest> GenerateAsync(SyntheticGenerationRequest request, CancellationToken cancellationToken = default);
}

public interface IThinSliceGenerator
{
    Task<GenerationManifest> GenerateAsync(ThinSliceGenerationRequest request, CancellationToken cancellationToken = default);
}
