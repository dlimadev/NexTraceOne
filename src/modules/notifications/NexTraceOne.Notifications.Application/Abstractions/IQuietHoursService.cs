namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de quiet hours para notificações.
/// Determina se um utilizador está em quiet hours e se a entrega deve ser diferida.
/// </summary>
public interface IQuietHoursService
{
    /// <summary>
    /// Verifica se o utilizador está actualmente em quiet hours.
    /// </summary>
    /// <param name="recipientUserId">Id do utilizador.</param>
    /// <param name="isMandatory">Se true, a notificação é obrigatória e ignora quiet hours.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se a entrega deve ser diferida.</returns>
    Task<bool> ShouldDeferAsync(
        Guid recipientUserId,
        bool isMandatory,
        CancellationToken cancellationToken = default);
}
