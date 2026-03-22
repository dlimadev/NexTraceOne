using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;

/// <summary>
/// Gateway central de alertas da plataforma.
/// Despacha alertas para todos os canais registados ou para um canal específico.
/// </summary>
public interface IAlertGateway
{
    /// <summary>
    /// Despacha o alerta para todos os canais registados (fan-out).
    /// Falhas individuais de canal não bloqueiam os restantes.
    /// </summary>
    Task<AlertDispatchResult> DispatchAsync(
        AlertPayload payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Despacha o alerta para um canal específico identificado pelo nome.
    /// </summary>
    Task<AlertDispatchResult> DispatchAsync(
        AlertPayload payload,
        string channelName,
        CancellationToken cancellationToken = default);
}
