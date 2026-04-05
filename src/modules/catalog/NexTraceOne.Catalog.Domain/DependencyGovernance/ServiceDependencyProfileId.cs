using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.DependencyGovernance;

/// <summary>Identificador fortemente tipado de ServiceDependencyProfile.</summary>
public sealed record ServiceDependencyProfileId(Guid Value) : TypedIdBase(Value)
{
    public static ServiceDependencyProfileId New() => new(Guid.NewGuid());
}
