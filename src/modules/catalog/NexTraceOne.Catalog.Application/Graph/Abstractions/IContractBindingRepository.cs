using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de vínculos de contrato do módulo Catalog Graph.
/// Suporta listagem por interface e gestão do ciclo de vida dos vínculos.
/// </summary>
public interface IContractBindingRepository
{
    /// <summary>Obtém um vínculo de contrato pelo identificador.</summary>
    Task<ContractBinding?> GetByIdAsync(ContractBindingId id, CancellationToken ct);

    /// <summary>Lista todos os vínculos de contrato de uma interface específica.</summary>
    Task<IReadOnlyList<ContractBinding>> ListByInterfaceAsync(Guid serviceInterfaceId, CancellationToken ct);

    /// <summary>Adiciona um novo vínculo de contrato para persistência.</summary>
    void Add(ContractBinding entity);
}
