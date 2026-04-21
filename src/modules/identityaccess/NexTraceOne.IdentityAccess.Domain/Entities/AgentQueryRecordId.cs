using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Identificador fortemente tipado de AgentQueryRecord.</summary>
public sealed record AgentQueryRecordId(Guid Value) : TypedIdBase(Value)
{
    public static AgentQueryRecordId New() => new(Guid.NewGuid());
    public static AgentQueryRecordId From(Guid value) => new(value);
}
