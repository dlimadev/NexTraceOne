using NexTraceOne.BuildingBlocks.Domain;

namespace NexTraceOne.ChangeIntelligence.Domain.Events;

/// <summary>
/// Evento emitido quando uma notificação de deployment é recebida de uma plataforma CI/CD.
/// Consumidores típicos: ChangeGovernance, Audit, OperationalIntelligence.
/// </summary>
public sealed record DeploymentEventReceivedEvent(
    Guid DeploymentId,
    string SourceSystem,
    string Environment,
    string Version,
    DateTimeOffset ReceivedAt) : IntegrationEventBase("ChangeGovernance");
