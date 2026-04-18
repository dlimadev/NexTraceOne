using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de deployments de versões de contrato.
/// Permite registar e consultar eventos de deployment por versão e ambiente.
/// </summary>
public interface IContractDeploymentRepository
{
    /// <summary>Busca um deployment pelo seu identificador.</summary>
    Task<ContractDeployment?> GetByIdAsync(ContractDeploymentId id, CancellationToken ct = default);

    /// <summary>Lista todos os deployments de uma versão de contrato, ordenados do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<ContractDeployment>> ListByContractVersionAsync(ContractVersionId contractVersionId, CancellationToken ct = default);

    /// <summary>
    /// Lista o deployment mais recente bem-sucedido (Success) por ambiente para um ativo de API.
    /// Usado para detectar drift de contrato entre ambientes (ex: staging vs produção).
    /// </summary>
    Task<IReadOnlyDictionary<string, ContractDeployment>> GetLatestSuccessfulByEnvironmentAsync(
        Guid apiAssetId, CancellationToken ct = default);

    /// <summary>Adiciona um novo deployment ao repositório.</summary>
    void Add(ContractDeployment deployment);
}
