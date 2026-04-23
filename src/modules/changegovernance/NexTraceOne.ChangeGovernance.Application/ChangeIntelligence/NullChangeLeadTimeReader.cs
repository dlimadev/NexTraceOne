using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;

/// <summary>
/// Implementação null (honest-null) de IChangeLeadTimeReader.
/// Retorna lista vazia — sem dados de lead time de mudanças disponíveis.
/// Wave AW.2 — GetChangeLeadTimeReport.
/// </summary>
public sealed class NullChangeLeadTimeReader : IChangeLeadTimeReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LeadTimeEntry>> ListReleaseLeadTimesByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<LeadTimeEntry>>([]);
}
