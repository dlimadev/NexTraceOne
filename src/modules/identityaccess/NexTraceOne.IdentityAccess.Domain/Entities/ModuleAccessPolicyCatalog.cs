namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Catálogo centralizado que define as políticas de acesso padrão por papel
/// ao nível de módulo/página/ação da plataforma NexTraceOne.
///
/// Este catálogo complementa o <see cref="RolePermissionCatalog"/> fornecendo
/// controlo granular ao nível de módulo/página/ação, enquanto o catálogo de
/// permissões trabalha com códigos planos "módulo:recurso:ação".
///
/// Utilizado como fonte de verdade para:
/// - Seed de dados para ambientes de desenvolvimento e produção.
/// - Fallback quando nenhuma política tenant-specific existe.
///
/// Formato de cada entrada: (Module, Page, Action, IsAllowed)
/// - Page="*" → acesso a todas as páginas do módulo.
/// - Action="*" → acesso a todas as ações da página.
/// </summary>
public static class ModuleAccessPolicyCatalog
{
    /// <summary>
    /// Representa uma política de acesso a módulo/página/ação.
    /// </summary>
    /// <param name="Module">Módulo da plataforma (ex.: "Identity", "Catalog").</param>
    /// <param name="Page">Página ou sub-área. Wildcard "*" para todas.</param>
    /// <param name="Action">Ação específica. Wildcard "*" para todas.</param>
    /// <param name="IsAllowed">Se o acesso é concedido.</param>
    public sealed record PolicyEntry(string Module, string Page, string Action, bool IsAllowed);

    /// <summary>
    /// Retorna as políticas de acesso padrão para um papel conhecido do sistema.
    /// Papéis não reconhecidos recebem lista vazia (deny-by-default).
    /// </summary>
    public static IReadOnlyList<PolicyEntry> GetPoliciesForRole(string roleName)
        => roleName switch
        {
            Role.PlatformAdmin => PlatformAdminPolicies,
            Role.TechLead => TechLeadPolicies,
            Role.Developer => DeveloperPolicies,
            Role.Viewer => ViewerPolicies,
            Role.Auditor => AuditorPolicies,
            Role.SecurityReview => SecurityReviewPolicies,
            Role.ApprovalOnly => ApprovalOnlyPolicies,
            _ => []
        };

    /// <summary>
    /// Retorna todos os nomes dos módulos cobertos pelo catálogo.
    /// Útil para validação e relatórios.
    /// </summary>
    public static IReadOnlyList<string> GetAllModules() =>
    [
        "Identity", "Catalog", "Contracts", "DeveloperPortal",
        "ChangeIntelligence", "Workflow", "Operations", "Governance",
        "Promotion", "Audit", "AI", "Integrations", "Platform",
        "Configuration", "Notifications", "Environments"
    ];

    // ── PlatformAdmin: acesso total a todos os módulos ─────────────────────

    private static readonly IReadOnlyList<PolicyEntry> PlatformAdminPolicies =
    [
        new("Identity", "*", "*", true),
        new("Catalog", "*", "*", true),
        new("Contracts", "*", "*", true),
        new("DeveloperPortal", "*", "*", true),
        new("ChangeIntelligence", "*", "*", true),
        new("Workflow", "*", "*", true),
        new("Operations", "*", "*", true),
        new("Governance", "*", "*", true),
        new("Promotion", "*", "*", true),
        new("Audit", "*", "*", true),
        new("AI", "*", "*", true),
        new("Integrations", "*", "*", true),
        new("Platform", "*", "*", true),
        new("Configuration", "*", "*", true),
        new("Notifications", "*", "*", true),
        new("Environments", "*", "*", true)
    ];

    // ── TechLead: acesso amplo com restrições em configuração/plataforma ───

    private static readonly IReadOnlyList<PolicyEntry> TechLeadPolicies =
    [
        new("Identity", "Users", "Read", true),
        new("Identity", "Roles", "Read", true),
        new("Identity", "Sessions", "Read", true),
        new("Identity", "JitAccess", "Decide", true),
        new("Identity", "BreakGlass", "Decide", true),
        new("Identity", "Delegations", "*", true),
        new("Catalog", "*", "*", true),
        new("Contracts", "*", "*", true),
        new("DeveloperPortal", "*", "*", true),
        new("ChangeIntelligence", "*", "*", true),
        new("Workflow", "*", "*", true),
        new("Operations", "Incidents", "*", true),
        new("Operations", "Runbooks", "*", true),
        new("Operations", "Reliability", "Read", true),
        new("Operations", "Runtime", "Read", true),
        new("Operations", "Automation", "Read", true),
        new("Governance", "Domains", "Read", true),
        new("Governance", "Teams", "Read", true),
        new("Governance", "Policies", "Read", true),
        new("Governance", "Compliance", "Read", true),
        new("Governance", "Reports", "Read", true),
        new("Promotion", "*", "*", true),
        new("Audit", "Trail", "Read", true),
        new("Audit", "Reports", "Read", true),
        new("AI", "Assistant", "*", true),
        new("AI", "Governance", "Read", true),
        new("Integrations", "*", "*", true),
        new("Notifications", "*", "*", true),
        new("Environments", "*", "Read", true),
        new("Environments", "*", "Write", true)
    ];

