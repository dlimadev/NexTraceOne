using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;

// ── Phase 5: High-Value Domain Events ──

/// <summary>
/// Publicado quando um provider de IA fica indisponível.
/// Consumidores: módulo de notificações (alertar AI admin e utilizadores impactados).
/// </summary>
public sealed record AiProviderUnavailableIntegrationEvent(
    string ProviderName,
    string ErrorMessage,
    Guid? TenantId) : IntegrationEventBase("AIKnowledge");

/// <summary>
/// Publicado quando o budget de tokens de IA é excedido.
/// Consumidores: módulo de notificações (alertar utilizador e AI governance role).
/// </summary>
public sealed record TokenBudgetExceededIntegrationEvent(
    Guid UserId,
    string ProviderName,
    int TokensUsed,
    int TokenLimit,
    Guid? TenantId) : IntegrationEventBase("AIKnowledge");

/// <summary>
/// Publicado quando uma geração de IA falha.
/// Consumidores: módulo de notificações (informar utilizador solicitante).
/// </summary>
public sealed record AiGenerationFailedIntegrationEvent(
    Guid RequestId,
    string ProviderName,
    string ErrorMessage,
    Guid? RequestingUserId,
    Guid? TenantId) : IntegrationEventBase("AIKnowledge");

/// <summary>
/// Publicado quando uma ação de IA é bloqueada por política.
/// Consumidores: módulo de notificações (informar utilizador e AI governance role).
/// </summary>
public sealed record AiActionBlockedByPolicyIntegrationEvent(
    string PolicyName,
    string ActionDescription,
    Guid? UserId,
    Guid? TenantId) : IntegrationEventBase("AIKnowledge");

/// <summary>
/// Publicado quando o feedback negativo acumulado sobre um modelo ou agent
/// excede o threshold configurado nas últimas 24h.
/// Consumidores: módulo de notificações (alertar Platform Admin).
/// </summary>
public sealed record ModelFeedbackThresholdExceededIntegrationEvent(
    string AgentName,
    string ModelUsed,
    int NegativeCount,
    int ThresholdValue,
    string Period,
    Guid? TenantId) : IntegrationEventBase("AIKnowledge");
