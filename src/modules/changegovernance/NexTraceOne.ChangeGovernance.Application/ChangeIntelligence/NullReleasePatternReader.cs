using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;

/// <summary>
/// Implementação null (honest-null) de IReleasePatternReader.
/// Retorna lista vazia — sem dados de padrões de release disponíveis.
/// Wave AW.1 — GetReleasePatternAnalysisReport.
/// </summary>
public sealed class NullReleasePatternReader : IReleasePatternReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ReleasePatternEntry>> ListReleasesByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ReleasePatternEntry>>([]);
}
