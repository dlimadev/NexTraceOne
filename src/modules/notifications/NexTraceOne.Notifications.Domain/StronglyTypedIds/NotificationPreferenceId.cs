using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.StronglyTypedIds;

/// <summary>Identificador fortemente tipado para a entidade NotificationPreference.</summary>
public sealed record NotificationPreferenceId(Guid Value) : TypedIdBase(Value);
