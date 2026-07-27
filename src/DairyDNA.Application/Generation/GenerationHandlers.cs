using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Application.Generation;

public sealed class CreateGenerationRunHandler
{
    private readonly IThinSliceGenerator _generator;

    public CreateGenerationRunHandler(IThinSliceGenerator generator) => _generator = generator;

    public Task<GenerationManifest> HandleAsync(ThinSliceGenerationRequest request, CancellationToken ct = default)
        => _generator.GenerateAsync(request, ct);
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
