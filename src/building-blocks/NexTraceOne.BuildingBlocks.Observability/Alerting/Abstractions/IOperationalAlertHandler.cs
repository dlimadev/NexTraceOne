using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;

/// <summary>
/// Handler para alertas operacionais que integra alerting ao fluxo de operação/incidentes.
/// Implementações deste handler são invocadas pelo AlertGateway após o dispatch para canais,
/// permitindo que alertas de severidade Error ou Critical criem/enriqueçam incidentes.
/// </summary>
public interface IOperationalAlertHandler
{
    /// <summary>
    /// Processa um alerta operacional, potencialmente criando ou enriquecendo incidentes.
    /// </summary>
    Task HandleAlertAsync(AlertPayload payload, AlertDispatchResult dispatchResult, CancellationToken cancellationToken);
}
