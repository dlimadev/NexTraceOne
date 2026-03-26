using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de metadados de Background Service para drafts em edição.
/// </summary>
public interface IBackgroundServiceDraftMetadataRepository
{
    /// <summary>Adiciona um novo BackgroundServiceDraftMetadata ao repositório.</summary>
    void Add(BackgroundServiceDraftMetadata metadata);

    /// <summary>Busca o BackgroundServiceDraftMetadata pelo seu identificador único.</summary>
    Task<BackgroundServiceDraftMetadata?> GetByIdAsync(BackgroundServiceDraftMetadataId id, CancellationToken ct = default);

    /// <summary>Busca o BackgroundServiceDraftMetadata associado a um draft de contrato.</summary>
    Task<BackgroundServiceDraftMetadata?> GetByDraftIdAsync(ContractDraftId draftId, CancellationToken ct = default);
}
