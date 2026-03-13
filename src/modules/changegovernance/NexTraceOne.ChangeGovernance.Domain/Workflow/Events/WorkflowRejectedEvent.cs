using NexTraceOne.BuildingBlocks.Domain;

namespace NexTraceOne.Workflow.Domain.Events;

/// <summary>
/// Evento emitido quando um workflow de aprovação é rejeitado.
/// Consumidores típicos: ChangeIntelligence, Audit.
/// </summary>
public sealed record WorkflowRejectedEvent(
    Guid WorkflowId,
    Guid ReleaseId,
    string RejectedBy,
    string Reason,
    DateTimeOffset RejectedAt) : IntegrationEventBase("ChangeGovernance");
