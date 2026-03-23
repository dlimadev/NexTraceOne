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

    // ── Workflow / Aprovação ──
    public const string ApprovalPending = "ApprovalPending";
    public const string ApprovalApproved = "ApprovalApproved";
    public const string ApprovalRejected = "ApprovalRejected";

    // ── Segurança / Acesso ──
    public const string BreakGlassActivated = "BreakGlassActivated";
    public const string JitAccessPending = "JitAccessPending";

    // ── Compliance / Governance ──
    public const string ComplianceCheckFailed = "ComplianceCheckFailed";

    // ── FinOps / Budget ──
    public const string BudgetExceeded = "BudgetExceeded";

    // ── Integrações / Operações ──
    public const string IntegrationFailed = "IntegrationFailed";

    // ── AI / Plataforma ──
    public const string AiProviderUnavailable = "AiProviderUnavailable";

    /// <summary>
    /// Todos os tipos de notificação registados no catálogo.
    /// Útil para validação e enumeração em relatórios e testes.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        IncidentCreated,
        IncidentEscalated,
        ApprovalPending,
        ApprovalApproved,
        ApprovalRejected,
        BreakGlassActivated,
        JitAccessPending,
        ComplianceCheckFailed,
        BudgetExceeded,
        IntegrationFailed,
        AiProviderUnavailable
    ];

    /// <summary>
    /// Verifica se um tipo de notificação é válido (pertence ao catálogo).
    /// </summary>
    public static bool IsValid(string eventType) =>
        All.Contains(eventType, StringComparer.Ordinal);
}
