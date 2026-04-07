using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for TaxonomyCategory.</summary>
public sealed record TaxonomyCategoryId(Guid Value) : TypedIdBase(Value);
