using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IPlatformHealthIndexReader"/>.
/// Retorna dados zerados quando o bridge com dados de saúde da plataforma não está configurado.
///
/// Wave AU.2 — GetPlatformHealthIndexReport (ChangeGovernance Platform).
/// </summary>
internal sealed class NullPlatformHealthIndexReader : IPlatformHealthIndexReader
{
    /// <inheritdoc/>
    public Task<IPlatformHealthIndexReader.PlatformHealthRawData> GetPlatformHealthDataAsync(
        string tenantId,
        DateTimeOffset since,
        CancellationToken ct)
        => Task.FromResult(new IPlatformHealthIndexReader.PlatformHealthRawData(
            ServiceCatalogCompleteness: 0m,
            ContractCoverage: 0m,
            ChangeGovernanceAdoption: 0m,
            SloGovernanceAdoption: 0m,
            ObservabilityContextualization: 0m,
            AiGovernanceReadiness: 0m,
            DataFreshness: 0m,
            BenchmarkPercentile: null));

    /// <inheritdoc/>
    public Task<IReadOnlyList<IPlatformHealthIndexReader.PlatformHealthTimelinePoint>> GetTimelineAsync(
        string tenantId,
        int months,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IPlatformHealthIndexReader.PlatformHealthTimelinePoint>>([]);
}
