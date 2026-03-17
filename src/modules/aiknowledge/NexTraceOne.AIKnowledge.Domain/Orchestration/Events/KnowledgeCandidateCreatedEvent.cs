using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Events;

/// <summary>
/// Evento emitido quando um candidato a conhecimento organizacional é identificado.
/// Consumidores típicos: AIKnowledge (review pipeline), Audit.
/// </summary>
public sealed record KnowledgeCandidateCreatedEvent(
    Guid CandidateId,
    string Source,
    string Category,
    DateTimeOffset CreatedAt) : IntegrationEventBase("AIKnowledge");
