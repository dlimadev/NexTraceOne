using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.StronglyTypedIds;

/// <summary>Identificador fortemente tipado para a entidade Notification.</summary>
public sealed record NotificationId(Guid Value) : TypedIdBase(Value);
