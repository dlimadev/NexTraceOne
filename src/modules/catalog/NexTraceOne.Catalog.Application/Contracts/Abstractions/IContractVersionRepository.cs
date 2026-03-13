using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Application.Abstractions;

/// <summary>
/// Abstração do repositório de versões de contrato.
/// </summary>
public interface IContractVersionRepository
{
    /// <summary>Adiciona uma nova versão de contrato ao repositório.</summary>
    void Add(ContractVersion version);

    /// <summary>Busca uma versão de contrato pelo seu identificador.</summary>
    Task<ContractVersion?> GetByIdAsync(ContractVersionId id, CancellationToken ct = default);

    /// <summary>Busca uma versão de contrato pelo ativo de API e versão semântica.</summary>
    Task<ContractVersion?> GetByApiAssetAndSemVerAsync(Guid apiAssetId, string semVer, CancellationToken ct = default);

    /// <summary>Lista todas as versões de contrato de um ativo de API.</summary>
    Task<IReadOnlyList<ContractVersion>> ListByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>Retorna a versão de contrato mais recente de um ativo de API.</summary>
    Task<ContractVersion?> GetLatestByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>
    /// Pesquisa versões de contrato com filtros opcionais e paginação.
    /// Retorna os itens da página solicitada e o total de registros que atendem aos filtros.
    /// </summary>
    Task<(IReadOnlyList<ContractVersion> Items, int TotalCount)> SearchAsync(
        ContractProtocol? protocol,
        ContractLifecycleState? lifecycleState,
        Guid? apiAssetId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
