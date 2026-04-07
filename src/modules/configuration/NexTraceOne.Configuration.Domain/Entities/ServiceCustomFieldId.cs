using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for ServiceCustomField.</summary>
public sealed record ServiceCustomFieldId(Guid Value) : TypedIdBase(Value);
