using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.StronglyTypedIds;

/// <summary>Identificador fortemente tipado para a entidade DeliveryChannelConfiguration.</summary>
public sealed record DeliveryChannelConfigurationId(Guid Value) : TypedIdBase(Value);
