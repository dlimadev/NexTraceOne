using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for AutomationRule.</summary>
public sealed record AutomationRuleId(Guid Value) : TypedIdBase(Value);
