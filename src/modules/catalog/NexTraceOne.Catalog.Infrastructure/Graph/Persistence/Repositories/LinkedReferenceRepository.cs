using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class LinkedReferenceRepository(CatalogGraphDbContext context)
    : RepositoryBase<LinkedReference, LinkedReferenceId>(context), ILinkedReferenceRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public override async Task<LinkedReference?> GetByIdAsync(LinkedReferenceId id, CancellationToken ct = default)
        => await _context.LinkedReferences
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<LinkedReference>> ListByAssetAsync(
        Guid assetId,
        LinkedAssetType assetType,
        CancellationToken ct = default)
        => await _context.LinkedReferences
            .Where(r => r.AssetId == assetId && r.AssetType == assetType)
            .OrderBy(r => r.Title)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LinkedReference>> ListByAssetAndTypeAsync(
        Guid assetId,
        LinkedAssetType assetType,
        LinkedReferenceType referenceType,
        CancellationToken ct = default)
        => await _context.LinkedReferences
            .Where(r => r.AssetId == assetId && r.AssetType == assetType && r.ReferenceType == referenceType)
            .OrderBy(r => r.Title)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LinkedReference>> SearchAsync(
        string searchTerm,
        LinkedReferenceType? referenceType,
        CancellationToken ct = default)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var tsQuery = EF.Functions.PlainToTsQuery("simple", term);
        var query = _context.LinkedReferences.AsQueryable();

        if (referenceType.HasValue)
        {
            query = query.Where(r => r.ReferenceType == referenceType.Value);
        }

        query = query.Where(r =>
            EF.Functions.ToTsVector(
                "simple",
                (r.Title ?? string.Empty) + " " +
                (r.Description ?? string.Empty) + " " +
                (r.Content ?? string.Empty))
            .Matches(tsQuery));

        return await query
            .OrderByDescending(r =>
                EF.Functions.ToTsVector(
                    "simple",
                    (r.Title ?? string.Empty) + " " +
                    (r.Description ?? string.Empty) + " " +
                    (r.Content ?? string.Empty))
                .Rank(tsQuery))
            .ToListAsync(ct);
    }
}
