using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração do gateway de pagamento para assinaturas SaaS.
/// Implementada na Infrastructure (Stripe via REST + webhook assinado).
/// </summary>
public interface IBillingGateway
{
    /// <summary>
    /// Cria uma sessão de checkout para upgrade de plano e retorna a URL
    /// de pagamento hospedada pelo gateway.
    /// </summary>
    Task<Result<string>> CreateCheckoutSessionUrlAsync(
        Guid tenantId,
        string plan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica a assinatura HMAC do payload de webhook recebido do gateway.
    /// Retorna false para assinaturas inválidas ou fora da janela de tolerância.
    /// </summary>
    bool VerifyWebhookSignature(string payload, string signatureHeader);
}
