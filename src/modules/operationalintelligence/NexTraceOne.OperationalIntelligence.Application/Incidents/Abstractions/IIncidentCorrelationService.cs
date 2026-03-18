using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Serviço de aplicação que recalcula correlação incidente↔change com critérios reais e auditáveis.
/// </summary>
public interface IIncidentCorrelationService
{
    /// <summary>
    /// Recalcula e persiste a correlação de um incidente.
    /// Retorna null quando o incidente não existe.
    /// </summary>
    Task<GetIncidentCorrelation.Response?> RecomputeAsync(string incidentId, CancellationToken cancellationToken);
}
