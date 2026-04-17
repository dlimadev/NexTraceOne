using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;

/// <summary>
/// Publicado quando uma promoção entre ambientes é concluída com sucesso.
/// Consumidores: módulo de notificações (informar owner do serviço sobre sucesso da promoção).
/// </summary>
public sealed record PromotionCompletedIntegrationEvent(
    Guid PromotionId,
    string ServiceName,
    string TargetEnvironment,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando uma promoção entre ambientes é bloqueada por política ou gate de qualidade.
/// Consumidores: módulo de notificações (alertar owner sobre bloqueio e razão).
/// </summary>
public sealed record PromotionBlockedIntegrationEvent(
    Guid PromotionId,
    string ServiceName,
    string TargetEnvironment,
    string Reason,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando um rollback é acionado para um serviço num determinado ambiente.
/// Consumidores: módulo de notificações (notificar equipa sobre rollback automático ou manual).
/// </summary>
public sealed record RollbackTriggeredIntegrationEvent(
    Guid ChangeId,
    string ServiceName,
    string EnvironmentName,
    string Reason,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando um deploy é concluído (com ou sem sucesso).
/// Consumidores: módulo de notificações (informar owner sobre resultado do deploy).
/// </summary>
public sealed record DeploymentCompletedIntegrationEvent(
    Guid ChangeId,
    string ServiceName,
    string EnvironmentName,
    bool IsSuccess,
    string? FailureReason,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando o score de confiança de uma mudança é calculado e está abaixo do limiar aceitável.
/// Consumidores: módulo de notificações (alertar owner sobre baixo change confidence score).
/// </summary>
public sealed record ChangeConfidenceScoredIntegrationEvent(
    Guid ChangeId,
    string ServiceName,
    decimal ConfidenceScore,
    string EnvironmentName,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando o blast radius de uma mudança é calculado como alto (acima de limiar configurado).
/// Consumidores: módulo de notificações (alertar owner sobre impacto potencial elevado).
/// </summary>
public sealed record BlastRadiusHighIntegrationEvent(
    Guid ChangeId,
    string ServiceName,
    int AffectedServiceCount,
    string EnvironmentName,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");

/// <summary>
/// Publicado quando a verificação pós-mudança falha para um serviço num dado ambiente.
/// Consumidores: módulo de notificações (alertar owner sobre falha de verificação pós-deploy).
/// </summary>
public sealed record PostChangeVerificationFailedIntegrationEvent(
    Guid ChangeId,
    string ServiceName,
    string EnvironmentName,
    string FailureReason,
    Guid? OwnerUserId) : IntegrationEventBase("ChangeGovernance");
