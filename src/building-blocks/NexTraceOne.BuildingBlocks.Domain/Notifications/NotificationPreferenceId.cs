namespace NexTraceOne.BuildingBlocks.Domain.Notifications;

/// <summary>
/// Identificador fortemente tipado para preferências de notificação do usuário.
/// Cada preferência define o canal e severidade mínima por categoria.
/// </summary>
public sealed record NotificationPreferenceId(Guid Value) : TypedIdBase(Value);
