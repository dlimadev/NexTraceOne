using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Maturity;

/// <summary>
/// Resultado imutável do cálculo de maturidade de um serviço.
/// Contém o nível, score geral e todos os sinais booleanos usados no scorecard.
/// </summary>
public sealed record ServiceMaturityResult(
    string Level,
    decimal OverallScore,
    bool HasOwnership,
    bool HasContracts,
    bool HasDocumentation,
    bool HasRunbook,
    bool HasMonitoring,
    bool HasRepository,
    int ApiCount,
    int ContractCount,
    int LinkCount);

/// <summary>
/// Contrato do calculador de maturidade de serviços.
/// Extraído do dashboard para permitir reutilização na feature ListServices.
/// </summary>
public interface IServiceMaturityCalculator
{
    /// <summary>
    /// Calcula a maturidade de um único serviço com dados já carregados em memória.
    /// Função pura — não executa queries.
    /// </summary>
    ServiceMaturityResult Compute(
        ServiceAsset service,
        IReadOnlyList<ServiceLink> links,
        IReadOnlyList<ApiAsset> apis,
        int contractCount);

    /// <summary>
    /// Calcula a maturidade para uma lista de serviços usando 3 queries batch.
    /// Elimina o padrão N+1 do dashboard original.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ServiceMaturityResult>> ComputeForServicesAsync(
        IReadOnlyList<ServiceAsset> services,
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementação do calculador de maturidade de serviços do catálogo.
/// Lógica extraída de GetServiceMaturityDashboard; dimensões e pesos preservados.
/// </summary>
public sealed class ServiceMaturityCalculator(
    IServiceLinkRepository serviceLinkRepository,
    IApiAssetRepository apiAssetRepository,
    IContractVersionRepository contractVersionRepository) : IServiceMaturityCalculator
{
    /// <inheritdoc/>
    public ServiceMaturityResult Compute(
        ServiceAsset service,
        IReadOnlyList<ServiceLink> links,
        IReadOnlyList<ApiAsset> apis,
        int contractCount)
    {
        // ── Sinais booleanos ──────────────────────────────────────────────────
        var hasOwnership = !string.IsNullOrWhiteSpace(service.TeamName)
            && !string.IsNullOrWhiteSpace(service.TechnicalOwner);
        var hasContracts = contractCount > 0;
        var hasDocumentation = !string.IsNullOrWhiteSpace(service.DocumentationUrl)
            || links.Any(l => l.Category is LinkCategory.Documentation or LinkCategory.Wiki);
        var hasRunbook = links.Any(l => l.Category == LinkCategory.Runbook);
        var hasMonitoring = links.Any(l =>
            l.Category is LinkCategory.Monitoring or LinkCategory.Dashboard);
        var hasRepository = !string.IsNullOrWhiteSpace(service.RepositoryUrl)
            || links.Any(l => l.Category == LinkCategory.Repository);

        // ── Scores por dimensão ───────────────────────────────────────────────
        var dimensionScores = new List<decimal>(5);

        // Ownership: 1.0 se completo, 0.4 se só team, 0.0 se nenhum
        if (hasOwnership)
            dimensionScores.Add(1m);
        else
            dimensionScores.Add(string.IsNullOrWhiteSpace(service.TeamName) ? 0m : 0.4m);

        // Contratos: 1.0 se tem, 0.5 se sem APIs (não penaliza), 0.0 se APIs sem contrato
        dimensionScores.Add(hasContracts ? 1m : (apis.Count == 0 ? 0.5m : 0m));

        // Documentação: 1.0 ou 0.0
        dimensionScores.Add(hasDocumentation ? 1m : 0m);

        // Repositório: 1.0 ou 0.0
        dimensionScores.Add(hasRepository ? 1m : 0m);

        // Prontidão operacional: 1.0 se ambos, 0.5 se um, 0.0 se nenhum
        dimensionScores.Add(hasMonitoring && hasRunbook ? 1m : (hasMonitoring || hasRunbook ? 0.5m : 0m));

        var overallScore = dimensionScores.Count > 0
            ? Math.Round(dimensionScores.Average(), 2)
            : 0m;

        return new ServiceMaturityResult(
            Level: ScoreToLevel(overallScore),
            OverallScore: overallScore,
            HasOwnership: hasOwnership,
            HasContracts: hasContracts,
            HasDocumentation: hasDocumentation,
            HasRunbook: hasRunbook,
            HasMonitoring: hasMonitoring,
            HasRepository: hasRepository,
            ApiCount: apis.Count,
            ContractCount: contractCount,
            LinkCount: links.Count);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, ServiceMaturityResult>> ComputeForServicesAsync(
        IReadOnlyList<ServiceAsset> services,
        CancellationToken cancellationToken)
    {
        if (services.Count == 0)
            return new Dictionary<Guid, ServiceMaturityResult>();

        var ids = services.Select(s => s.Id).ToList();

        // Batch query 1 — todos os links dos serviços pedidos
        var allLinks = await serviceLinkRepository.ListByServiceIdsAsync(ids, cancellationToken);
        var linksByService = allLinks
            .GroupBy(l => l.ServiceAssetId.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ServiceLink>)g.ToList());

        // Batch query 2 — todas as APIs dos serviços pedidos
        var allApis = await apiAssetRepository.ListByServiceIdsAsync(ids, cancellationToken);
        var apisByService = allApis
            .GroupBy(a => a.OwnerService.Id.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApiAsset>)g.ToList());

        // Batch query 3 — contratos de todas as APIs carregadas
        var contractCountByService = new Dictionary<Guid, int>();
        var allApiIds = allApis.Select(a => a.Id.Value).ToList();

        if (allApiIds.Count > 0)
        {
            var allContracts = await contractVersionRepository.ListByApiAssetIdsAsync(
                allApiIds, cancellationToken);

            // Mapa: ApiAssetId (Guid) → ServiceAssetId (Guid)
            var apiToService = allApis.ToDictionary(a => a.Id.Value, a => a.OwnerService.Id.Value);

            foreach (var contract in allContracts)
            {
                if (apiToService.TryGetValue(contract.ApiAssetId, out var svcGuid))
                    contractCountByService[svcGuid] =
                        contractCountByService.GetValueOrDefault(svcGuid) + 1;
            }
        }

        // Cálculo por serviço usando os dados batch
        var result = new Dictionary<Guid, ServiceMaturityResult>(services.Count);
        foreach (var service in services)
        {
            var svcGuid = service.Id.Value;
            var links = linksByService.GetValueOrDefault(svcGuid, []);
            var apis = apisByService.GetValueOrDefault(svcGuid, []);
            var contractCount = contractCountByService.GetValueOrDefault(svcGuid);
            result[svcGuid] = Compute(service, links, apis, contractCount);
        }

        return result;
    }

    private static string ScoreToLevel(decimal score) =>
        score >= 0.9m ? "Optimizing"
        : score >= 0.7m ? "Managed"
        : score >= 0.5m ? "Defined"
        : score >= 0.25m ? "Developing"
        : "Initial";
}
