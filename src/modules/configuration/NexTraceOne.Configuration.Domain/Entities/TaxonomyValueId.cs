using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for TaxonomyValue.</summary>
public sealed record TaxonomyValueId(Guid Value) : TypedIdBase(Value);
