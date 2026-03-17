using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Events;

/// <summary>
/// Evento emitido quando uma resposta de IA externa é recebida.
/// Consumidores típicos: Audit, AIKnowledge (knowledge capture).
/// </summary>
public sealed record ExternalAIResponseReceivedEvent(
    Guid QueryId,
    string Provider,
    bool Success,
    int TokensUsed,
    DateTimeOffset ReceivedAt) : IntegrationEventBase("AIKnowledge");
