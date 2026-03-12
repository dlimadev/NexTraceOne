using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de versões de contrato OpenAPI, implementando consultas específicas de negócio.
/// </summary>
internal sealed class ContractVersionRepository(ContractsDbContext context)
    : RepositoryBase<ContractVersion, ContractVersionId>(context), IContractVersionRepository
{
    /// <summary>Busca uma versão de contrato pelo Id, incluindo os diffs associados.</summary>
    public override async Task<ContractVersion?> GetByIdAsync(ContractVersionId id, CancellationToken ct = default)
        => await context.ContractVersions
            .Include(v => v.Diffs)
            .SingleOrDefaultAsync(v => v.Id == id, ct);

    /// <summary>Busca uma versão de contrato pelo ativo de API e versão semântica.</summary>
    public async Task<ContractVersion?> GetByApiAssetAndSemVerAsync(Guid apiAssetId, string semVer, CancellationToken ct = default)
        => await context.ContractVersions
            .Include(v => v.Diffs)
            .SingleOrDefaultAsync(v => v.ApiAssetId == apiAssetId && v.SemVer == semVer, ct);

    /// <summary>Lista todas as versões de contrato de um ativo de API, ordenadas por data de criação.</summary>
    public async Task<IReadOnlyList<ContractVersion>> ListByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ContractVersions
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Retorna a versão de contrato mais recente de um ativo de API.</summary>
    public async Task<ContractVersion?> GetLatestByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ContractVersions
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);
}
