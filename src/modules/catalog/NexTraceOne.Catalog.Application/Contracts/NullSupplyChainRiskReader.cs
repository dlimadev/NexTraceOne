using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de ISupplyChainRiskReader.
/// Retorna lista vazia — sem dados de componentes vulneráveis disponíveis.
/// Wave AO.3 — GetSupplyChainRiskReport.
/// </summary>
public sealed class NullSupplyChainRiskReader : ISupplyChainRiskReader
{
    public Task<IReadOnlyList<ISupplyChainRiskReader.VulnerableComponentEntry>> ListVulnerableComponentsByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISupplyChainRiskReader.VulnerableComponentEntry>>([]);
}
