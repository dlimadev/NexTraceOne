using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de detalhes AsyncAPI de versões de contrato publicadas.
/// Persiste e consulta EventContractDetail vinculados a ContractVersion com Protocol = AsyncApi.
/// </summary>
internal sealed class EventContractDetailRepository(ContractsDbContext context)
    : RepositoryBase<EventContractDetail, EventContractDetailId>(context), IEventContractDetailRepository
{
    /// <summary>Busca o EventContractDetail associado a uma versão de contrato.</summary>
    public async Task<EventContractDetail?> GetByContractVersionIdAsync(
        ContractVersionId contractVersionId,
        CancellationToken ct = default)
        => await context.EventContractDetails
            .SingleOrDefaultAsync(d => d.ContractVersionId == contractVersionId, ct);
}
