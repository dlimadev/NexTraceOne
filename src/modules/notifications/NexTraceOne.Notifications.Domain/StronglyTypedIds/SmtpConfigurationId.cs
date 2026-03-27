using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.StronglyTypedIds;

/// <summary>Identificador fortemente tipado para a entidade SmtpConfiguration.</summary>
public sealed record SmtpConfigurationId(Guid Value) : TypedIdBase(Value);
