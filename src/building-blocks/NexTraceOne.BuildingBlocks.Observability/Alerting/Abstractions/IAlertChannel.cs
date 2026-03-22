using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;

/// <summary>
/// Contrato para um canal de envio de alertas operacionais.
/// Cada implementação representa um meio de entrega (webhook, email, etc.).
/// </summary>
public interface IAlertChannel
{
    /// <summary>Nome identificador do canal (ex: "Webhook", "Email").</summary>
    string ChannelName { get; }

    /// <summary>
    /// Envia o alerta pelo canal.
    /// Retorna true se o envio foi bem-sucedido, false caso contrário.
    /// </summary>
    Task<bool> SendAsync(AlertPayload payload, CancellationToken cancellationToken = default);
}
