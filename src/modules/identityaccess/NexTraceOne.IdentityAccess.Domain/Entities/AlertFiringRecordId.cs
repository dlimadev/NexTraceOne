using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Strongly-typed ID para AlertFiringRecord.</summary>
public sealed record AlertFiringRecordId(Guid Value) : TypedIdBase(Value)
{
    public static AlertFiringRecordId New() => new(Guid.NewGuid());
    public static AlertFiringRecordId From(Guid value) => new(value);
}
