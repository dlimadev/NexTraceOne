using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime;

/// <summary>
/// Implementação null (honest-null) de ISreMaturityReader.
/// Retorna lista vazia — sem dados de maturidade SRE disponíveis.
/// Wave AN.3 — GetSreMaturityIndexReport.
/// </summary>
public sealed class NullSreMaturityReader : ISreMaturityReader
{
    public Task<IReadOnlyList<ISreMaturityReader.TeamSreDataEntry>> ListByTenantAsync(
        string tenantId, int chaosLookbackMonths, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISreMaturityReader.TeamSreDataEntry>>([]);
}
