using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ChangeGovernance.Domain.Workflow.Events;

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
