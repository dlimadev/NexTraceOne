namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Identificador fortemente tipado para tentativas de entrega de notificação.
/// Cada delivery rastreia uma tentativa de envio por um canal específico.
/// </summary>
public sealed record NotificationDeliveryId(Guid Value) : TypedIdBase(Value);
