using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de detalhes de Background Service Contracts publicados.
/// Persiste e consulta BackgroundServiceContractDetail vinculados a ContractVersion
/// com ContractType = BackgroundService.
/// </summary>
internal sealed class BackgroundServiceContractDetailRepository(ContractsDbContext context)
    : RepositoryBase<BackgroundServiceContractDetail, BackgroundServiceContractDetailId>(context), IBackgroundServiceContractDetailRepository
{
    /// <summary>Busca o BackgroundServiceContractDetail associado a uma versão de contrato.</summary>
    public async Task<BackgroundServiceContractDetail?> GetByContractVersionIdAsync(
        ContractVersionId contractVersionId,
        CancellationToken ct = default)
        => await context.BackgroundServiceContractDetails
            .SingleOrDefaultAsync(d => d.ContractVersionId == contractVersionId, ct);
}
