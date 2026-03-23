using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;

/// <summary>
/// Publicado quando uma aprovação de workflow fica pendente.
/// Consumidores: módulo de notificações (gerar notificação ao aprovador designado).
/// </summary>
public sealed record ApprovalPendingIntegrationEvent(
    Guid WorkflowId,
    Guid StageId,
    string WorkflowName,
    string RequestedBy,
    Guid? ApproverUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando um workflow é rejeitado.
/// Consumidores: módulo de notificações (informar o solicitante da rejeição).
/// </summary>
public sealed record WorkflowRejectedIntegrationEvent(
    Guid WorkflowId,
    Guid StageId,
    string WorkflowName,
    string RejectedBy,
    string Reason,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");
