using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IFeatureFlagRiskReader.
/// Retorna lista vazia — sem dados de risco de feature flags disponíveis.
/// Wave AS.2 — GetFeatureFlagRiskReport.
/// </summary>
public sealed class NullFeatureFlagRiskReader : IFeatureFlagRiskReader
{
    public Task<IReadOnlyList<IFeatureFlagRiskReader.FlagRiskEntry>> ListFlagRiskByTenantAsync(
        string tenantId,
        int staleFlagDays,
        int prodPresenceDays,
        int incidentWindowHours,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IFeatureFlagRiskReader.FlagRiskEntry>>([]);
}
