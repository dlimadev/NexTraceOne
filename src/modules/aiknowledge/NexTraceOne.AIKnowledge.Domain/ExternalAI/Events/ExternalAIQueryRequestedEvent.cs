using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Events;

/// <summary>
/// Evento emitido quando uma consulta a IA externa é solicitada.
/// Consumidores típicos: Audit (rastreabilidade), AIKnowledge (knowledge capture).
/// </summary>
public sealed record ExternalAIQueryRequestedEvent(
    Guid QueryId,
    string Provider,
    string Context,
    DateTimeOffset RequestedAt) : IntegrationEventBase("AIKnowledge");
