using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de detalhes de Background Service Contracts publicados.
/// </summary>
public interface IBackgroundServiceContractDetailRepository
{
    /// <summary>Adiciona um novo BackgroundServiceContractDetail ao repositório.</summary>
    void Add(BackgroundServiceContractDetail detail);

    /// <summary>Busca o BackgroundServiceContractDetail pelo seu identificador único.</summary>
    Task<BackgroundServiceContractDetail?> GetByIdAsync(BackgroundServiceContractDetailId id, CancellationToken ct = default);

    /// <summary>Busca o BackgroundServiceContractDetail associado a uma versão de contrato.</summary>
    Task<BackgroundServiceContractDetail?> GetByContractVersionIdAsync(ContractVersionId contractVersionId, CancellationToken ct = default);
}
