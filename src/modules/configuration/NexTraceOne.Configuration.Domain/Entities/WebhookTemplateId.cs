using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier para WebhookTemplate.</summary>
public sealed record WebhookTemplateId(Guid Value) : TypedIdBase(Value);
