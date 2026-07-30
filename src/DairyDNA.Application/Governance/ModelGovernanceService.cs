using System.Text.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Forecasting;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Governance;

public interface IModelGovernanceService
{
    Task<IReadOnlyList<ModelVersionListItem>> ListAsync(string? family, CancellationToken ct = default);
    Task<ModelVersionListItem?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ModelCard?> GetCardAsync(Guid id, CancellationToken ct = default);
    Task<ModelVersionListItem> PublishAsync(Guid id, string? actor, string reason, bool overrideQualityGate, CancellationToken ct = default);
    Task<ModelVersionListItem> RetireAsync(Guid id, string? actor, string reason, CancellationToken ct = default);
    IReadOnlyList<OptimizerCatalogItem> GetOptimizerCatalog();
}

public sealed class ModelGovernanceService : IModelGovernanceService
{
    private const string DefaultActor = "demo-admin";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDairyDnaDbContext _db;

    public ModelGovernanceService(IDairyDnaDbContext db) => _db = db;

    public async Task<IReadOnlyList<ModelVersionListItem>> ListAsync(string? family, CancellationToken ct = default)
    {
        var items = new List<ModelVersionListItem>();
        if (IncludeFamily(family, "supply"))
            items.AddRange((await _db.SupplyModelVersions.ToListAsync(ct)).Select(m => ToListItem(m, "supply")));
        if (IncludeFamily(family, "demand"))
            items.AddRange((await _db.DemandModelVersions.ToListAsync(ct)).Select(m => ToListItem(m, "demand")));
        if (IncludeFamily(family, "price"))
            items.AddRange((await _db.PriceModelVersions.ToListAsync(ct)).Select(m => ToListItem(m, "price")));
        return items.OrderByDescending(i => i.TrainedAt).ToList();
    }

