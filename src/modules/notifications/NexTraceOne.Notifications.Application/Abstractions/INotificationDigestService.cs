namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de digest de notificações.
/// Gera resumos consolidados de notificações elegíveis por período.
/// </summary>
public interface INotificationDigestService
{
    /// <summary>
    /// Gera um digest para um utilizador com base nas notificações elegíveis acumuladas.
    /// </summary>
    /// <param name="recipientUserId">Id do utilizador.</param>
    /// <param name="tenantId">Id do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado do digest gerado.</returns>
    Task<DigestResult> GenerateDigestAsync(
        Guid recipientUserId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado da geração de digest.
/// </summary>
public sealed record DigestResult(
    bool Generated,
    int NotificationCount,
    string? Summary = null);
