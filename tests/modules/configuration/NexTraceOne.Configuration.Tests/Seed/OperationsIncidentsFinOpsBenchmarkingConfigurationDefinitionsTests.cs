using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam as definições de configuração de operações, incidentes,
/// FinOps e benchmarking introduzidas na Fase 6 da parametrização.
/// Garante que todas as chaves estão bem formadas, com categorias, tipos,
/// escopos e valores padrão corretos para o domínio operacional e financeiro.
/// </summary>
public sealed class OperationsIncidentsFinOpsBenchmarkingConfigurationDefinitionsTests
{
    /// <summary>
    /// Constrói as definições Phase 6 de operações, incidentes, FinOps e benchmarking,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase6Definitions() =>
    [
        // Block A — Incident Taxonomy, Severity, Criticality & SLA
        ConfigurationDefinition.Create("incidents.taxonomy.categories", "Incident Categories", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Infrastructure","Application","Security","Data","Network","ThirdParty"]""", uiEditorType: "json-editor", sortOrder: 5000),
        ConfigurationDefinition.Create("incidents.taxonomy.types", "Incident Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Outage","Degradation","Latency","ErrorSpike","SecurityBreach","DataLoss","ConfigDrift"]""", uiEditorType: "json-editor", sortOrder: 5010),
        ConfigurationDefinition.Create("incidents.severity.defaults_by_type", "Default Severity by Incident Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Outage":"Critical","Degradation":"High","Latency":"Medium","ErrorSpike":"High","SecurityBreach":"Critical","DataLoss":"Critical","ConfigDrift":"Low"}""", uiEditorType: "json-editor", sortOrder: 5020),
        ConfigurationDefinition.Create("incidents.severity.defaults_by_category", "Default Severity by Incident Category", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Infrastructure":"High","Application":"Medium","Security":"Critical","Data":"High","Network":"High","ThirdParty":"Medium"}""", uiEditorType: "json-editor", sortOrder: 5030),
        ConfigurationDefinition.Create("incidents.criticality.defaults", "Incident Criticality Defaults", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Outage_Infrastructure":"Critical","SecurityBreach_Security":"Critical","Degradation_Application":"High","Latency_Network":"Medium","ConfigDrift_Application":"Low"}""", uiEditorType: "json-editor", sortOrder: 5040),
        ConfigurationDefinition.Create("incidents.severity.mapping", "Severity Mapping Table", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626","weight":4},"High":{"label":"High","color":"#F59E0B","weight":3},"Medium":{"label":"Medium","color":"#3B82F6","weight":2},"Low":{"label":"Low","color":"#10B981","weight":1}}""", uiEditorType: "json-editor", sortOrder: 5050),
        ConfigurationDefinition.Create("incidents.sla.by_severity", "SLA by Severity", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Critical":{"acknowledgementMinutes":5,"firstResponseMinutes":15,"resolutionMinutes":240},"High":{"acknowledgementMinutes":15,"firstResponseMinutes":60,"resolutionMinutes":480},"Medium":{"acknowledgementMinutes":60,"firstResponseMinutes":240,"resolutionMinutes":1440},"Low":{"acknowledgementMinutes":240,"firstResponseMinutes":480,"resolutionMinutes":4320}}""", uiEditorType: "json-editor", sortOrder: 5060),
        ConfigurationDefinition.Create("incidents.sla.by_environment", "SLA Adjustments by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"multiplier":1.0},"PreProduction":{"multiplier":2.0},"Staging":{"multiplier":3.0},"Development":{"multiplier":5.0}}""", uiEditorType: "json-editor", sortOrder: 5070),
        ConfigurationDefinition.Create("incidents.sla.production_behavior", "Production Severity Behavior", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":{"autoEscalate":true,"pageOnCall":true,"requirePostMortem":true},"High":{"autoEscalate":true,"pageOnCall":false,"requirePostMortem":true},"Medium":{"autoEscalate":false},"Low":{"autoEscalate":false}}""", uiEditorType: "json-editor", sortOrder: 5080),

        // Block B — Owners, Classification, Correlation & Auto-Incident
        ConfigurationDefinition.Create("incidents.owner.default_by_category", "Default Owner by Category", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Infrastructure":"platform-ops","Application":"service-owner","Security":"security-team","Data":"data-engineering","Network":"network-ops","ThirdParty":"vendor-management"}""", uiEditorType: "json-editor", sortOrder: 5100),
        ConfigurationDefinition.Create("incidents.owner.fallback", "Fallback Incident Owner", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "platform-admin", uiEditorType: "text", sortOrder: 5110),
        ConfigurationDefinition.Create("incidents.classification.auto_enabled", "Automatic Classification Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5120),
        ConfigurationDefinition.Create("incidents.correlation.policy", "Alert-to-Incident Correlation Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"correlateByService":true,"correlateByEnvironment":true,"correlateBySeverity":false,"correlationWindowMinutes":30,"correlationKeyFields":["service","environment","alertType"]}""", uiEditorType: "json-editor", sortOrder: 5130),
        ConfigurationDefinition.Create("incidents.auto_creation.enabled", "Auto-Incident Creation Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5140),
        ConfigurationDefinition.Create("incidents.auto_creation.policy", "Auto-Incident Creation Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"minSeverityForAutoCreate":"High","maxAutoIncidentsPerHour":10,"requireCorrelationMatch":true,"blockedCategories":[]}""", uiEditorType: "json-editor", sortOrder: 5150),
        ConfigurationDefinition.Create("incidents.auto_creation.blocked_environments", "Auto-Incident Blocked Environments", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """[]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 5160),
        ConfigurationDefinition.Create("incidents.enrichment.enabled", "Incident Enrichment Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5170),

        // Block C — Playbooks, Runbooks & Operational Automation
        ConfigurationDefinition.Create("operations.playbook.defaults_by_type", "Default Playbook by Incident Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Outage":"playbook-outage-standard","Degradation":"playbook-degradation-triage","SecurityBreach":"playbook-security-response","DataLoss":"playbook-data-recovery","Latency":"playbook-performance-investigation"}""", uiEditorType: "json-editor", sortOrder: 5200),
        ConfigurationDefinition.Create("operations.runbook.defaults_by_category", "Default Runbook by Category", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Infrastructure":"runbook-infra-ops","Application":"runbook-app-debug","Security":"runbook-sec-incident","Network":"runbook-network-diag"}""", uiEditorType: "json-editor", sortOrder: 5210),
        ConfigurationDefinition.Create("operations.playbook.required_by_environment", "Playbook Required by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":true,"PreProduction":false,"Development":false}""", uiEditorType: "json-editor", sortOrder: 5220),
        ConfigurationDefinition.Create("operations.playbook.required_by_criticality", "Playbook Required by Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5230),
        ConfigurationDefinition.Create("operations.automation.enabled_by_environment", "Automation Enabled by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"autoRestart":false,"autoScale":false,"autoRemediate":false},"PreProduction":{"autoRestart":true,"autoScale":true,"autoRemediate":false},"Development":{"autoRestart":true,"autoScale":true,"autoRemediate":true}}""", uiEditorType: "json-editor", sortOrder: 5240),
        ConfigurationDefinition.Create("operations.automation.blocked_in_production", "Automation Blocked in Production", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["autoRemediate","autoDeleteResources","autoModifyData"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 5250),
        ConfigurationDefinition.Create("operations.automation.by_severity", "Automation Allowed by Severity", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":["autoNotify","autoEscalate"],"High":["autoNotify","autoEscalate","autoRestart"],"Medium":["autoNotify","autoRestart"],"Low":["autoNotify"]}""", uiEditorType: "json-editor", sortOrder: 5260),
        ConfigurationDefinition.Create("operations.postincident.template_enabled", "Post-Incident Template Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5270),

        // Block D — FinOps Budgets & Thresholds
        ConfigurationDefinition.Create("finops.budget.default_currency", "Default Budget Currency", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "USD", validationRules: """{"maxLength":3,"minLength":3}""", uiEditorType: "text", sortOrder: 5300),
        ConfigurationDefinition.Create("finops.budget.by_tenant", "Budget by Tenant", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"default":{"monthlyBudget":10000,"alertOnExceed":true}}""", uiEditorType: "json-editor", sortOrder: 5310),
        ConfigurationDefinition.Create("finops.budget.by_team", "Budget by Team", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"default":{"monthlyBudget":5000,"alertOnExceed":true}}""", uiEditorType: "json-editor", sortOrder: 5320),
        ConfigurationDefinition.Create("finops.budget.by_service", "Budget by Service", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"default":{"monthlyBudget":2000,"alertOnExceed":true}}""", uiEditorType: "json-editor", sortOrder: 5330),
        ConfigurationDefinition.Create("finops.budget.alert_thresholds", "Budget Alert Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """[{"percent":80,"severity":"Low","action":"Notify"},{"percent":90,"severity":"Medium","action":"Notify"},{"percent":100,"severity":"High","action":"NotifyAndBlock"},{"percent":120,"severity":"Critical","action":"Escalate"}]""", uiEditorType: "json-editor", sortOrder: 5340),
        ConfigurationDefinition.Create("finops.budget.by_environment", "Budget by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"monthlyBudget":8000,"hardLimit":true},"PreProduction":{"monthlyBudget":3000,"hardLimit":false},"Development":{"monthlyBudget":1000,"hardLimit":false}}""", uiEditorType: "json-editor", sortOrder: 5350),
        ConfigurationDefinition.Create("finops.budget.periodicity", "Budget Periodicity", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Monthly", validationRules: """{"enum":["Monthly","Quarterly","Yearly"]}""", uiEditorType: "select", sortOrder: 5360),
        ConfigurationDefinition.Create("finops.budget.rollover_enabled", "Budget Rollover Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 5370),

        // Block E — Anomaly, Waste & Financial Recommendations
        ConfigurationDefinition.Create("finops.anomaly.detection_enabled", "Cost Anomaly Detection Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5400),
        ConfigurationDefinition.Create("finops.anomaly.thresholds", "Anomaly Detection Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"warning":20,"high":50,"critical":100}""", uiEditorType: "json-editor", sortOrder: 5410),
        ConfigurationDefinition.Create("finops.anomaly.comparison_window_days", "Anomaly Comparison Window (Days)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "30", validationRules: """{"min":7,"max":90}""", uiEditorType: "text", sortOrder: 5420),
        ConfigurationDefinition.Create("finops.waste.detection_enabled", "Waste Detection Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5430),
        ConfigurationDefinition.Create("finops.waste.thresholds", "Waste Detection Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"idleResourcePercent":90,"underutilizationPercent":20,"unusedDaysThreshold":14}""", uiEditorType: "json-editor", sortOrder: 5440),
        ConfigurationDefinition.Create("finops.waste.categories", "Waste Categories", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["IdleResources","Overprovisioned","UnattachedStorage","UnusedLicenses","OrphanedResources","OverlappingServices"]""", uiEditorType: "json-editor", sortOrder: 5450),
        ConfigurationDefinition.Create("finops.recommendation.policy", "Financial Recommendation Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"autoRecommend":true,"minSavingsThreshold":50,"showInDashboard":true,"notifyOnHighSavings":true,"highSavingsThreshold":500}""", uiEditorType: "json-editor", sortOrder: 5460),
        ConfigurationDefinition.Create("finops.notification.policy", "Financial Notification Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"notifyOnAnomaly":true,"notifyOnBudgetBreach":true,"notifyOnWasteDetected":true,"notifyOnRecommendation":false,"digestFrequency":"Weekly"}""", uiEditorType: "json-editor", sortOrder: 5470),
        ConfigurationDefinition.Create("finops.anomaly.by_criticality", "Anomaly Policy by Service Criticality", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"critical":{"warningDeviation":10,"autoEscalate":true},"standard":{"warningDeviation":20,"autoEscalate":false}}""", uiEditorType: "json-editor", sortOrder: 5480),

        // Block F — Benchmarking Weights, Thresholds & Formulas
        ConfigurationDefinition.Create("benchmarking.score.weights", "Benchmarking Score Weights", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"reliability":25,"performance":20,"costEfficiency":20,"security":15,"operationalExcellence":10,"documentation":10}""", uiEditorType: "json-editor", sortOrder: 5500),
        ConfigurationDefinition.Create("benchmarking.score.thresholds", "Benchmarking Score Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"Excellent":90,"Good":70,"NeedsImprovement":50,"Critical":0}""", uiEditorType: "json-editor", sortOrder: 5510),
        ConfigurationDefinition.Create("benchmarking.score.bands", "Benchmarking Score Bands", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Excellent":{"label":"Excellent","color":"#10B981","minScore":90},"Good":{"label":"Good","color":"#3B82F6","minScore":70},"NeedsImprovement":{"label":"Needs Improvement","color":"#F59E0B","minScore":50},"Critical":{"label":"Critical","color":"#DC2626","minScore":0}}""", uiEditorType: "json-editor", sortOrder: 5520),
        ConfigurationDefinition.Create("benchmarking.formula.components", "Benchmarking Formula Components", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"reliability":{"uptimeWeight":0.5,"mttrWeight":0.3,"incidentRateWeight":0.2},"performance":{"p99LatencyWeight":0.4,"throughputWeight":0.3,"errorRateWeight":0.3},"costEfficiency":{"budgetAdherenceWeight":0.5,"wasteReductionWeight":0.3,"optimizationAdoptionWeight":0.2}}""", uiEditorType: "json-editor", sortOrder: 5530),
        ConfigurationDefinition.Create("benchmarking.score.by_dimension", "Score Weights by Dimension", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"reliability":{"uptime":50,"mttr":30,"incidentRate":20},"performance":{"latency":40,"throughput":30,"errorRate":30},"costEfficiency":{"budgetAdherence":50,"waste":30,"optimization":20},"security":{"vulnerabilities":40,"compliance":30,"patchCurrency":30},"operationalExcellence":{"automation":40,"documentation":30,"changeSuccess":30},"documentation":{"coverage":50,"freshness":30,"quality":20}}""", uiEditorType: "json-editor", sortOrder: 5540),
        ConfigurationDefinition.Create("benchmarking.thresholds.by_environment", "Benchmarking Thresholds by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"Excellent":95,"Good":80,"NeedsImprovement":60,"Critical":0},"Development":{"Excellent":80,"Good":60,"NeedsImprovement":40,"Critical":0}}""", uiEditorType: "json-editor", sortOrder: 5550),
        ConfigurationDefinition.Create("benchmarking.missing_data.policy", "Missing Data Calculation Policy", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "UseDefault", validationRules: """{"enum":["SkipDimension","UseDefault","Penalize"]}""", uiEditorType: "select", sortOrder: 5560),
        ConfigurationDefinition.Create("benchmarking.missing_data.default_score", "Missing Data Default Score", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "50", validationRules: """{"min":0,"max":100}""", uiEditorType: "text", sortOrder: 5570),

        // Block G — Functional Health/Anomaly/Drift Thresholds
        ConfigurationDefinition.Create("operations.health.anomaly_thresholds", "Operational Health Anomaly Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """{"errorRateWarning":1.0,"errorRateCritical":5.0,"latencyP99Warning":500,"latencyP99Critical":2000,"availabilityWarning":99.5,"availabilityCritical":99.0}""", uiEditorType: "json-editor", sortOrder: 5600),
        ConfigurationDefinition.Create("operations.health.drift_detection_enabled", "Configuration Drift Detection Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 5610),
        ConfigurationDefinition.Create("operations.health.drift_thresholds", "Drift Detection Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"minor":{"maxDriftedConfigs":5},"major":{"maxDriftedConfigs":15},"critical":{"maxDriftedConfigs":30}}""", uiEditorType: "json-editor", sortOrder: 5620),
    ];

