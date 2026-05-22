namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de métricas de qualidade de código por tenant.
///
/// Agrega os últimos registos de análise por serviço para alimentar relatórios de portfólio,
/// scores de completude e dashboards de engenharia.
///
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public interface ICodeQualityReader
{
    /// <summary>
    /// Lista o resultado de análise mais recente por serviço para o tenant.
    /// </summary>
    Task<IReadOnlyList<CodeQualityEntry>> ListLatestByTenantAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Entrada agregada de qualidade de código para um serviço.
/// Wave AQ.2.
/// </summary>
public sealed record CodeQualityEntry(
    string ServiceId,
    string ServiceName,
    string ProjectKey,
    string QualityGateStatus,
    double Coverage,
    int Bugs,
    int Vulnerabilities,
    int CodeSmells,
    double DuplicatedLinesDensity,
    string? Branch,
    DateTimeOffset AnalyzedAt,
    bool QualityGatePassed);

/// <summary>
/// Implementação nula de <see cref="ICodeQualityReader"/> — retorna lista vazia.
/// Substituída por implementação EF Core quando Wave AQ.2 estiver ativa.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public sealed class NullCodeQualityReader : ICodeQualityReader
{
    public Task<IReadOnlyList<CodeQualityEntry>> ListLatestByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<CodeQualityEntry>>([]);
}
