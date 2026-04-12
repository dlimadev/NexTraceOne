using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for ContractTemplate.</summary>
public sealed record ContractTemplateId(Guid Value) : TypedIdBase(Value);
