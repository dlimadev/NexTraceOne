using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para scores de saúde de contratos.
/// </summary>
public interface IContractHealthScoreRepository
{
    /// <summary>Obtém o score de saúde mais recente de um contrato.</summary>
    Task<ContractHealthScore?> GetByApiAssetIdAsync(Guid apiAssetId, CancellationToken cancellationToken);

    /// <summary>Lista contratos com score abaixo de um threshold.</summary>
    Task<IReadOnlyList<ContractHealthScore>> ListBelowThresholdAsync(int threshold, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo score de saúde.</summary>
    Task AddAsync(ContractHealthScore score, CancellationToken cancellationToken);

    /// <summary>Atualiza um score de saúde existente.</summary>
    Task UpdateAsync(ContractHealthScore score, CancellationToken cancellationToken);
}
