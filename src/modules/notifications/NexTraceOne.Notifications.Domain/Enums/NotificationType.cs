namespace NexTraceOne.Notifications.Domain.Enums;

/// <summary>
/// Catálogo executável de tipos de notificação da plataforma NexTraceOne.
/// Cada tipo mapeia para um evento de negócio real e rastreável.
/// Utilizado pelo orquestrador para decidir template, severidade, roteamento e deduplicação.
/// Novos tipos devem ser adicionados aqui — não usar strings soltas no código.
/// </summary>
public static class NotificationType
{
    // ── Operações / Incidentes ──
    public const string IncidentCreated = "IncidentCreated";
    public const string IncidentEscalated = "IncidentEscalated";
    public const string IncidentResolved = "IncidentResolved";
    public const string AnomalyDetected = "AnomalyDetected";
    public const string HealthDegradation = "HealthDegradation";

    // ── Workflow / Aprovação ──
    public const string ApprovalPending = "ApprovalPending";
    public const string ApprovalApproved = "ApprovalApproved";
    public const string ApprovalRejected = "ApprovalRejected";
    public const string ApprovalExpiring = "ApprovalExpiring";

    // ── Catálogo / Contratos ──
    public const string ContractPublished = "ContractPublished";
    public const string BreakingChangeDetected = "BreakingChangeDetected";
    public const string ContractValidationFailed = "ContractValidationFailed";

    // ── Segurança / Acesso ──
    public const string BreakGlassActivated = "BreakGlassActivated";
    public const string JitAccessPending = "JitAccessPending";
    public const string JitAccessGranted = "JitAccessGranted";
    public const string UserRoleChanged = "UserRoleChanged";
    public const string AccessReviewPending = "AccessReviewPending";

    // ── Compliance / Governance ──
    public const string ComplianceCheckFailed = "ComplianceCheckFailed";
    public const string PolicyViolated = "PolicyViolated";
    public const string EvidenceExpiring = "EvidenceExpiring";

    // ── FinOps / Budget ──
    public const string BudgetExceeded = "BudgetExceeded";
    public const string BudgetThresholdReached = "BudgetThresholdReached";

    // ── Integrações / Operações ──
    public const string IntegrationFailed = "IntegrationFailed";
    public const string SyncFailed = "SyncFailed";
    public const string ConnectorAuthFailed = "ConnectorAuthFailed";

    // ── Change Intelligence / Production Change Confidence ──
    public const string PromotionCompleted = "PromotionCompleted";
    public const string PromotionBlocked = "PromotionBlocked";
    public const string RollbackTriggered = "RollbackTriggered";
    public const string DeploymentCompleted = "DeploymentCompleted";
    public const string ChangeConfidenceScored = "ChangeConfidenceScored";
    public const string BlastRadiusHigh = "BlastRadiusHigh";
    public const string PostChangeVerificationFailed = "PostChangeVerificationFailed";

    // ── AI / Plataforma ──
    public const string AiProviderUnavailable = "AiProviderUnavailable";
    public const string TokenBudgetExceeded = "TokenBudgetExceeded";
    public const string AiGenerationFailed = "AiGenerationFailed";
    public const string AiActionBlockedByPolicy = "AiActionBlockedByPolicy";

    /// <summary>
    /// Todos os tipos de notificação registados no catálogo.
    /// Útil para validação e enumeração em relatórios e testes.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        IncidentCreated,
        IncidentEscalated,
        IncidentResolved,
        AnomalyDetected,
        HealthDegradation,
        ApprovalPending,
        ApprovalApproved,
        ApprovalRejected,
        ApprovalExpiring,
        ContractPublished,
        BreakingChangeDetected,
        ContractValidationFailed,
        BreakGlassActivated,
        JitAccessPending,
        JitAccessGranted,
        UserRoleChanged,
        AccessReviewPending,
        ComplianceCheckFailed,
        PolicyViolated,
        EvidenceExpiring,
        BudgetExceeded,
        BudgetThresholdReached,
        IntegrationFailed,
        SyncFailed,
        ConnectorAuthFailed,
        AiProviderUnavailable,
        TokenBudgetExceeded,
        AiGenerationFailed,
        AiActionBlockedByPolicy,
        PromotionCompleted,
        PromotionBlocked,
        RollbackTriggered,
        DeploymentCompleted,
        ChangeConfidenceScored,
        BlastRadiusHigh,
        PostChangeVerificationFailed
    ];

    /// <summary>
    /// Verifica se um tipo de notificação é válido (pertence ao catálogo).
    /// </summary>
    public static bool IsValid(string eventType) =>
        All.Contains(eventType, StringComparer.Ordinal);
}
