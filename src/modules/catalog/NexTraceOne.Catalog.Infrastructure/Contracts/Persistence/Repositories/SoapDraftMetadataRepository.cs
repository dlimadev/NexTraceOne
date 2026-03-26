using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de metadados SOAP/WSDL para drafts de contrato em edição no Contract Studio.
/// Persiste e consulta SoapDraftMetadata vinculados a ContractDraft com ContractType = Soap.
/// </summary>
internal sealed class SoapDraftMetadataRepository(ContractsDbContext context)
    : RepositoryBase<SoapDraftMetadata, SoapDraftMetadataId>(context), ISoapDraftMetadataRepository
{
    /// <summary>Busca o SoapDraftMetadata associado a um draft de contrato.</summary>
    public async Task<SoapDraftMetadata?> GetByDraftIdAsync(
        ContractDraftId draftId,
        CancellationToken ct = default)
        => await context.SoapDraftMetadata
            .SingleOrDefaultAsync(m => m.ContractDraftId == draftId, ct);
}
