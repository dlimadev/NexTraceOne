using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Governance.Contracts;

/// <summary>
/// Eventos de integração publicados pelo módulo Governance.
/// Utilizado por outros módulos para reagir a mudanças de estado de governança.
/// </summary>
public static class IntegrationEvents
{
    /// <summary>Publicado quando um relatório de risco é gerado.</summary>
    public sealed record RiskReportGenerated(
        string ReportId,
        string Scope,
        DateTimeOffset GeneratedAt);

    /// <summary>Publicado quando gaps de compliance são detectados.</summary>
    public sealed record ComplianceGapsDetected(
        string ReportId,
        int GapCount,
        DateTimeOffset DetectedAt);

    /// <summary>
    /// Publicado quando verificações de compliance falham para um serviço.
    /// Consumidores: módulo de notificações (alertar owner e papel de governança).
    /// </summary>
    public sealed record ComplianceCheckFailedIntegrationEvent(
        string ReportId,
        string ServiceName,
        int GapCount,
        Guid? OwnerUserId) : IntegrationEventBase("Governance");

    // ── Phase 5: High-Value Domain Events ──

    /// <summary>
    /// Publicado quando uma política de governança é violada.
    /// Consumidores: módulo de notificações (alertar owner e compliance role).
    /// </summary>
    public sealed record PolicyViolatedIntegrationEvent(
        string PolicyName,
        string ServiceName,
        string ViolationDescription,
        Guid? OwnerUserId) : IntegrationEventBase("Governance");

    /// <summary>
    /// Publicado quando uma evidência de compliance está prestes a expirar.
    /// Consumidores: módulo de notificações (alertar owner e governance role).
    /// </summary>
    public sealed record EvidenceExpiringIntegrationEvent(
        Guid EvidenceId,
        string EvidenceName,
        string ServiceName,
        DateTimeOffset ExpiresAt,
        Guid? OwnerUserId) : IntegrationEventBase("Governance");

    /// <summary>
    /// Publicado quando um limiar de orçamento é atingido (80%, 90%, 100%).
    /// Consumidores: módulo de notificações (alertar owner e gestores financeiros).
    /// </summary>
    public sealed record BudgetThresholdReachedIntegrationEvent(
        string ServiceName,
        int ThresholdPercent,
        decimal CurrentSpend,
        decimal BudgetLimit,
        Guid? OwnerUserId) : IntegrationEventBase("Governance");
}
