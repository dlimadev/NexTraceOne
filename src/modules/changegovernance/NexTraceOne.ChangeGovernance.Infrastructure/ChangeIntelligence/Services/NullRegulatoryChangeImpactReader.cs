using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IRegulatoryChangeImpactReader"/>.
/// Retorna listas vazias e score 0 até a infraestrutura real ser ligada.
/// Wave BB.3 — GetRegulatoryChangeImpactReport.
/// </summary>
internal sealed class NullRegulatoryChangeImpactReader : IRegulatoryChangeImpactReader
{
    public Task<IReadOnlyList<IRegulatoryChangeImpactReader.ServiceRegulatoryImpactEntry>> ListImpactedServicesAsync(
        string tenantId, string standardId, string newControlId, string scope, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IRegulatoryChangeImpactReader.ServiceRegulatoryImpactEntry>>([]);

    public Task<decimal> GetTenantRegulatoryReadinessScoreAsync(string tenantId, CancellationToken ct)
        => Task.FromResult(100m);
}
