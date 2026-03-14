namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Identificador fortemente tipado para destinatários de notificação.
/// Cada destinatário representa a associação entre uma notificação e um usuário.
/// </summary>
public sealed record NotificationRecipientId(Guid Value) : TypedIdBase(Value);
