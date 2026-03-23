using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de suppressão de notificações.
/// Avalia se uma notificação deve ser suprimida antes de ser criada/entregue.
/// </summary>
public interface INotificationSuppressionService
{
    /// <summary>
    /// Avalia se um pedido de notificação deve ser suprimido.
    /// </summary>
    /// <param name="request">O pedido de notificação.</param>
    /// <param name="recipientUserId">Id do destinatário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da avaliação: se deve suprimir e a razão.</returns>
    Task<SuppressionResult> EvaluateAsync(
        NotificationRequest request,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado da avaliação de supressão.
/// </summary>
public sealed record SuppressionResult(bool ShouldSuppress, string? Reason = null)
{
    public static SuppressionResult Allow() => new(false);
    public static SuppressionResult SuppressWith(string reason) => new(true, reason);
}
