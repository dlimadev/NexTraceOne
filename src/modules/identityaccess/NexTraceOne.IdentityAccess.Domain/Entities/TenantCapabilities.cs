namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Capabilities disponíveis por plano SaaS.
/// Cada string é incluída como claim "capabilities" no JWT.
/// Frontend e backend usam estas strings para habilitar/desabilitar features.
/// </summary>
public static class TenantCapabilities
{
    // ── Core — disponível em todos os planos ──────────────────────────────
    public const string Apm = "apm";
    public const string Infra = "infra";
    public const string ServiceCatalog = "service_catalog";
    public const string ChangeGovernanceBasic = "change_governance_basic";
    public const string KnowledgeBase = "knowledge_base";
    public const string BasicAnalytics = "basic_analytics";

    // ── IA — controlo granular de uso de inteligência artificial ─────────
    /// <summary>Toggle principal: IA ligada para o tenant. Quando ausente, toda UI de IA é ocultada.</summary>
    public const string AiEnabled = "ai_enabled";
    /// <summary>Permite uso de IA interna (Ollama / LM Studio). Requer ai_enabled.</summary>
    public const string AiInternal = "ai_internal";
    /// <summary>Permite uso de IA externa (OpenAI / Anthropic). Requer ai_enabled.</summary>
    public const string AiExternal = "ai_external";
    /// <summary>Activa gestão de budget e quotas de tokens por utilizador/tenant.</summary>
    public const string AiTokenBudget = "ai_token_budget";

    // ── Professional ──────────────────────────────────────────────────────
    public const string AiGovernance = "ai_governance";
    public const string ContractStudio = "contract_studio";
    public const string ComplianceBasic = "compliance_basic";
    public const string FinOps = "finops";
    public const string ChangeGovernanceAdvanced = "change_governance_advanced";
    public const string DeveloperPortal = "developer_portal";
    public const string AdvancedAnalytics = "advanced_analytics";

    // ── Enterprise ────────────────────────────────────────────────────────
    public const string ComplianceAdvanced = "compliance_advanced";
    public const string MultiRegion = "multi_region";
    public const string AirGapped = "air_gapped";
    public const string CustomAgents = "custom_agents";
    public const string SsoEnterprise = "sso_enterprise";
    public const string AuditExport = "audit_export";
    public const string CrossTenantBenchmarks = "cross_tenant_benchmarks";

    /// <summary>Retorna a lista de capabilities para o plano dado.</summary>
    public static IReadOnlyList<string> ForPlan(TenantPlan plan) => plan switch
    {
        TenantPlan.Starter => StarterCapabilities,
        TenantPlan.Professional => ProfessionalCapabilities,
        TenantPlan.Enterprise => EnterpriseCapabilities,
        TenantPlan.Trial => TrialCapabilities,
        _ => StarterCapabilities,
    };

    private static readonly IReadOnlyList<string> StarterCapabilities =
    [
        Apm, Infra, ServiceCatalog, ChangeGovernanceBasic, KnowledgeBase, BasicAnalytics,
    ];

    private static readonly IReadOnlyList<string> ProfessionalCapabilities =
    [
        Apm, Infra, ServiceCatalog, ChangeGovernanceBasic, KnowledgeBase, BasicAnalytics,
        AiEnabled, AiInternal, AiTokenBudget,
        AiGovernance, ContractStudio, ComplianceBasic, FinOps,
        ChangeGovernanceAdvanced, DeveloperPortal, AdvancedAnalytics,
    ];

    private static readonly IReadOnlyList<string> EnterpriseCapabilities =
    [
        Apm, Infra, ServiceCatalog, ChangeGovernanceBasic, KnowledgeBase, BasicAnalytics,
        AiEnabled, AiInternal, AiExternal, AiTokenBudget,
        AiGovernance, ContractStudio, ComplianceBasic, FinOps,
        ChangeGovernanceAdvanced, DeveloperPortal, AdvancedAnalytics,
        ComplianceAdvanced, MultiRegion, AirGapped, CustomAgents,
        SsoEnterprise, AuditExport, CrossTenantBenchmarks,
    ];

    // Trial: todas as Professional + preview das Enterprise mais apelativas.
    // multi_region / air_gapped são escolhas de topologia, não features a demonstrar em trial.
    private static readonly IReadOnlyList<string> TrialCapabilities =
    [
        Apm, Infra, ServiceCatalog, ChangeGovernanceBasic, KnowledgeBase, BasicAnalytics,
        AiEnabled, AiInternal, AiExternal, AiTokenBudget,
        AiGovernance, ContractStudio, ComplianceBasic, FinOps,
        ChangeGovernanceAdvanced, DeveloperPortal, AdvancedAnalytics,
        ComplianceAdvanced, CustomAgents, SsoEnterprise, AuditExport,
    ];
}
