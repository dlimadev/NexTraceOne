using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de metadados AsyncAPI para drafts de contrato em edição.
/// </summary>
public interface IEventDraftMetadataRepository
{
    /// <summary>Adiciona um novo EventDraftMetadata ao repositório.</summary>
    void Add(EventDraftMetadata metadata);

    /// <summary>Busca o EventDraftMetadata pelo seu identificador único.</summary>
    Task<EventDraftMetadata?> GetByIdAsync(EventDraftMetadataId id, CancellationToken ct = default);

    /// <summary>Busca o EventDraftMetadata associado a um draft de contrato.</summary>
    Task<EventDraftMetadata?> GetByDraftIdAsync(ContractDraftId draftId, CancellationToken ct = default);
}
