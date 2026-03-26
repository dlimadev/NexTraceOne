using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de detalhes SOAP/WSDL de versões de contrato publicadas.
/// Persiste e consulta SoapContractDetail vinculados a ContractVersion com Protocol = Wsdl.
/// </summary>
internal sealed class SoapContractDetailRepository(ContractsDbContext context)
    : RepositoryBase<SoapContractDetail, SoapContractDetailId>(context), ISoapContractDetailRepository
{
    /// <summary>Busca o SoapContractDetail associado a uma versão de contrato.</summary>
    public async Task<SoapContractDetail?> GetByContractVersionIdAsync(
        ContractVersionId contractVersionId,
        CancellationToken ct = default)
        => await context.SoapContractDetails
            .SingleOrDefaultAsync(d => d.ContractVersionId == contractVersionId, ct);
}
