namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Interface de superfície para consumo contextual de releases pela IA.
///
/// Expõe consultas de releases por tenant e ambiente para:
/// - análise de frequência e risco de mudanças por ambiente
/// - correlação entre releases e incidentes
/// - análise de readiness para promoção de um ambiente para outro
/// - detecção de regressão entre versões de um serviço
///
/// Toda consulta é isolada por TenantId.
/// </summary>
public interface IReleaseContextSurface
{
    /// <summary>
    /// Lista releases de um serviço em um ambiente específico de um tenant.
    /// Base para análise de risco de promoção pela IA.
    /// </summary>
    Task<IReadOnlyList<ReleaseContextEntry>> ListByContextAsync(
        Guid tenantId,
        Guid? environmentId,
        string? serviceName,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista releases recentes em ambientes não produtivos de um tenant.
    /// Usado para análise comparativa antes de promoção para produção.
    /// </summary>
    Task<IReadOnlyList<ReleaseContextEntry>> ListNonProductionReleasesAsync(
        Guid tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}

/// <summary>Entrada resumida de release para consulta pela IA.</summary>
public sealed record ReleaseContextEntry(
    Guid ReleaseId,
    string ServiceName,
    string Version,
    string Environment,
    Guid? TenantId,
    Guid? EnvironmentId,
    string Status,
    decimal ChangeScore,
    DateTimeOffset CreatedAt);
