namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Identificador fortemente tipado para notificações da plataforma.
/// Previne confusão entre IDs de diferentes aggregates.
/// </summary>
public sealed record NotificationId(Guid Value) : TypedIdBase(Value);
