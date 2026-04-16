namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Port de saída para envio de webhooks outbound de pedidos de aprovação a sistemas externos.
/// Implementado na camada Infrastructure via HttpClient.
/// </summary>
public interface IExternalApprovalWebhookSender
{
    /// <summary>
    /// Envia um pedido de aprovação outbound para um sistema externo.
    /// </summary>
    /// <param name="webhookUrl">URL do sistema externo que receberá o pedido.</param>
    /// <param name="payload">Payload do pedido de aprovação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>true se o envio foi bem-sucedido (2xx); false em caso de falha.</returns>
    Task<bool> SendAsync(string webhookUrl, ApprovalWebhookPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payload enviado para o sistema externo no pedido de aprovação outbound.
/// Segue o padrão de webhook com callbackUrl para receber a decisão.
/// </summary>
public sealed record ApprovalWebhookPayload(
    string CallbackUrl,
    string ApprovalRequestId,
    string ReleaseId,
    string ServiceName,
    string Version,
    string TargetEnvironment,
    double RiskScore,
    string ImpactSummary,
    DateTimeOffset ExpiresAt);
