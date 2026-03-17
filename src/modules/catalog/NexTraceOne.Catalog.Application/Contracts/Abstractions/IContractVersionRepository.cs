using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

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

    /// <summary>
    /// Lista versões de contrato mais recentes para cada ApiAssetId distinto,
    /// com filtros opcionais por protocolo, ciclo de vida e paginação.
    /// Usado para a visão de catálogo de contratos.
    /// </summary>
    Task<(IReadOnlyList<ContractVersion> Items, int TotalCount)> ListLatestPerApiAssetAsync(
        ContractProtocol? protocol,
        ContractLifecycleState? lifecycleState,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista versões de contrato para um conjunto de ApiAssetIds.
    /// Usado para obter contratos relacionados a um serviço específico.
    /// </summary>
    Task<IReadOnlyList<ContractVersion>> ListByApiAssetIdsAsync(
        IEnumerable<Guid> apiAssetIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém contagens agregadas de contratos por protocolo e ciclo de vida.
    /// Usado para o dashboard de governança de contratos.
    /// </summary>
    Task<ContractSummaryData> GetSummaryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Dados agregados de resumo de contratos para o dashboard de governança.
/// </summary>
public sealed record ContractSummaryData(
    int TotalVersions,
    int DistinctContracts,
    int DraftCount,
    int InReviewCount,
    int ApprovedCount,
    int LockedCount,
    int DeprecatedCount,
    IReadOnlyList<ProtocolCount> ByProtocol);

/// <summary>Contagem agrupada por protocolo.</summary>
public sealed record ProtocolCount(string Protocol, int Count);
