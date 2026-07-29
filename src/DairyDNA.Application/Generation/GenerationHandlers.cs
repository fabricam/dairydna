using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DairyDNA.Application.Generation;

public sealed class CreateGenerationRunHandler
{
    private readonly ISyntheticDataGenerator _generator;

    public CreateGenerationRunHandler(ISyntheticDataGenerator generator) => _generator = generator;

    public Task<GenerationManifest> HandleAsync(SyntheticGenerationRequest request, CancellationToken ct = default)
        => _generator.GenerateAsync(request, ct);

    public Task<GenerationManifest> HandleAsync(ThinSliceGenerationRequest request, CancellationToken ct = default)
        => _generator.GenerateAsync(request.ToSynthetic(), ct);
}

public sealed class GetGenerationRunHandler
{
    private readonly IDairyDnaDbContext _db;

    public GetGenerationRunHandler(IDairyDnaDbContext db) => _db = db;

    public Task<GenerationManifest?> HandleAsync(Guid id, CancellationToken ct = default)
        => _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == id, ct);
}

public sealed class ListGenerationRunsHandler
{
    private readonly IDairyDnaDbContext _db;

    public ListGenerationRunsHandler(IDairyDnaDbContext db) => _db = db;

    public async Task<IReadOnlyList<GenerationManifest>> HandleAsync(CancellationToken ct = default)
        => await _db.GenerationManifests.OrderByDescending(x => x.GeneratedAt).ToListAsync(ct);
}

public sealed class GetValidationReportHandler
{
    private readonly IDairyDnaDbContext _db;

    public GetValidationReportHandler(IDairyDnaDbContext db) => _db = db;

    public async Task<ValidationReport?> HandleAsync(Guid generationId, CancellationToken ct = default)
    {
        var gen = await _db.GenerationManifests.FirstOrDefaultAsync(x => x.Id == generationId, ct);
        if (gen is null) return null;
        return JsonSerializer.Deserialize<ValidationReport>(gen.ValidationReportJson) ?? new ValidationReport { Passed = false };
    }
}
