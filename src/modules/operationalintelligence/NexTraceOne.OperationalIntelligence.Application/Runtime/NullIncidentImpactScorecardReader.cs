using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime;

/// <summary>
/// Implementação null (honest-null) de IIncidentImpactScorecardReader.
/// Retorna lista vazia — sem dados de incidentes disponíveis.
/// Wave AN.2 — GetIncidentImpactScorecardReport.
/// </summary>
public sealed class NullIncidentImpactScorecardReader : IIncidentImpactScorecardReader
{
    public Task<IReadOnlyList<IIncidentImpactScorecardReader.IncidentEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IIncidentImpactScorecardReader.IncidentEntry>>([]);
}
