using DairyDNA.Application.Forecasting;
using DairyDNA.Domain.Enums;

namespace DairyDNA.Application.Governance;

public sealed record ModelVersionListItem(
    Guid Id,
    Guid GenerationId,
    string ModelFamily,
    string Algorithm,
    string FeatureSchemaVersion,
    string DatasetVersion,
    int RandomSeed,
    ForecastRunStatus TrainingStatus,
    ModelLifecycleStatus LifecycleStatus,
    bool MeetsAcceptanceBar,
    string? ArtifactChecksumSha256,
    DateTimeOffset TrainedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? RetiredAt,
    string? Notes,
    string DataClassification);

public sealed record GovernanceAuditEventDto(
    Guid Id,
    Guid ModelVersionId,
    string ModelFamily,
    string Action,
    string Actor,
    string Reason,
    DateTimeOffset At,
    string? Notes);

public sealed record ModelCard(
    ModelVersionListItem Version,
    string Intent,
    string DataSummary,
    ForecastMetrics? Metrics,
    string Limitations,
    string LeakageControlStatement,
    IReadOnlyList<GovernanceAuditEventDto> AuditTrail);

public sealed record OptimizerCatalogItem(string Version, string Description);

public sealed class PublishModelRequest
{
    public string? Actor { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool OverrideQualityGate { get; set; }
}

public sealed class RetireModelRequest
{
    public string? Actor { get; set; }
    public string Reason { get; set; } = string.Empty;
}
