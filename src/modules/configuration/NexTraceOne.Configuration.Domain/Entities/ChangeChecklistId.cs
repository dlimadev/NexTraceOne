using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for ChangeChecklist.</summary>
public sealed record ChangeChecklistId(Guid Value) : TypedIdBase(Value);
