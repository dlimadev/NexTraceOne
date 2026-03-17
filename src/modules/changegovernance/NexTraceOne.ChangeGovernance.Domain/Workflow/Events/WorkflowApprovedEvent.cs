using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ChangeGovernance.Domain.Workflow.Events;

/// <summary>
/// Evento emitido quando um workflow de aprovação é concluído com sucesso.
/// Consumidores típicos: Promotion, Audit, ChangeIntelligence.
/// </summary>
public sealed record WorkflowApprovedEvent(
    Guid WorkflowId,
    Guid ReleaseId,
    string ApprovedBy,
    DateTimeOffset ApprovedAt) : IntegrationEventBase("ChangeGovernance");
