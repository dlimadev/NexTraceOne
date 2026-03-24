using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam as definições de configuração de workflow, approvals e
/// promotion governance introduzidas na Fase 3 da parametrização.
/// Garante que todas as chaves estão bem formadas, com categorias, tipos,
/// escopos e valores padrão corretos para o domínio de governança de fluxos.
/// </summary>
public sealed class WorkflowPromotionConfigurationDefinitionsTests
{
    /// <summary>
    /// Constrói as definições Phase 3 de workflow e promotion governance,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase3Definitions() =>
    [
        // Block A — Workflow Types & Templates
        ConfigurationDefinition.Create("workflow.types.enabled", "Enabled Workflow Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["ReleaseApproval","PromotionApproval","WaiverApproval","GovernanceReview"]""", uiEditorType: "json-editor", sortOrder: 2000),
        ConfigurationDefinition.Create("workflow.templates.default", "Default Workflow Template", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"name":"Standard Approval","stages":[{"name":"Review","order":1,"requiredApprovals":1,"approvalRule":"SingleApprover"}]}""", uiEditorType: "json-editor", sortOrder: 2010),
        ConfigurationDefinition.Create("workflow.templates.by_change_level", "Workflow Templates by Change Level", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"1":"Standard","2":"Enhanced","3":"FullGovernance"}""", uiEditorType: "json-editor", sortOrder: 2020),
        ConfigurationDefinition.Create("workflow.templates.active_version", "Active Workflow Template Version", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "1", validationRules: """{"min":1,"max":9999}""", uiEditorType: "text", sortOrder: 2030),

        // Block B — Stages, Sequencing & Quorum
        ConfigurationDefinition.Create("workflow.stages.max_count", "Maximum Workflow Stages", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "10", validationRules: """{"min":1,"max":50}""", uiEditorType: "text", sortOrder: 2100),
        ConfigurationDefinition.Create("workflow.stages.allow_parallel", "Allow Parallel Stages", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 2110),
        ConfigurationDefinition.Create("workflow.quorum.default_rule", "Default Quorum Rule", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "SingleApprover", validationRules: """{"enum":["SingleApprover","Majority","Unanimous"]}""", uiEditorType: "select", sortOrder: 2120),
        ConfigurationDefinition.Create("workflow.quorum.minimum_approvers", "Minimum Approvers per Stage", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "1", validationRules: """{"min":1,"max":20}""", uiEditorType: "text", sortOrder: 2130),
        ConfigurationDefinition.Create("workflow.stages.allow_optional", "Allow Optional Stages", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 2140),