    public async Task<ModelVersionListItem?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var (model, family) = await FindAsync(id, ct);
        return model is null ? null : ToListItem(model, family);
    }

    public async Task<ModelCard?> GetCardAsync(Guid id, CancellationToken ct = default)
    {
        var (model, family) = await FindAsync(id, ct);
        if (model is null) return null;

        ForecastMetrics? metrics = null;
        try { metrics = JsonSerializer.Deserialize<ForecastMetrics>(model.MetricsJson, JsonOptions); }
        catch (JsonException) { /* metrics unavailable; card still renders without the chart data */ }

        var audit = await _db.GovernanceAuditEvents
            .Where(a => a.ModelVersionId == id)
            .OrderByDescending(a => a.At)
            .ToListAsync(ct);

        return new ModelCard(
            ToListItem(model, family),
            Intent: $"{Capitalize(family)} forecasting model version trained on generation {model.GenerationId} using {model.Algorithm}.",
            DataSummary: $"Dataset version '{model.DatasetVersion}', feature schema '{model.FeatureSchemaVersion}', random seed {model.RandomSeed}.",
            Metrics: metrics,
            Limitations: "This model produces estimates for demo and portfolio purposes only. It is not production advice and must not be used as the sole basis for an operational decision.",
            LeakageControlStatement: "Training features are built only from history available strictly before the forecast date, and evaluation uses a time-ordered train/test split so no future information leaks into reported metrics.",
            AuditTrail: audit.Select(ToAuditDto).ToList());
    }

    public async Task<ModelVersionListItem> PublishAsync(Guid id, string? actor, string reason, bool overrideQualityGate, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var (model, family) = await FindAsync(id, ct);
        if (model is null) throw new KeyNotFoundException("Model version was not found.");
        if (string.IsNullOrWhiteSpace(model.ArtifactChecksumSha256))
            throw new InvalidOperationException("Cannot publish a model version without an artifact checksum.");

        var overriding = !model.MeetsAcceptanceBar;
        if (overriding && !overrideQualityGate)
            throw new InvalidOperationException("Model version does not meet the quality gate. Set overrideQualityGate to true to publish anyway.");

        var actorName = NormalizeActor(actor);
        var trimmedReason = reason.Trim();
        var now = DateTimeOffset.UtcNow;

        await RetirePublishedPeersAsync(family, model.GenerationId, model.Id, actorName, "Superseded by a newly published version.", now, ct);

        model.LifecycleStatus = ModelLifecycleStatus.Published;
        model.PublishedAt = now;
        model.RetiredAt = null;

        _db.Add(new GovernanceAuditEvent
        {
            Id = Guid.NewGuid(),
            ModelVersionId = model.Id,
            ModelFamily = family,
            Action = overriding ? "Override" : "Publish",
            Actor = actorName,
            Reason = trimmedReason,
            At = now,
            Notes = overriding ? "Published despite not meeting the quality acceptance bar." : null
        });

        await _db.SaveChangesAsync(ct);
        return ToListItem(model, family);
    }

    public async Task<ModelVersionListItem> RetireAsync(Guid id, string? actor, string reason, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var (model, family) = await FindAsync(id, ct);
        if (model is null) throw new KeyNotFoundException("Model version was not found.");

        var actorName = NormalizeActor(actor);
        var now = DateTimeOffset.UtcNow;
        model.LifecycleStatus = ModelLifecycleStatus.Retired;
        model.RetiredAt = now;

        _db.Add(new GovernanceAuditEvent
        {
            Id = Guid.NewGuid(),
            ModelVersionId = model.Id,
            ModelFamily = family,
            Action = "Retire",
            Actor = actorName,
            Reason = reason.Trim(),
            At = now
        });

        await _db.SaveChangesAsync(ct);
        return ToListItem(model, family);
    }

    public IReadOnlyList<OptimizerCatalogItem> GetOptimizerCatalog() =>
    [
        new("ortools-cm-v1", "OR-Tools CP-SAT contribution-margin optimizer; default allocation solver for movement recommendations."),
        new("naive-cm-v1", "Naive greedy contribution-margin optimizer used as a regression/comparison baseline against OR-Tools."),
        new("transport-cost-v2", "Distance- and time-based transportation costing model with fuel price and empty-return assumptions.")
    ];

    private async Task RetirePublishedPeersAsync(string family, Guid generationId, Guid excludeId, string actor, string reason, DateTimeOffset at, CancellationToken ct)
    {
        switch (family)
        {
            case "supply":
                foreach (var peer in await _db.SupplyModelVersions
                    .Where(m => m.GenerationId == generationId && m.LifecycleStatus == ModelLifecycleStatus.Published && m.Id != excludeId)
                    .ToListAsync(ct))
                    RetirePeer(peer, family, actor, reason, at);
                break;
            case "demand":
                foreach (var peer in await _db.DemandModelVersions
                    .Where(m => m.GenerationId == generationId && m.LifecycleStatus == ModelLifecycleStatus.Published && m.Id != excludeId)
                    .ToListAsync(ct))
                    RetirePeer(peer, family, actor, reason, at);
                break;
            case "price":
                foreach (var peer in await _db.PriceModelVersions
                    .Where(m => m.GenerationId == generationId && m.LifecycleStatus == ModelLifecycleStatus.Published && m.Id != excludeId)
                    .ToListAsync(ct))
                    RetirePeer(peer, family, actor, reason, at);
                break;
        }
    }

    private void RetirePeer(IModelVersion peer, string family, string actor, string reason, DateTimeOffset at)
    {
        peer.LifecycleStatus = ModelLifecycleStatus.Retired;
        peer.RetiredAt = at;
        _db.Add(new GovernanceAuditEvent
        {
            Id = Guid.NewGuid(),
            ModelVersionId = peer.Id,
            ModelFamily = family,
            Action = "Retire",
            Actor = actor,
            Reason = reason,
            At = at
        });
    }

    private async Task<(IModelVersion? Model, string Family)> FindAsync(Guid id, CancellationToken ct)
    {
        var supply = await _db.SupplyModelVersions.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (supply is not null) return (supply, "supply");
        var demand = await _db.DemandModelVersions.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (demand is not null) return (demand, "demand");
        var price = await _db.PriceModelVersions.FirstOrDefaultAsync(m => m.Id == id, ct);
        return price is not null ? (price, "price") : (null, string.Empty);
    }

    private static bool IncludeFamily(string? filter, string family) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(filter, family, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeActor(string? actor) => string.IsNullOrWhiteSpace(actor) ? DefaultActor : actor.Trim();

    private static string Capitalize(string value) => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static ModelVersionListItem ToListItem(IModelVersion m, string family) => new(
        m.Id,
        m.GenerationId,
        family,
        m.Algorithm,
        m.FeatureSchemaVersion,
        m.DatasetVersion,
        m.RandomSeed,
        m.Status,
        m.LifecycleStatus,
        m.MeetsAcceptanceBar,
        m.ArtifactChecksumSha256,
        m.TrainedAt,
        m.PublishedAt,
        m.RetiredAt,
        m.Notes,
        m.DataClassification);

    private static GovernanceAuditEventDto ToAuditDto(GovernanceAuditEvent e) =>
        new(e.Id, e.ModelVersionId, e.ModelFamily, e.Action, e.Actor, e.Reason, e.At, e.Notes);
}
