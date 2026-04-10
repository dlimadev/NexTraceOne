using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for ContractCompliancePolicy.</summary>
public sealed record ContractCompliancePolicyId(Guid Value) : TypedIdBase(Value);
