using NexTraceOne.BuildingBlocks.Domain;

namespace NexTraceOne.AiOrchestration.Domain.Events;

/// <summary>
/// Evento emitido quando um candidato a conhecimento organizacional é identificado.
/// Consumidores típicos: AIKnowledge (review pipeline), Audit.
/// </summary>
public sealed record KnowledgeCandidateCreatedEvent(
    Guid CandidateId,
    string Source,
    string Category,
    DateTimeOffset CreatedAt) : IntegrationEventBase("AIKnowledge");
