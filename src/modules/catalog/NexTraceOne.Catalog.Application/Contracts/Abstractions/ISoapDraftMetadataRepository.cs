using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de metadados SOAP/WSDL para drafts de contrato em edição.
/// </summary>
public interface ISoapDraftMetadataRepository
{
    /// <summary>Adiciona um novo SoapDraftMetadata ao repositório.</summary>
    void Add(SoapDraftMetadata metadata);

    /// <summary>Busca o SoapDraftMetadata pelo seu identificador único.</summary>
    Task<SoapDraftMetadata?> GetByIdAsync(SoapDraftMetadataId id, CancellationToken ct = default);

    /// <summary>Busca o SoapDraftMetadata associado a um draft de contrato.</summary>
    Task<SoapDraftMetadata?> GetByDraftIdAsync(ContractDraftId draftId, CancellationToken ct = default);
}
