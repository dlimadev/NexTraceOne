using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier para UserWatch.</summary>
public sealed record UserWatchId(Guid Value) : TypedIdBase(Value);
