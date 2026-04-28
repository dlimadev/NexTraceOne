using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Strongly-typed ID para AgentRegistration.</summary>
public sealed record AgentRegistrationId(Guid Value) : TypedIdBase(Value)
{
    public static AgentRegistrationId New() => new(Guid.NewGuid());
    public static AgentRegistrationId From(Guid value) => new(value);
}
