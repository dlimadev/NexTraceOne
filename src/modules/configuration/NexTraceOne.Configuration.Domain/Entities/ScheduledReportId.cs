using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier for ScheduledReport.</summary>
public sealed record ScheduledReportId(Guid Value) : TypedIdBase(Value);
