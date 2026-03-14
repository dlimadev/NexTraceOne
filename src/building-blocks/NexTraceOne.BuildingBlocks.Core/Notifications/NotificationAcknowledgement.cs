using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Registra a confirmação explícita de ciência de uma notificação por um usuário.
/// Usado quando a notificação exige acknowledgement obrigatório (alertas de segurança,
/// aprovações críticas, break glass). A confirmação é imutável após criação.
/// </summary>
public sealed class NotificationAcknowledgement : Entity<NotificationAcknowledgementId>
{
    /// <summary>Referência à notificação que foi confirmada.</summary>
    public NotificationId NotificationRequestId { get; private set; } = default!;

    /// <summary>Identificador do usuário que confirmou ciência.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Data/hora UTC em que o usuário confirmou ciência.</summary>
    public DateTimeOffset AcknowledgedAt { get; private set; }

    /// <summary>Comentário opcional do usuário ao confirmar ciência.</summary>
    public string? Comment { get; private set; }

    private NotificationAcknowledgement() { }

    /// <summary>
    /// Factory method para criação de uma confirmação de notificação.
    /// </summary>
    /// <param name="notificationRequestId">Id da notificação confirmada.</param>
    /// <param name="userId">Id do usuário que confirma.</param>
    /// <param name="acknowledgedAt">Data/hora UTC da confirmação.</param>
    /// <param name="comment">Comentário opcional.</param>
    /// <returns>Instância imutável de <see cref="NotificationAcknowledgement"/>.</returns>
    public static NotificationAcknowledgement Create(
        NotificationId notificationRequestId,
        Guid userId,
        DateTimeOffset acknowledgedAt,
        string? comment = null)
    {
        return new NotificationAcknowledgement
        {
            Id = new NotificationAcknowledgementId(Guid.NewGuid()),
            NotificationRequestId = notificationRequestId,
            UserId = userId,
            AcknowledgedAt = acknowledgedAt,
            Comment = comment
        };
    }
}
