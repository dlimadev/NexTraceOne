namespace NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;

/// <summary>
/// Abstracção para leitura de dados brutos do Platform Health Index.
/// Por omissão satisfeita por <c>NullPlatformHealthIndexReader</c> (honest-null).
/// Wave AU.2 — GetPlatformHealthIndexReport.
/// </summary>
public interface IPlatformHealthIndexReader
{
    /// <summary>Retorna dados brutos das dimensões do Platform Health Index.</summary>
    Task<PlatformHealthRawData> GetPlatformHealthDataAsync(string tenantId, DateTimeOffset since, CancellationToken ct);

    /// <summary>Retorna série temporal mensal do Platform Health Index (últimos N meses).</summary>
    Task<IReadOnlyList<PlatformHealthTimelinePoint>> GetTimelineAsync(string tenantId, int months, CancellationToken ct);

    /// <summary>Dados brutos das 7 dimensões do Platform Health Index.</summary>
    public sealed record PlatformHealthRawData(
        decimal ServiceCatalogCompleteness,
        decimal ContractCoverage,
        decimal ChangeGovernanceAdoption,
        decimal SloGovernanceAdoption,
        decimal ObservabilityContextualization,
        decimal AiGovernanceReadiness,
        decimal DataFreshness,
        decimal? BenchmarkPercentile);

    /// <summary>Ponto da série temporal mensal do Platform Health Index.</summary>
    public sealed record PlatformHealthTimelinePoint(DateTimeOffset Month, decimal PlatformHealthIndex);
}
