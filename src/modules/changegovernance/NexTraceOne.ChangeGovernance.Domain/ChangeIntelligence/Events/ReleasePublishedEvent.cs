using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;

/// <summary>
/// Evento emitido quando uma release é publicada em um ambiente.
/// Consumidores típicos: Audit, OperationalIntelligence, Catalog.
/// </summary>
public sealed record ReleasePublishedEvent(
    Guid ReleaseId,
    string Version,
    string Environment,
    DateTimeOffset PublishedAt) : IntegrationEventBase("ChangeGovernance");
