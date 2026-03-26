using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de metadados de Background Service para drafts de contrato em edição no Contract Studio.
/// Persiste e consulta BackgroundServiceDraftMetadata vinculados a ContractDraft
/// com ContractType = BackgroundService.
/// </summary>
internal sealed class BackgroundServiceDraftMetadataRepository(ContractsDbContext context)
    : RepositoryBase<BackgroundServiceDraftMetadata, BackgroundServiceDraftMetadataId>(context), IBackgroundServiceDraftMetadataRepository
{
    /// <summary>Busca o BackgroundServiceDraftMetadata associado a um draft de contrato.</summary>
    public async Task<BackgroundServiceDraftMetadata?> GetByDraftIdAsync(
        ContractDraftId draftId,
        CancellationToken ct = default)
        => await context.BackgroundServiceDraftMetadata
            .SingleOrDefaultAsync(m => m.ContractDraftId == draftId, ct);
}
