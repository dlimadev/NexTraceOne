using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.BuildingBlocks.Core.Tags;

/// <summary>Strongly-typed identifier for EntityTag.</summary>
public sealed record EntityTagId(Guid Value) : TypedIdBase(Value);
