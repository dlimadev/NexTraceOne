namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de risco da cadeia de fornecimento.
/// Cruza componentes vulneráveis do SBOM com o grafo de dependências de serviços.
/// Por omissão satisfeita por <c>NullSupplyChainRiskReader</c> (honest-null).
/// Wave AO.3 — GetSupplyChainRiskReport.
/// </summary>
public interface ISupplyChainRiskReader
{
    Task<IReadOnlyList<VulnerableComponentEntry>> ListVulnerableComponentsByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de componente vulnerável com serviços afectados.</summary>
    public sealed record VulnerableComponentEntry(
        string ComponentName,
        string ComponentVersion,
        string HighestCveSeverity,
        int CveCount,
        DateTimeOffset? CvePublishedAt,
        string? FixVersion,
        IReadOnlyList<string> DirectlyAffectedServiceIds,
        IReadOnlyList<string> TransitivelyAffectedServiceIds,
        bool HasCustomerFacingExposed,
        int TotalServicesInTenant);
}
