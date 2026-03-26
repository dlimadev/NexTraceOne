using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de metadados AsyncAPI para drafts de contrato de evento em edição no Contract Studio.
/// Persiste e consulta EventDraftMetadata vinculados a ContractDraft com ContractType = Event.
/// </summary>
internal sealed class EventDraftMetadataRepository(ContractsDbContext context)
    : RepositoryBase<EventDraftMetadata, EventDraftMetadataId>(context), IEventDraftMetadataRepository
{
    /// <summary>Busca o EventDraftMetadata associado a um draft de contrato.</summary>
    public async Task<EventDraftMetadata?> GetByDraftIdAsync(
        ContractDraftId draftId,
        CancellationToken ct = default)
        => await context.EventDraftMetadata
            .SingleOrDefaultAsync(m => m.ContractDraftId == draftId, ct);
}
