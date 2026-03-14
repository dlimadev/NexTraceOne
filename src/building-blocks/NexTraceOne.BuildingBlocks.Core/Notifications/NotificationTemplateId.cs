namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Identificador fortemente tipado para templates de notificação.
/// Garante que IDs de templates não sejam confundidos com IDs de outros aggregates.
/// </summary>
public sealed record NotificationTemplateId(Guid Value) : TypedIdBase(Value);
