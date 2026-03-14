using NexTraceOne.BuildingBlocks.Domain.Notifications;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Notifications;

/// <summary>
/// Adapter de canal de notificação.
/// Cada implementação é responsável por enviar notificações através de um canal específico
/// (Email, Teams, WhatsApp, SMS, InApp, Push).
/// O orquestrador seleciona o adapter apropriado com base nas preferências do usuário
/// e nos canais suportados pelo template.
/// </summary>
public interface INotificationChannelAdapter
{
    /// <summary>
    /// Canal que este adapter suporta.
    /// Usado pelo orquestrador para selecionar o adapter correto.
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Indica se o adapter está configurado e operacional.
    /// O orquestrador pode usar esta propriedade para decidir fallback.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Envia uma notificação pelo canal específico deste adapter.
    /// </summary>
    /// <param name="delivery">Registro de entrega com dados do destinatário e canal.</param>
    /// <param name="renderedNotification">Conteúdo renderizado (subject e body).</param>
    /// <param name="request">Requisição original com metadados da notificação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// Id externo da mensagem no provedor (ex: message-id do email) em caso de sucesso,
    /// ou erro de envio com detalhes técnicos.
    /// </returns>
    Task<Result<string?>> SendAsync(
        NotificationDelivery delivery,
        RenderedNotification renderedNotification,
        NotificationRequest request,
        CancellationToken ct = default);
}
