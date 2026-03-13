using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de releases, implementando consultas específicas de negócio.
/// </summary>
internal sealed class ReleaseRepository(ChangeIntelligenceDbContext context)
    : RepositoryBase<Release, ReleaseId>(context), IReleaseRepository
{
    /// <summary>Busca uma Release pelo seu identificador.</summary>
    public override async Task<Release?> GetByIdAsync(ReleaseId id, CancellationToken ct = default)
        => await context.Releases
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Busca releases de um ativo de API por versão e ambiente.</summary>
    public async Task<Release?> GetByApiAssetAndVersionAsync(Guid apiAssetId, string version, string environment, CancellationToken cancellationToken = default)
        => await context.Releases
            .SingleOrDefaultAsync(r => r.ApiAssetId == apiAssetId
                && r.Version == version
                && r.Environment == environment, cancellationToken);

    /// <summary>Lista releases de um ativo de API ordenadas por data de criação descendente.</summary>
    public async Task<IReadOnlyList<Release>> ListByApiAssetAsync(Guid apiAssetId, int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Releases
            .Where(r => r.ApiAssetId == apiAssetId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Conta o total de releases de um ativo de API.</summary>
    public async Task<int> CountByApiAssetAsync(Guid apiAssetId, CancellationToken cancellationToken = default)
        => await context.Releases
            .CountAsync(r => r.ApiAssetId == apiAssetId, cancellationToken);
}
