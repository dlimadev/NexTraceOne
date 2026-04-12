using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier para UserAlertRule.</summary>
public sealed record UserAlertRuleId(Guid Value) : TypedIdBase(Value);