    // ── All Phase 6 definitions have unique keys ──────────────────────

    [Fact]
    public void Phase6Definitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase6Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("each configuration definition key must be unique");
    }

    // ── All Phase 6 definitions have unique sortOrders ─────────────────

    [Fact]
    public void Phase6Definitions_ShouldHaveUniqueSortOrders()
    {
        var definitions = BuildPhase6Definitions();
        var sortOrders = definitions.Select(d => d.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("each definition must have a unique sortOrder");
    }

    // ── All Phase 6 definitions are Functional category ───────────────

    [Fact]
    public void Phase6Definitions_ShouldAllBeFunctionalCategory()
    {
        var definitions = BuildPhase6Definitions();

        definitions.Should().OnlyContain(
            d => d.Category == ConfigurationCategory.Functional,
            "operations, incidents, FinOps and benchmarking definitions should be Functional category");
    }

    // ── All definitions use correct prefix ─────────────────────────────

    [Fact]
    public void Phase6Definitions_ShouldHaveCorrectKeyPrefix()
    {
        var definitions = BuildPhase6Definitions();

        definitions.Should().OnlyContain(
            d => d.Key.StartsWith("incidents.") || d.Key.StartsWith("operations.") || d.Key.StartsWith("finops.") || d.Key.StartsWith("benchmarking."),
            "Phase 6 definitions must use incidents.*, operations.*, finops.* or benchmarking.* key prefix");
    }

    // ── Sort orders are in the 5000+ range ────────────────────────────

    [Fact]
    public void Phase6Definitions_SortOrdersShouldBeInPhase6Range()
    {
        var definitions = BuildPhase6Definitions();

        definitions.Should().OnlyContain(
            d => d.SortOrder >= 5000 && d.SortOrder <= 5999,
            "Phase 6 definitions should have sortOrders in the 5000-5999 range");
    }

    // ── Incident taxonomy definitions ─────────────────────────────────

    [Fact]
    public void IncidentCategories_ShouldIncludeStandardCategories()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.taxonomy.categories");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().Contain("Infrastructure");
        def.DefaultValue.Should().Contain("Security");
        def.DefaultValue.Should().Contain("Application");
    }

    [Fact]
    public void IncidentTypes_ShouldIncludeStandardTypes()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.taxonomy.types");

        def.DefaultValue.Should().Contain("Outage");
        def.DefaultValue.Should().Contain("SecurityBreach");
        def.DefaultValue.Should().Contain("DataLoss");
    }

    [Fact]
    public void SeverityByType_ShouldMapOutageToCritical()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.severity.defaults_by_type");

        def.DefaultValue.Should().Contain("Outage");
        def.DefaultValue.Should().Contain("Critical");
    }

    // ── SLA definitions ───────────────────────────────────────────────

    [Fact]
    public void SlaBySeverity_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.sla.by_severity");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment,
            "SLA should vary by environment");
        def.DefaultValue.Should().Contain("acknowledgementMinutes");
        def.DefaultValue.Should().Contain("resolutionMinutes");
    }

    [Fact]
    public void SlaByEnvironment_ShouldHaveProductionMultiplier()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.sla.by_environment");

        def.DefaultValue.Should().Contain("Production");
        def.DefaultValue.Should().Contain("1.0");
    }

    // ── Owners and correlation ────────────────────────────────────────

    [Fact]
    public void FallbackOwner_ShouldDefaultToPlatformAdmin()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.owner.fallback");

        def.DefaultValue.Should().Be("platform-admin");
        def.ValueType.Should().Be(ConfigurationValueType.String);
    }

    [Fact]
    public void AutoCreationEnabled_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.auto_creation.enabled");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
        def.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void AutoCreationBlockedEnvironments_ShouldBeSystemOnly()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "incidents.auto_creation.blocked_environments");

        def.AllowedScopes.Should().HaveCount(1);
        def.AllowedScopes.Should().Contain(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse();
    }

    // ── Playbooks & automation ────────────────────────────────────────

    [Fact]
    public void PlaybookDefaults_ShouldMapIncidentTypesToPlaybooks()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "operations.playbook.defaults_by_type");

        def.DefaultValue.Should().Contain("Outage");
        def.DefaultValue.Should().Contain("playbook-outage-standard");
    }

    [Fact]
    public void AutomationBlockedInProduction_ShouldBeSystemOnly()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "operations.automation.blocked_in_production");

        def.AllowedScopes.Should().HaveCount(1);
        def.AllowedScopes.Should().Contain(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse();
    }

    // ── FinOps budget definitions ─────────────────────────────────────

    [Fact]
    public void BudgetCurrency_ShouldDefault_USD()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.budget.default_currency");

        def.DefaultValue.Should().Be("USD");
        def.ValidationRules.Should().Contain("maxLength");
    }

    [Fact]
    public void BudgetAlertThresholds_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.budget.alert_thresholds");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
        def.DefaultValue.Should().Contain("80");
        def.DefaultValue.Should().Contain("90");
        def.DefaultValue.Should().Contain("100");
    }

    [Fact]
    public void BudgetPeriodicity_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.budget.periodicity");

        def.ValidationRules.Should().Contain("Monthly");
        def.ValidationRules.Should().Contain("Quarterly");
        def.ValidationRules.Should().Contain("Yearly");
        def.DefaultValue.Should().Be("Monthly");
    }

    [Fact]
    public void BudgetRollover_ShouldDefaultToFalse()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.budget.rollover_enabled");

        def.DefaultValue.Should().Be("false");
    }

    // ── Anomaly & waste ───────────────────────────────────────────────

    [Fact]
    public void AnomalyDetection_ShouldDefaultToEnabled()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.anomaly.detection_enabled");

        def.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void AnomalyComparisonWindow_ShouldHaveMinMaxValidation()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.anomaly.comparison_window_days");

        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
        def.DefaultValue.Should().Be("30");
    }

    [Fact]
    public void WasteCategories_ShouldIncludeStandardCategories()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "finops.waste.categories");

        def.DefaultValue.Should().Contain("IdleResources");
        def.DefaultValue.Should().Contain("Overprovisioned");
        def.DefaultValue.Should().Contain("OrphanedResources");
    }

    // ── Benchmarking ──────────────────────────────────────────────────

    [Fact]
    public void BenchmarkingWeights_ShouldIncludeAllDimensions()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "benchmarking.score.weights");

        def.DefaultValue.Should().Contain("reliability");
        def.DefaultValue.Should().Contain("performance");
        def.DefaultValue.Should().Contain("costEfficiency");
        def.DefaultValue.Should().Contain("security");
    }

    [Fact]
    public void BenchmarkingScoreThresholds_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "benchmarking.score.thresholds");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
    }

    [Fact]
    public void BenchmarkingMissingDataPolicy_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "benchmarking.missing_data.policy");

        def.ValidationRules.Should().Contain("SkipDimension");
        def.ValidationRules.Should().Contain("UseDefault");
        def.ValidationRules.Should().Contain("Penalize");
        def.DefaultValue.Should().Be("UseDefault");
    }

    [Fact]
    public void BenchmarkingDefaultScore_ShouldHaveMinMaxValidation()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "benchmarking.missing_data.default_score");

        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
        def.DefaultValue.Should().Be("50");
    }

    // ── Operational health ────────────────────────────────────────────

    [Fact]
    public void HealthAnomalyThresholds_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "operations.health.anomaly_thresholds");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
        def.DefaultValue.Should().Contain("errorRateWarning");
    }

    [Fact]
    public void DriftDetection_ShouldDefaultToEnabled()
    {
        var definitions = BuildPhase6Definitions();
        var def = definitions.Single(d => d.Key == "operations.health.drift_detection_enabled");

        def.DefaultValue.Should().Be("true");
    }

    // ── Boolean definitions use toggle editor ─────────────────────────

    [Fact]
    public void AllBooleanDefinitions_ShouldUseToggleEditor()
    {
        var definitions = BuildPhase6Definitions();
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
        var definitions = BuildPhase6Definitions();
        var jsonDefs = definitions.Where(d => d.ValueType == ConfigurationValueType.Json).ToList();

        jsonDefs.Should().NotBeEmpty();
        jsonDefs.Should().OnlyContain(
            d => d.UiEditorType == "json-editor",
            "all JSON definitions should use the json-editor UI editor");
    }

    // ── Total count validation ────────────────────────────────────────

    [Fact]
    public void Phase6Definitions_ShouldHaveExpectedCount()
    {
        var definitions = BuildPhase6Definitions();

        // 9 incident taxonomy/SLA + 8 owners/correlation + 8 playbooks/automation + 8 budgets + 9 anomaly/waste + 8 benchmarking + 3 health/drift = 53
        definitions.Should().HaveCount(53,
            "Phase 6 should deliver 53 operations, incidents, FinOps and benchmarking definitions");
    }
}
