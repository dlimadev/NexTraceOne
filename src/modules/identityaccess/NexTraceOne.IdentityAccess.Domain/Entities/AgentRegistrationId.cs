using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Strongly-typed ID para AgentRegistration.</summary>
public sealed record AgentRegistrationId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static AgentRegistrationId New() => new(Guid.NewGuid());
    public static AgentRegistrationId From(Guid value) => new(value);
}