    // ── Developer: foco em desenvolvimento, leitura ampla ──────────────────

    private static readonly IReadOnlyList<PolicyEntry> DeveloperPolicies =
    [
        new("Identity", "Users", "Read", true),
        new("Catalog", "*", "Read", true),
        new("Contracts", "*", "*", true),
        new("DeveloperPortal", "*", "*", true),
        new("ChangeIntelligence", "*", "Read", true),
        new("Workflow", "Instances", "Read", true),
        new("Operations", "Incidents", "*", true),
        new("Operations", "Reliability", "Read", true),
        new("Operations", "Runbooks", "Read", true),
        new("Operations", "Runtime", "Read", true),
        new("Governance", "Domains", "Read", true),
        new("Governance", "Teams", "Read", true),
        new("Promotion", "Requests", "Read", true),
        new("Audit", "Trail", "Read", true),
        new("AI", "Assistant", "*", true),
        new("Integrations", "*", "Read", true),
        new("Notifications", "Inbox", "*", true),
        new("Notifications", "Preferences", "*", true),
        new("Environments", "*", "Read", true)
    ];

    // ── Viewer: apenas leitura em todos os módulos visíveis ────────────────

    private static readonly IReadOnlyList<PolicyEntry> ViewerPolicies =
    [
        new("Identity", "Users", "Read", true),
        new("Catalog", "*", "Read", true),
        new("Contracts", "*", "Read", true),
        new("DeveloperPortal", "*", "Read", true),
        new("ChangeIntelligence", "*", "Read", true),
        new("Workflow", "Instances", "Read", true),
        new("Operations", "Incidents", "Read", true),
        new("Operations", "Reliability", "Read", true),
        new("Governance", "Domains", "Read", true),
        new("Governance", "Reports", "Read", true),
        new("Promotion", "Requests", "Read", true),
        new("Audit", "Trail", "Read", true),
        new("AI", "Assistant", "Read", true),
        new("Notifications", "Inbox", "Read", true),
        new("Notifications", "Preferences", "Read", true),
        new("Environments", "*", "Read", true)
    ];

    // ── Auditor: foco em trilha de auditoria e compliance ──────────────────

    private static readonly IReadOnlyList<PolicyEntry> AuditorPolicies =
    [
        new("Identity", "Users", "Read", true),
        new("Identity", "Sessions", "Read", true),
        new("Catalog", "*", "Read", true),
        new("Contracts", "*", "Read", true),
        new("DeveloperPortal", "*", "Read", true),
        new("ChangeIntelligence", "*", "Read", true),
        new("Workflow", "Instances", "Read", true),
        new("Operations", "Incidents", "Read", true),
        new("Operations", "Reliability", "Read", true),
        new("Operations", "Runbooks", "Read", true),
        new("Governance", "Domains", "Read", true),
        new("Governance", "Compliance", "*", true),
        new("Governance", "Evidence", "Read", true),
        new("Governance", "Reports", "Read", true),
        new("Promotion", "Requests", "Read", true),
        new("Audit", "*", "*", true),
        new("Environments", "*", "Read", true),
        new("Notifications", "Inbox", "Read", true),
        new("Notifications", "Preferences", "Read", true)
    ];

    // ── SecurityReview: foco em segurança e compliance ──────────────────────

    private static readonly IReadOnlyList<PolicyEntry> SecurityReviewPolicies =
    [
        new("Identity", "Users", "Read", true),
        new("Identity", "Roles", "Read", true),
        new("Identity", "Sessions", "*", true),
        new("Identity", "BreakGlass", "Decide", true),
        new("Catalog", "*", "Read", true),
        new("Contracts", "*", "Read", true),
        new("DeveloperPortal", "*", "Read", true),
        new("ChangeIntelligence", "*", "Read", true),
        new("Workflow", "Instances", "Read", true),
        new("Operations", "Incidents", "Read", true),
        new("Operations", "Reliability", "Read", true),
        new("Governance", "Compliance", "Read", true),
        new("Governance", "Risk", "Read", true),
        new("Governance", "Evidence", "Read", true),
        new("Governance", "Policies", "Read", true),
        new("Promotion", "Requests", "Read", true),
        new("Audit", "Trail", "Read", true),
        new("Audit", "Reports", "Read", true),
        new("Audit", "Compliance", "Read", true),
        new("Environments", "*", "Read", true),
        new("Notifications", "Inbox", "Read", true),
        new("Notifications", "Preferences", "Read", true)
    ];

    // ── ApprovalOnly: restrito a fluxos de aprovação ───────────────────────

    private static readonly IReadOnlyList<PolicyEntry> ApprovalOnlyPolicies =
    [
        new("ChangeIntelligence", "*", "Read", true),
        new("Workflow", "Instances", "*", true),
        new("Promotion", "Requests", "*", true),
        new("Promotion", "Gates", "Override", true),
        new("Audit", "Trail", "Read", true),
        new("Notifications", "Inbox", "*", true),
        new("Notifications", "Preferences", "Read", true)
    ];
}
