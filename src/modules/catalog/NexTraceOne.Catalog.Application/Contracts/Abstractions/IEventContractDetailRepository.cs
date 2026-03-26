using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de detalhes AsyncAPI de versões de contrato publicadas.
/// </summary>
public interface IEventContractDetailRepository
{
    /// <summary>Adiciona um novo EventContractDetail ao repositório.</summary>
    void Add(EventContractDetail detail);

    /// <summary>Busca o EventContractDetail pelo seu identificador único.</summary>
    Task<EventContractDetail?> GetByIdAsync(EventContractDetailId id, CancellationToken ct = default);

    /// <summary>Busca o EventContractDetail associado a uma versão de contrato.</summary>
    Task<EventContractDetail?> GetByContractVersionIdAsync(ContractVersionId contractVersionId, CancellationToken ct = default);
}