        // Block C — Approvers, Fallback & Escalation
        ConfigurationDefinition.Create("workflow.approvers.policy", "Approver Assignment Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"strategy":"ByOwnership","roles":["TechLead","Architect"]}""", uiEditorType: "json-editor", sortOrder: 2200),
        ConfigurationDefinition.Create("workflow.approvers.fallback", "Fallback Approver Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"enabled":true,"fallbackRoles":["PlatformAdmin"]}""", uiEditorType: "json-editor", sortOrder: 2210),
        ConfigurationDefinition.Create("workflow.approvers.self_approval_allowed", "Self-Approval Allowed", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "false", uiEditorType: "toggle", sortOrder: 2220),
        ConfigurationDefinition.Create("workflow.escalation.enabled", "Workflow Escalation Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 2230),
        ConfigurationDefinition.Create("workflow.escalation.delay_minutes", "Escalation Delay (Minutes)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "240", validationRules: """{"min":15,"max":10080}""", uiEditorType: "text", sortOrder: 2240),
        ConfigurationDefinition.Create("workflow.escalation.target_roles", "Escalation Target Roles", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["PlatformAdmin","Architect"]""", uiEditorType: "json-editor", sortOrder: 2250),
        ConfigurationDefinition.Create("workflow.escalation.by_criticality", "Escalation Policy by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":{"delayMinutes":60,"targets":["PlatformAdmin"]},"high":{"delayMinutes":120,"targets":["TechLead"]},"medium":{"delayMinutes":240,"targets":["TechLead"]}}""", uiEditorType: "json-editor", sortOrder: 2260),

        // Block D — SLAs, Deadlines, Timeout & Expiration
        ConfigurationDefinition.Create("workflow.sla.default_hours", "Default Workflow SLA (Hours)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "48", validationRules: """{"min":1,"max":720}""", uiEditorType: "text", sortOrder: 2300),
        ConfigurationDefinition.Create("workflow.sla.by_type", "Workflow SLA by Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"ReleaseApproval":24,"PromotionApproval":8,"WaiverApproval":48,"GovernanceReview":72}""", uiEditorType: "json-editor", sortOrder: 2310),
        ConfigurationDefinition.Create("workflow.sla.by_environment", "Workflow SLA by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":4,"PreProduction":8}""", uiEditorType: "json-editor", sortOrder: 2320),
        ConfigurationDefinition.Create("workflow.timeout.approval_hours", "Approval Timeout (Hours)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "72", validationRules: """{"min":1,"max":720}""", uiEditorType: "text", sortOrder: 2330),
        ConfigurationDefinition.Create("workflow.expiry.hours", "Workflow Expiry (Hours)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "168", validationRules: """{"min":1,"max":2160}""", uiEditorType: "text", sortOrder: 2340),
        ConfigurationDefinition.Create("workflow.expiry.action", "Expiry Action", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Cancel", validationRules: """{"enum":["Cancel","Escalate","Notify"]}""", uiEditorType: "select", sortOrder: 2350),
        ConfigurationDefinition.Create("workflow.resubmission.allowed", "Re-submission Allowed", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 2360),
        ConfigurationDefinition.Create("workflow.resubmission.max_attempts", "Maximum Re-submission Attempts", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "3", validationRules: """{"min":1,"max":10}""", uiEditorType: "text", sortOrder: 2370),

        // Block E — Gates, Checklists & Auto-Approval
        ConfigurationDefinition.Create("workflow.gates.enabled", "Gates Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 2400),
        ConfigurationDefinition.Create("workflow.gates.by_environment", "Gates by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":["SecurityScan","TestCoverage","ApprovalComplete","EvidencePack"],"PreProduction":["TestCoverage","ApprovalComplete"],"Development":[]}""", uiEditorType: "json-editor", sortOrder: 2410),
        ConfigurationDefinition.Create("workflow.gates.by_criticality", "Gates by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":["SecurityScan","TestCoverage","ApprovalComplete","EvidencePack"],"high":["TestCoverage","ApprovalComplete"],"medium":["ApprovalComplete"],"low":[]}""", uiEditorType: "json-editor", sortOrder: 2420),
        ConfigurationDefinition.Create("workflow.checklist.by_type", "Checklist by Workflow Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"ReleaseApproval":["ChangeDescriptionReviewed","RiskAssessed","RollbackPlanDefined"],"PromotionApproval":["TargetEnvironmentVerified","DependenciesChecked"]}""", uiEditorType: "json-editor", sortOrder: 2430),
        ConfigurationDefinition.Create("workflow.checklist.by_environment", "Checklist by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":["ProductionReadinessConfirmed","MonitoringVerified","RollbackTested"]}""", uiEditorType: "json-editor", sortOrder: 2440),
        ConfigurationDefinition.Create("workflow.auto_approval.enabled", "Auto-Approval Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "false", uiEditorType: "toggle", sortOrder: 2450),
        ConfigurationDefinition.Create("workflow.auto_approval.conditions", "Auto-Approval Conditions", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"maxChangeLevel":1,"excludeEnvironments":["Production","PreProduction"],"requireAllGatesPassed":true}""", uiEditorType: "json-editor", sortOrder: 2460),
        ConfigurationDefinition.Create("workflow.auto_approval.blocked_environments", "Auto-Approval Blocked Environments", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["Production"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 2470),
        ConfigurationDefinition.Create("workflow.evidence_pack.required", "Evidence Pack Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "false", uiEditorType: "toggle", sortOrder: 2480),
        ConfigurationDefinition.Create("workflow.rejection.require_reason", "Require Rejection Reason", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 2490),

        // Block F — Promotion Governance
        ConfigurationDefinition.Create("promotion.paths.allowed", "Allowed Promotion Paths", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """[{"source":"Development","targets":["Test"]},{"source":"Test","targets":["QA"]},{"source":"QA","targets":["PreProduction"]},{"source":"PreProduction","targets":["Production"]}]""", uiEditorType: "json-editor", sortOrder: 2500),
        ConfigurationDefinition.Create("promotion.production.extra_approvers_required", "Extra Approvers for Production", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "1", validationRules: """{"min":0,"max":10}""", uiEditorType: "text", sortOrder: 2510),
        ConfigurationDefinition.Create("promotion.production.extra_gates", "Extra Gates for Production Promotion", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["SecurityScan","ComplianceCheck","PerformanceBaseline"]""", uiEditorType: "json-editor", sortOrder: 2520),
        ConfigurationDefinition.Create("promotion.restrictions.by_criticality", "Promotion Restrictions by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":{"requireAdditionalApprovers":2,"requireEvidencePack":true},"high":{"requireAdditionalApprovers":1,"requireEvidencePack":true}}""", uiEditorType: "json-editor", sortOrder: 2530),
        ConfigurationDefinition.Create("promotion.rollback.recommendation_enabled", "Rollback Recommendation Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 2540),

        // Block G — Release Windows & Freeze Policies
        ConfigurationDefinition.Create("promotion.release_window.enabled", "Release Windows Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "false", uiEditorType: "toggle", sortOrder: 2600),
        ConfigurationDefinition.Create("promotion.release_window.schedule", "Release Window Schedule", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"days":["Monday","Tuesday","Wednesday","Thursday","Friday"],"startTimeUtc":"06:00","endTimeUtc":"18:00"}""", uiEditorType: "json-editor", sortOrder: 2610),
        ConfigurationDefinition.Create("promotion.freeze.enabled", "Freeze Policy Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "false", uiEditorType: "toggle", sortOrder: 2620),
        ConfigurationDefinition.Create("promotion.freeze.windows", "Freeze Windows", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "[]", uiEditorType: "json-editor", sortOrder: 2630),
        ConfigurationDefinition.Create("promotion.freeze.override_allowed", "Freeze Override Allowed", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System], defaultValue: "false", isInheritable: false, uiEditorType: "toggle", sortOrder: 2640),
        ConfigurationDefinition.Create("promotion.freeze.override_roles", "Freeze Override Authorized Roles", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["PlatformAdmin"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 2650),
    ];

    // ── All Phase 3 definitions have unique keys ──────────────────────

    [Fact]
    public void Phase3Definitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase3Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("each configuration definition key must be unique");
    }

    // ── All Phase 3 definitions have unique sortOrders ─────────────────

    [Fact]
    public void Phase3Definitions_ShouldHaveUniqueSortOrders()
    {
        var definitions = BuildPhase3Definitions();
        var sortOrders = definitions.Select(d => d.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("each definition must have a unique sortOrder");
    }

    // ── All Phase 3 definitions are Functional category ───────────────

    [Fact]
    public void Phase3Definitions_ShouldAllBeFunctionalCategory()
    {
        var definitions = BuildPhase3Definitions();

        definitions.Should().OnlyContain(
            d => d.Category == ConfigurationCategory.Functional,
            "workflow and promotion governance definitions should be Functional category");
    }

    // ── All workflow definitions have workflow.* prefix ────────────────

    [Fact]
    public void WorkflowDefinitions_ShouldHaveCorrectKeyPrefix()
    {
        var definitions = BuildPhase3Definitions();
        var workflowDefs = definitions.Where(d => d.Key.StartsWith("workflow.")).ToList();

        workflowDefs.Should().NotBeEmpty("Phase 3 must include workflow definitions");
        workflowDefs.Should().OnlyContain(d => d.Key.StartsWith("workflow."));
    }

    // ── All promotion definitions have promotion.* prefix ─────────────

    [Fact]
    public void PromotionDefinitions_ShouldHaveCorrectKeyPrefix()
    {
        var definitions = BuildPhase3Definitions();
        var promotionDefs = definitions.Where(d => d.Key.StartsWith("promotion.")).ToList();

        promotionDefs.Should().NotBeEmpty("Phase 3 must include promotion governance definitions");
        promotionDefs.Should().OnlyContain(d => d.Key.StartsWith("promotion."));
    }

    // ── Sort orders are in the 2000+ range ────────────────────────────

    [Fact]
    public void Phase3Definitions_SortOrdersShouldBeInPhase3Range()
    {
        var definitions = BuildPhase3Definitions();

        definitions.Should().OnlyContain(
            d => d.SortOrder >= 2000 && d.SortOrder <= 2999,
            "Phase 3 definitions should have sortOrders in the 2000-2999 range");
    }

    // ── Workflow type & template definitions ───────────────────────────

    [Fact]
    public void WorkflowTypes_ShouldBeJsonAndSystemTenantScoped()
    {
        var definitions = BuildPhase3Definitions();
        var typesDef = definitions.Single(d => d.Key == "workflow.types.enabled");

        typesDef.ValueType.Should().Be(ConfigurationValueType.Json);
        typesDef.AllowedScopes.Should().Contain(ConfigurationScope.System);
        typesDef.AllowedScopes.Should().Contain(ConfigurationScope.Tenant);
        typesDef.DefaultValue.Should().Contain("ReleaseApproval");
    }

    [Fact]
    public void WorkflowTemplateDefault_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase3Definitions();
        var templateDef = definitions.Single(d => d.Key == "workflow.templates.default");

        templateDef.ValueType.Should().Be(ConfigurationValueType.Json);
        templateDef.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
    }

    // ── Quorum and stages definitions ─────────────────────────────────

    [Fact]
    public void QuorumDefaultRule_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase3Definitions();
        var quorumDef = definitions.Single(d => d.Key == "workflow.quorum.default_rule");

        quorumDef.ValueType.Should().Be(ConfigurationValueType.String);
        quorumDef.ValidationRules.Should().Contain("SingleApprover");
        quorumDef.ValidationRules.Should().Contain("Majority");
        quorumDef.ValidationRules.Should().Contain("Unanimous");
        quorumDef.DefaultValue.Should().Be("SingleApprover");
    }

    [Fact]
    public void QuorumMinimumApprovers_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.quorum.minimum_approvers");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
        def.DefaultValue.Should().Be("1");
    }

    // ── Approver, fallback & escalation definitions ───────────────────

    [Fact]
    public void SelfApproval_ShouldBeDisabledByDefault()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.approvers.self_approval_allowed");

        def.DefaultValue.Should().Be("false",
            "self-approval should be disabled by default for separation of duties");
    }

    [Fact]
    public void EscalationDelay_ShouldHaveValidationRules()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.escalation.delay_minutes");

        def.DefaultValue.Should().Be("240");
        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
    }

    [Fact]
    public void EscalationByCriticality_ShouldBeJsonWithCriticalityLevels()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.escalation.by_criticality");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().Contain("critical");
        def.DefaultValue.Should().Contain("high");
    }

    // ── SLA definitions ───────────────────────────────────────────────

    [Fact]
    public void SlaDefaultHours_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.sla.default_hours");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "SLA should vary by environment (production = stricter)");
        def.DefaultValue.Should().Be("48");
    }

    [Fact]
    public void SlaByType_ShouldHaveProductionPromotionSLA()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.sla.by_type");

        def.DefaultValue.Should().Contain("PromotionApproval");
        def.DefaultValue.Should().Contain("8");
    }

    [Fact]
    public void ExpiryAction_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.expiry.action");

        def.ValidationRules.Should().Contain("Cancel");
        def.ValidationRules.Should().Contain("Escalate");
        def.ValidationRules.Should().Contain("Notify");
    }

    // ── Gates, checklists & auto-approval definitions ─────────────────

    [Fact]
    public void AutoApproval_ShouldBeDisabledByDefault()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.auto_approval.enabled");

        def.DefaultValue.Should().Be("false",
            "auto-approval should be opt-in for safety");
    }

    [Fact]
    public void AutoApprovalBlockedEnvironments_ShouldBeSystemOnlyAndNonInheritable()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.auto_approval.blocked_environments");

        def.AllowedScopes.Should().HaveCount(1);
        def.AllowedScopes.Should().Contain(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse(
            "blocked environments for auto-approval must be system-level only");
    }

    [Fact]
    public void GatesByEnvironment_ShouldIncludeProductionGates()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.gates.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("SecurityScan");
        def.DefaultValue.Should().Contain("EvidencePack");
    }

    [Fact]
    public void ChecklistByEnvironment_ShouldIncludeProductionReadiness()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.checklist.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("ProductionReadinessConfirmed");
    }

    [Fact]
    public void RejectionRequireReason_ShouldBeEnabledByDefault()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "workflow.rejection.require_reason");

        def.DefaultValue.Should().Be("true",
            "rejection reason should be required by default for governance");
    }

    // ── Promotion governance definitions ──────────────────────────────

    [Fact]
    public void PromotionPaths_ShouldDefineSequentialEnvironmentProgression()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "promotion.paths.allowed");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().Contain("Development");
        def.DefaultValue.Should().Contain("Production");
    }

    [Fact]
    public void ProductionExtraApprovers_ShouldDefaultToOne()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "promotion.production.extra_approvers_required");

        def.DefaultValue.Should().Be("1",
            "production promotion should require at least 1 extra approver by default");
    }

    [Fact]
    public void ProductionExtraGates_ShouldIncludeSecurityAndCompliance()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "promotion.production.extra_gates");

        def.DefaultValue.Should().Contain("SecurityScan");
        def.DefaultValue.Should().Contain("ComplianceCheck");
    }

    // ── Release windows & freeze policies ─────────────────────────────

    [Fact]
    public void ReleaseWindowEnabled_ShouldBeDisabledByDefault()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "promotion.release_window.enabled");

        def.DefaultValue.Should().Be("false",
            "release windows should be opt-in");
    }

    [Fact]
    public void FreezeEnabled_ShouldBeDisabledByDefault()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "promotion.freeze.enabled");

        def.DefaultValue.Should().Be("false",
            "freeze policies should be opt-in");
    }

    [Fact]
    public void FreezeOverride_ShouldBeSystemOnlyAndNonInheritable()
    {
        var definitions = BuildPhase3Definitions();
        var freezeOverride = definitions.Single(d => d.Key == "promotion.freeze.override_allowed");
        var freezeRoles = definitions.Single(d => d.Key == "promotion.freeze.override_roles");

        freezeOverride.AllowedScopes.Should().HaveCount(1);
        freezeOverride.AllowedScopes.Should().Contain(ConfigurationScope.System);
        freezeOverride.IsInheritable.Should().BeFalse();

        freezeRoles.AllowedScopes.Should().HaveCount(1);
        freezeRoles.AllowedScopes.Should().Contain(ConfigurationScope.System);
        freezeRoles.IsInheritable.Should().BeFalse();
    }

    [Fact]
    public void ReleaseWindowSchedule_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase3Definitions();
        var def = definitions.Single(d => d.Key == "promotion.release_window.schedule");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "release windows should be configurable per environment");
        def.DefaultValue.Should().Contain("Monday");
    }

    // ── Boolean definitions use toggle editor ─────────────────────────

    [Fact]
    public void AllBooleanDefinitions_ShouldUseToggleEditor()
    {
        var definitions = BuildPhase3Definitions();
        var boolDefs = definitions.Where(d => d.ValueType == ConfigurationValueType.Boolean).ToList();

        boolDefs.Should().NotBeEmpty();
        boolDefs.Should().OnlyContain(
            d => d.UiEditorType == "toggle",
            "all Boolean definitions should use the toggle UI editor");
    }

    // ── JSON definitions use json-editor ──────────────────────────────

    [Fact]
    public void AllJsonDefinitions_ShouldUseJsonEditor()
    {
        var definitions = BuildPhase3Definitions();
        var jsonDefs = definitions.Where(d => d.ValueType == ConfigurationValueType.Json).ToList();

        jsonDefs.Should().NotBeEmpty();
        jsonDefs.Should().OnlyContain(
            d => d.UiEditorType == "json-editor",
            "all JSON definitions should use the json-editor UI editor");
    }

    // ── Total count validation ────────────────────────────────────────

    [Fact]
    public void Phase3Definitions_ShouldHaveExpectedCount()
    {
        var definitions = BuildPhase3Definitions();

        // 4 types/templates + 5 stages/quorum + 7 approvers/escalation
        // + 8 SLA/timeout + 10 gates/checklists + 5 promotion + 6 release/freeze = 45
        definitions.Should().HaveCount(45,
            "Phase 3 should deliver 45 workflow and promotion governance definitions");
    }
}
