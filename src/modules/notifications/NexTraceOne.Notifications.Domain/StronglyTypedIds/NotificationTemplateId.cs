using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.StronglyTypedIds;

/// <summary>Identificador fortemente tipado para a entidade NotificationTemplate.</summary>
public sealed record NotificationTemplateId(Guid Value) : TypedIdBase(Value);
