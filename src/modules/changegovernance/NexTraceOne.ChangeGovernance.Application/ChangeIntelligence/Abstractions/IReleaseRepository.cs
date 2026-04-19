using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para a entidade Release.</summary>
public interface IReleaseRepository
{
    /// <summary>Busca uma Release pelo seu identificador.</summary>
    Task<Release?> GetByIdAsync(ReleaseId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma release pela chave natural do sistema de origem externo.
    /// Permite que consumidores externos (Jenkins, GitHub, Azure DevOps) consultem
    /// uma release pelo seu próprio ID sem precisar conhecer o GUID interno do NexTraceOne.
    /// </summary>
    Task<Release?> GetByExternalKeyAsync(string externalReleaseId, string externalSystem, CancellationToken cancellationToken = default);

    /// <summary>Busca releases de um ativo de API por versão.</summary>
    Task<Release?> GetByApiAssetAndVersionAsync(Guid apiAssetId, string version, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma release existente pelo nome do serviço, versão e ambiente.
    /// Usado na correlação de eventos de deploy externos que não têm ApiAssetId.
    /// </summary>
    Task<Release?> GetByServiceNameVersionEnvironmentAsync(string serviceName, string version, string environment, CancellationToken cancellationToken = default);

    /// <summary>Lista releases de um ativo de API ordenadas por data de criação descendente.</summary>
    Task<IReadOnlyList<Release>> ListByApiAssetAsync(Guid apiAssetId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Conta o total de releases de um ativo de API.</summary>
    Task<int> CountByApiAssetAsync(Guid apiAssetId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova Release ao repositório.</summary>
    void Add(Release release);

    /// <summary>Lista mudanças com filtros avançados para o catálogo de changes.</summary>
    Task<IReadOnlyList<Release>> ListFilteredAsync(
        Guid tenantId,
        string? serviceName,
        string? teamName,
        string? environment,
        ChangeType? changeType,
        ConfidenceStatus? confidenceStatus,
        DeploymentStatus? deploymentStatus,
        string? searchTerm,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Conta mudanças com filtros avançados.</summary>
    Task<int> CountFilteredAsync(
        Guid tenantId,
        string? serviceName,
        string? teamName,
        string? environment,
        ChangeType? changeType,
        ConfidenceStatus? confidenceStatus,
        DeploymentStatus? deploymentStatus,
        string? searchTerm,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    /// <summary>Lista releases por nome de serviço.</summary>
    Task<IReadOnlyList<Release>> ListByServiceNameAsync(
        string serviceName,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Conta releases por nome de serviço.</summary>
    Task<int> CountByServiceNameAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>Lista releases numa janela temporal, com filtro opcional de ambiente e isolamento por tenant.</summary>
    Task<IReadOnlyList<Release>> ListInRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém contadores agregados de mudanças.</summary>
    Task<(int total, int validated, int needsAttention, int suspectedRegressions, int correlatedWithIncidents)>
        GetSummaryCountsAsync(
            Guid tenantId,
            string? teamName,
            string? environment,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista releases históricas similares à release alvo para cálculo de padrão histórico.
    /// Filtra por mesmo nome de serviço, ambiente e nível de mudança dentro de uma janela temporal.
    /// Exclui a própria release-alvo do resultado.
    /// </summary>
    Task<IReadOnlyList<Release>> ListSimilarReleasesAsync(
        ReleaseId excludeReleaseId,
        string serviceName,
        string environment,
        ChangeLevel changeLevel,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxResults,
        CancellationToken cancellationToken = default);
}
