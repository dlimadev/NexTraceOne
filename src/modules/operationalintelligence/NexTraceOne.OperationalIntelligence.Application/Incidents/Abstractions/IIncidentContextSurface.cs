using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Interface de superfície para consumo contextual de incidentes pela IA.
///
/// Expõe consultas de incidentes filtradas por tenant e ambiente para:
/// - análise de padrões de incidentes em ambientes não produtivos
/// - correlação de incidentes com mudanças por ambiente
/// - comparação de comportamento entre ambientes do mesmo tenant
/// - detecção de riscos antes de promoção para produção
///
/// Toda consulta é isolada por TenantId — cross-tenant é impossível por design.
/// </summary>
public interface IIncidentContextSurface
{
    /// <summary>
    /// Lista incidentes de um ambiente específico de um tenant.
    /// Base para análise de padrões e risco de promoção pela IA.
    /// </summary>
    Task<IReadOnlyList<ListIncidents.IncidentListItem>> ListByContextAsync(
        Guid tenantId,
        Guid? environmentId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta incidentes por severidade em um ambiente, para scoring de readiness.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetSeverityCountByContextAsync(
        Guid tenantId,
        Guid? environmentId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista incidentes recentes em ambientes não produtivos de um tenant.
    /// Usado pela IA para detectar sinais de risco antes de promoção para produção.
    /// </summary>
    Task<IReadOnlyList<ListIncidents.IncidentListItem>> ListNonProductionSignalsAsync(
        Guid tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
