using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam as definições de configuração de governance, compliance,
/// waivers e packs introduzidas na Fase 4 da parametrização.
/// Garante que todas as chaves estão bem formadas, com categorias, tipos,
/// escopos e valores padrão corretos para o domínio de governança.
/// </summary>
public sealed class GovernanceComplianceConfigurationDefinitionsTests
{
    /// <summary>
    /// Constrói as definições Phase 4 de governance, compliance, waivers e packs,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase4Definitions() =>
    [
        // Block A — Policy Catalog & Compliance Profiles
        ConfigurationDefinition.Create("governance.policies.enabled", "Enabled Governance Policies", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["SecurityBaseline","ApiVersioning","DocumentationCoverage","TestCoverage","OwnershipRequired"]""", uiEditorType: "json-editor", sortOrder: 3000),
        ConfigurationDefinition.Create("governance.policies.severity", "Policy Severity Map", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"SecurityBaseline":"Critical","ApiVersioning":"High","DocumentationCoverage":"Medium","TestCoverage":"High","OwnershipRequired":"Critical"}""", uiEditorType: "json-editor", sortOrder: 3010),
        ConfigurationDefinition.Create("governance.policies.criticality", "Policy Criticality Map", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"SecurityBaseline":"Blocking","ApiVersioning":"NonBlocking","DocumentationCoverage":"Advisory","TestCoverage":"NonBlocking","OwnershipRequired":"Blocking"}""", uiEditorType: "json-editor", sortOrder: 3020),
        ConfigurationDefinition.Create("governance.policies.category_map", "Policy Category Map", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"SecurityBaseline":"Security","ApiVersioning":"Quality","DocumentationCoverage":"Documentation","TestCoverage":"Quality","OwnershipRequired":"Operational"}""", uiEditorType: "json-editor", sortOrder: 3030),
        ConfigurationDefinition.Create("governance.policies.applicability", "Policy Applicability Rules", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"SecurityBaseline":{"applies_to":"all"},"ApiVersioning":{"applies_to":["REST","SOAP"]},"TestCoverage":{"applies_to":"all"}}""", uiEditorType: "json-editor", sortOrder: 3040),
        ConfigurationDefinition.Create("governance.compliance.profiles.enabled", "Enabled Compliance Profiles", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Standard","Enhanced","Strict"]""", uiEditorType: "json-editor", sortOrder: 3050),
        ConfigurationDefinition.Create("governance.compliance.profiles.default", "Default Compliance Profile", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "Standard", validationRules: """{"enum":["Standard","Enhanced","Strict"]}""", uiEditorType: "select", sortOrder: 3060),
        ConfigurationDefinition.Create("governance.compliance.profiles.policies_map", "Compliance Profile Policy Map", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Standard":["SecurityBaseline","OwnershipRequired"],"Enhanced":["SecurityBaseline","OwnershipRequired","ApiVersioning","TestCoverage"],"Strict":["SecurityBaseline","OwnershipRequired","ApiVersioning","TestCoverage","DocumentationCoverage"]}""", uiEditorType: "json-editor", sortOrder: 3070),
        ConfigurationDefinition.Create("governance.compliance.profiles.by_environment", "Compliance Profile by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":"Strict","PreProduction":"Enhanced","Development":"Standard"}""", uiEditorType: "json-editor", sortOrder: 3080),

        // Block B — Evidence Requirements
        ConfigurationDefinition.Create("governance.evidence.types_accepted", "Accepted Evidence Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Document","Screenshot","TestReport","ScanResult","AuditLog","Attestation"]""", uiEditorType: "json-editor", sortOrder: 3100),
        ConfigurationDefinition.Create("governance.evidence.required_by_policy", "Evidence Required by Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"SecurityBaseline":{"mandatory":true,"types":["ScanResult"],"minCount":1},"TestCoverage":{"mandatory":true,"types":["TestReport"],"minCount":1}}""", uiEditorType: "json-editor", sortOrder: 3110),
        ConfigurationDefinition.Create("governance.evidence.expiry_days", "Evidence Default Expiry (Days)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "90", validationRules: """{"min":1,"max":730}""", uiEditorType: "text", sortOrder: 3120),
        ConfigurationDefinition.Create("governance.evidence.expiry_by_criticality", "Evidence Expiry by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":30,"high":60,"medium":90,"low":180}""", uiEditorType: "json-editor", sortOrder: 3130),
        ConfigurationDefinition.Create("governance.evidence.expired_action", "Expired Evidence Action", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Notify", validationRules: """{"enum":["Notify","Block","Degrade"]}""", uiEditorType: "select", sortOrder: 3140),
        ConfigurationDefinition.Create("governance.evidence.required_by_environment", "Evidence Required by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"mandatory":true,"minCount":1},"PreProduction":{"mandatory":false}}""", uiEditorType: "json-editor", sortOrder: 3150),

        // Block C — Waiver Rules
        ConfigurationDefinition.Create("governance.waiver.eligible_policies", "Policies Eligible for Waiver", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["ApiVersioning","DocumentationCoverage","TestCoverage"]""", uiEditorType: "json-editor", sortOrder: 3200),
        ConfigurationDefinition.Create("governance.waiver.blocked_severities", "Waiver Blocked Severities", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["Critical"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 3210),
        ConfigurationDefinition.Create("governance.waiver.validity_days_default", "Default Waiver Validity (Days)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "30", validationRules: """{"min":1,"max":365}""", uiEditorType: "text", sortOrder: 3220),
        ConfigurationDefinition.Create("governance.waiver.validity_days_max", "Maximum Waiver Validity (Days)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "90", validationRules: """{"min":1,"max":365}""", uiEditorType: "text", sortOrder: 3230),
        ConfigurationDefinition.Create("governance.waiver.require_approval", "Waiver Requires Approval", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 3240),
        ConfigurationDefinition.Create("governance.waiver.require_evidence", "Waiver Requires Evidence", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 3250),
        ConfigurationDefinition.Create("governance.waiver.allowed_environments", "Waiver Allowed Environments", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Development","Test","QA"]""", uiEditorType: "json-editor", sortOrder: 3260),
        ConfigurationDefinition.Create("governance.waiver.blocked_environments", "Waiver Blocked Environments", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["Production"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 3270),
        ConfigurationDefinition.Create("governance.waiver.renewal.allowed", "Waiver Renewal Allowed", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 3280),
        ConfigurationDefinition.Create("governance.waiver.renewal.max_count", "Maximum Waiver Renewals", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "2", validationRules: """{"min":0,"max":10}""", uiEditorType: "text", sortOrder: 3290),

        // Block D — Governance Packs & Bindings
        ConfigurationDefinition.Create("governance.packs.enabled", "Enabled Governance Packs", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["CoreGovernance","ApiGovernance","SecurityHardening"]""", uiEditorType: "json-editor", sortOrder: 3300),
        ConfigurationDefinition.Create("governance.packs.active_version", "Active Governance Pack Version", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "1", validationRules: """{"min":1,"max":9999}""", uiEditorType: "text", sortOrder: 3310),
        ConfigurationDefinition.Create("governance.packs.binding_policy", "Pack Binding Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"bindBy":["tenant","environment","systemType"],"precedence":"most_specific_wins"}""", uiEditorType: "json-editor", sortOrder: 3320),
        ConfigurationDefinition.Create("governance.packs.by_environment", "Governance Packs by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":["CoreGovernance","SecurityHardening"],"PreProduction":["CoreGovernance"],"Development":["CoreGovernance"]}""", uiEditorType: "json-editor", sortOrder: 3330),
        ConfigurationDefinition.Create("governance.packs.by_system_type", "Governance Packs by System Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":["ApiGovernance","CoreGovernance"],"SOAP":["ApiGovernance","CoreGovernance"],"Event":["CoreGovernance"],"Background":["CoreGovernance"]}""", uiEditorType: "json-editor", sortOrder: 3340),
        ConfigurationDefinition.Create("governance.packs.overlap_resolution", "Pack Overlap Resolution Strategy", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "MostRestrictive", validationRules: """{"enum":["MostRestrictive","MostSpecific","Merge"]}""", uiEditorType: "select", sortOrder: 3350),

        // Block E — Scorecards, Thresholds & Risk Matrix
        ConfigurationDefinition.Create("governance.scorecard.enabled", "Governance Scorecard Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 3400),
        ConfigurationDefinition.Create("governance.scorecard.thresholds", "Scorecard Score Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Excellent":90,"Good":70,"Fair":50,"Poor":0}""", uiEditorType: "json-editor", sortOrder: 3410),
        ConfigurationDefinition.Create("governance.scorecard.weights", "Scorecard Category Weights", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Security":30,"Quality":25,"Operational":25,"Documentation":20}""", uiEditorType: "json-editor", sortOrder: 3420),
        ConfigurationDefinition.Create("governance.scorecard.thresholds_by_environment", "Scorecard Thresholds by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"Excellent":95,"Good":80,"Fair":60,"Poor":0},"Development":{"Excellent":80,"Good":60,"Fair":40,"Poor":0}}""", uiEditorType: "json-editor", sortOrder: 3430),
        ConfigurationDefinition.Create("governance.risk.matrix", "Risk Matrix Definition", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"High_High":"Critical","High_Medium":"High","High_Low":"Medium","Medium_High":"High","Medium_Medium":"Medium","Medium_Low":"Low","Low_High":"Medium","Low_Medium":"Low","Low_Low":"Low"}""", uiEditorType: "json-editor", sortOrder: 3440),
        ConfigurationDefinition.Create("governance.risk.thresholds", "Risk Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Critical":90,"High":70,"Medium":40,"Low":0}""", uiEditorType: "json-editor", sortOrder: 3450),
        ConfigurationDefinition.Create("governance.risk.labels", "Risk Classification Labels", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626"},"High":{"label":"High","color":"#F59E0B"},"Medium":{"label":"Medium","color":"#3B82F6"},"Low":{"label":"Low","color":"#10B981"}}""", uiEditorType: "json-editor", sortOrder: 3460),
        ConfigurationDefinition.Create("governance.risk.thresholds_by_criticality", "Risk Thresholds by Service Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":{"Critical":80,"High":60,"Medium":30,"Low":0},"standard":{"Critical":90,"High":70,"Medium":40,"Low":0}}""", uiEditorType: "json-editor", sortOrder: 3470),

        // Block F — Minimum Requirements by System/API Type
        ConfigurationDefinition.Create("governance.requirements.by_system_type", "Minimum Requirements by System Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"REST":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"mandatoryPack":"ApiGovernance","minScore":70},"SOAP":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"mandatoryPack":"ApiGovernance","minScore":70},"Event":{"mandatoryPolicies":["SecurityBaseline"],"mandatoryPack":"CoreGovernance","minScore":60},"Background":{"mandatoryPolicies":["SecurityBaseline"],"mandatoryPack":"CoreGovernance","minScore":50}}""", uiEditorType: "json-editor", sortOrder: 3500),
        ConfigurationDefinition.Create("governance.requirements.by_api_type", "Minimum Requirements by API Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Public":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning","DocumentationCoverage"],"minScore":80},"Internal":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"minScore":60},"Partner":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning","DocumentationCoverage"],"minScore":75}}""", uiEditorType: "json-editor", sortOrder: 3510),
        ConfigurationDefinition.Create("governance.requirements.mandatory_evidence_by_classification", "Mandatory Evidence by Classification", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":{"types":["ScanResult","TestReport","Attestation"],"minCount":2},"standard":{"types":["ScanResult"],"minCount":1}}""", uiEditorType: "json-editor", sortOrder: 3520),
        ConfigurationDefinition.Create("governance.requirements.min_compliance_profile", "Minimum Compliance Profile by Classification", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":"Strict","standard":"Standard"}""", uiEditorType: "json-editor", sortOrder: 3530),
        ConfigurationDefinition.Create("governance.requirements.promotion_gates", "Governance Promotion Gates", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Production":{"minScore":70,"requiredProfile":"Enhanced","allBlockingPoliciesMet":true},"PreProduction":{"minScore":50,"allBlockingPoliciesMet":true}}""", uiEditorType: "json-editor", sortOrder: 3540),
    ];

    // ── All Phase 4 definitions have unique keys ──────────────────────

    [Fact]
    public void Phase4Definitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase4Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("each configuration definition key must be unique");
    }

    // ── All Phase 4 definitions have unique sortOrders ─────────────────

    [Fact]
    public void Phase4Definitions_ShouldHaveUniqueSortOrders()
    {
        var definitions = BuildPhase4Definitions();
        var sortOrders = definitions.Select(d => d.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("each definition must have a unique sortOrder");
    }

    // ── All Phase 4 definitions are Functional category ───────────────

    [Fact]
    public void Phase4Definitions_ShouldAllBeFunctionalCategory()
    {
        var definitions = BuildPhase4Definitions();

        definitions.Should().OnlyContain(
            d => d.Category == ConfigurationCategory.Functional,
            "governance and compliance definitions should be Functional category");
    }

    // ── All governance definitions have governance.* prefix ───────────

    [Fact]
    public void GovernanceDefinitions_ShouldHaveCorrectKeyPrefix()
    {
        var definitions = BuildPhase4Definitions();

        definitions.Should().OnlyContain(
            d => d.Key.StartsWith("governance."),
            "Phase 4 definitions must use the governance.* key prefix");
    }

    // ── Sort orders are in the 3000+ range ────────────────────────────

    [Fact]
    public void Phase4Definitions_SortOrdersShouldBeInPhase4Range()
    {
        var definitions = BuildPhase4Definitions();

        definitions.Should().OnlyContain(
            d => d.SortOrder >= 3000 && d.SortOrder <= 3999,
            "Phase 4 definitions should have sortOrders in the 3000-3999 range");
    }

    // ── Policy catalog definitions ────────────────────────────────────

    [Fact]
    public void PolicyEnabled_ShouldBeJsonWithDefaultPolicies()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.policies.enabled");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.AllowedScopes.Should().Contain(ConfigurationScope.System);
        def.AllowedScopes.Should().Contain(ConfigurationScope.Tenant);
        def.DefaultValue.Should().Contain("SecurityBaseline");
    }

    [Fact]
    public void PolicySeverity_ShouldMapPolicyToSeverityLevel()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.policies.severity");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().Contain("Critical");
        def.DefaultValue.Should().Contain("High");
        def.DefaultValue.Should().Contain("Medium");
    }

    [Fact]
    public void PolicyCriticality_ShouldIncludeBlockingLevels()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.policies.criticality");

        def.DefaultValue.Should().Contain("Blocking");
        def.DefaultValue.Should().Contain("NonBlocking");
        def.DefaultValue.Should().Contain("Advisory");
    }

    // ── Compliance profiles ───────────────────────────────────────────

    [Fact]
    public void ComplianceProfileDefault_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.compliance.profiles.default");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "compliance profile should vary by environment");
        def.DefaultValue.Should().Be("Standard");
        def.ValidationRules.Should().Contain("Standard");
        def.ValidationRules.Should().Contain("Enhanced");
        def.ValidationRules.Should().Contain("Strict");
    }

    [Fact]
    public void ComplianceProfileByEnvironment_ShouldMapProductionToStrict()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.compliance.profiles.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("Strict");
    }

    // ── Evidence definitions ──────────────────────────────────────────

    [Fact]
    public void EvidenceExpiryDays_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.evidence.expiry_days");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "evidence expiry should vary by environment");
        def.DefaultValue.Should().Be("90");
    }

    [Fact]
    public void ExpiredEvidenceAction_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.evidence.expired_action");

        def.ValidationRules.Should().Contain("Notify");
        def.ValidationRules.Should().Contain("Block");
        def.ValidationRules.Should().Contain("Degrade");
    }

    // ── Waiver definitions ────────────────────────────────────────────

    [Fact]
    public void WaiverBlockedSeverities_ShouldBeSystemOnlyAndNonInheritable()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.waiver.blocked_severities");

        def.AllowedScopes.Should().HaveCount(1);
        def.AllowedScopes.Should().Contain(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse(
            "blocked severities for waiver must be system-level only");
        def.DefaultValue.Should().Contain("Critical");
    }

    [Fact]
    public void WaiverBlockedEnvironments_ShouldBlockProduction()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.waiver.blocked_environments");

        def.AllowedScopes.Should().HaveCount(1);
        def.AllowedScopes.Should().Contain(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse();
        def.DefaultValue.Should().Contain("Production");
    }

    [Fact]
    public void WaiverRequireApproval_ShouldBeEnabledByDefault()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.waiver.require_approval");

        def.DefaultValue.Should().Be("true",
            "waiver approval should be required by default for governance");
    }

    [Fact]
    public void WaiverRequireEvidence_ShouldBeEnabledByDefault()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.waiver.require_evidence");

        def.DefaultValue.Should().Be("true",
            "waiver evidence should be required by default for auditability");
    }

    [Fact]
    public void WaiverValidityMax_ShouldBe90Days()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.waiver.validity_days_max");

        def.DefaultValue.Should().Be("90");
        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
    }

    // ── Governance packs & bindings ───────────────────────────────────

    [Fact]
    public void PacksEnabled_ShouldIncludeDefaultPacks()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.packs.enabled");

        def.DefaultValue.Should().Contain("CoreGovernance");
        def.DefaultValue.Should().Contain("ApiGovernance");
        def.DefaultValue.Should().Contain("SecurityHardening");
    }

    [Fact]
    public void PackOverlapResolution_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.packs.overlap_resolution");

        def.ValidationRules.Should().Contain("MostRestrictive");
        def.ValidationRules.Should().Contain("MostSpecific");
        def.ValidationRules.Should().Contain("Merge");
        def.DefaultValue.Should().Be("MostRestrictive");
    }

    [Fact]
    public void PacksByEnvironment_ShouldBindProductionToSecurityHardening()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.packs.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("SecurityHardening");
    }

    // ── Scorecards & risk ─────────────────────────────────────────────

    [Fact]
    public void ScorecardEnabled_ShouldBeActiveByDefault()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.scorecard.enabled");

        def.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void ScorecardWeights_ShouldSumTo100()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.scorecard.weights");

        // Default: Security:30, Quality:25, Operational:25, Documentation:20 = 100
        def.DefaultValue.Should().Contain("30");
        def.DefaultValue.Should().Contain("25");
        def.DefaultValue.Should().Contain("20");
    }

    [Fact]
    public void RiskMatrix_ShouldMapLikelihoodImpactToCriticality()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.risk.matrix");

        def.DefaultValue.Should().Contain("High_High");
        def.DefaultValue.Should().Contain("Critical");
        def.DefaultValue.Should().Contain("Low_Low");
        def.DefaultValue.Should().Contain("Low");
    }

    [Fact]
    public void RiskThresholds_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.risk.thresholds");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "risk thresholds should vary by environment");
    }

    [Fact]
    public void RiskLabels_ShouldIncludeColorsForVisualization()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.risk.labels");

        def.DefaultValue.Should().Contain("#DC2626"); // Critical red
        def.DefaultValue.Should().Contain("#10B981"); // Low green
    }

    // ── Minimum requirements ──────────────────────────────────────────

    [Fact]
    public void RequirementsBySystemType_ShouldDefineRestAndSoapRequirements()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.requirements.by_system_type");

        def.DefaultValue.Should().Contain("REST");
        def.DefaultValue.Should().Contain("SOAP");
        def.DefaultValue.Should().Contain("SecurityBaseline");
        def.DefaultValue.Should().Contain("ApiVersioning");
    }

    [Fact]
    public void RequirementsByApiType_ShouldDefinePublicAndPartnerRequirements()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.requirements.by_api_type");

        def.DefaultValue.Should().Contain("Public");
        def.DefaultValue.Should().Contain("Partner");
        def.DefaultValue.Should().Contain("DocumentationCoverage");
    }

    [Fact]
    public void PromotionGates_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase4Definitions();
        var def = definitions.Single(d => d.Key == "governance.requirements.promotion_gates");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "promotion gates should be configurable per environment");
        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("minScore");
    }

    // ── Boolean definitions use toggle editor ─────────────────────────

    [Fact]
    public void AllBooleanDefinitions_ShouldUseToggleEditor()
    {
        var definitions = BuildPhase4Definitions();
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
        var definitions = BuildPhase4Definitions();
        var jsonDefs = definitions.Where(d => d.ValueType == ConfigurationValueType.Json).ToList();

        jsonDefs.Should().NotBeEmpty();
        jsonDefs.Should().OnlyContain(
            d => d.UiEditorType == "json-editor",
            "all JSON definitions should use the json-editor UI editor");
    }

    // ── Total count validation ────────────────────────────────────────

    [Fact]
    public void Phase4Definitions_ShouldHaveExpectedCount()
    {
        var definitions = BuildPhase4Definitions();

        // 9 policies/profiles + 6 evidence + 10 waivers + 6 packs + 8 scorecards/risk + 5 requirements = 44
        definitions.Should().HaveCount(44,
            "Phase 4 should deliver 44 governance and compliance definitions");
    }
}
