namespace NexTraceOne.BuildingBlocks.Domain.Notifications;

/// <summary>
/// Identificador fortemente tipado para confirmações de leitura/ação em notificações.
/// Registra que um usuário reconheceu uma notificação que exige confirmação explícita.
/// </summary>
public sealed record NotificationAcknowledgementId(Guid Value) : TypedIdBase(Value);
