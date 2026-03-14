using NexTraceOne.BuildingBlocks.Core;

namespace NexTraceOne.Workflow.Domain.Events;

/// <summary>
/// Evento emitido quando um workflow de aprovação é concluído com sucesso.
/// Consumidores típicos: Promotion, Audit, ChangeIntelligence.
/// </summary>
public sealed record WorkflowApprovedEvent(
    Guid WorkflowId,
    Guid ReleaseId,
    string ApprovedBy,
    DateTimeOffset ApprovedAt) : IntegrationEventBase("ChangeGovernance");
