using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.StronglyTypedIds;

/// <summary>Identificador fortemente tipado para a entidade NotificationDelivery.</summary>
public sealed record NotificationDeliveryId(Guid Value) : TypedIdBase(Value);
