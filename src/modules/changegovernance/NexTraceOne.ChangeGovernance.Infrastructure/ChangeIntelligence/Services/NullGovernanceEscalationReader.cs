using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IGovernanceEscalationReader"/>.
/// Retorna <see cref="IGovernanceEscalationReader.GovernanceEscalationData"/> com listas vazias
/// e <c>PreviousPeriodBreakGlassCount</c> nulo quando o bridge não está configurado.
///
/// Wave AP.3 — GetGovernanceEscalationReport (ChangeGovernance Compliance).
/// </summary>
internal sealed class NullGovernanceEscalationReader : IGovernanceEscalationReader
{
    /// <inheritdoc/>
    public Task<IGovernanceEscalationReader.GovernanceEscalationData> GetByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult(new IGovernanceEscalationReader.GovernanceEscalationData(
            BreakGlassEvents: [],
            JitAccessRequests: [],
            PreviousPeriodBreakGlassCount: null));
}
