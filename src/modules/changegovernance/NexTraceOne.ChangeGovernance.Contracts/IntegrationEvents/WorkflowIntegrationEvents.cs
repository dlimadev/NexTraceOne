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

// ── Phase 5: High-Value Domain Events ──

/// <summary>
/// Publicado quando uma aprovação de workflow é concedida.
/// Consumidores: módulo de notificações (informar o solicitante da aprovação).
/// </summary>
public sealed record ApprovalApprovedIntegrationEvent(
    Guid WorkflowId,
    Guid StageId,
    string WorkflowName,
    string ApprovedBy,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando uma aprovação está prestes a expirar.
/// Consumidores: módulo de notificações (alertar aprovador sobre prazo).
/// </summary>
public sealed record ApprovalExpiringIntegrationEvent(
    Guid WorkflowId,
    Guid StageId,
    string WorkflowName,
    DateTimeOffset ExpiresAt,
    Guid? ApproverUserId) : IntegrationEventBase("ChangeGovernance");
