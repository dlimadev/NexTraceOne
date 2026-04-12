using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>Strongly-typed identifier para SavedPrompt.</summary>
public sealed record SavedPromptId(Guid Value) : TypedIdBase(Value);
