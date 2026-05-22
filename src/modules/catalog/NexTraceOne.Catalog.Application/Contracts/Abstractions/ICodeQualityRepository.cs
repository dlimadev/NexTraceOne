namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório de CodeQualityRecord — regista resultados de análise de qualidade de código
/// provenientes de ferramentas como SonarQube para cada serviço.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public interface ICodeQualityRepository
{
    Task AddAsync(CodeQualityRecord record, CancellationToken ct);

    Task<CodeQualityRecord?> GetLatestAsync(string serviceId, string tenantId, CancellationToken ct);

    Task<IReadOnlyList<CodeQualityRecord>> ListByTenantAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Resultado de uma análise de qualidade de código para um serviço.
/// Captura métricas SonarQube (ou compatíveis): quality gate, cobertura, dívida técnica.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public sealed record CodeQualityRecord(
    Guid Id,
    string TenantId,
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
    DateTimeOffset AnalyzedAt);
