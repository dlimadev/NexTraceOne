using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam a capacidade de criar definições de configuração representativas
/// do Phase 1, simulando os mesmos parâmetros utilizados pelo seeder.
/// Valida padrões de chave, categorias e integridade de configurações por bloco.
/// </summary>
public sealed class ConfigurationDefinitionSeederTests
{
    /// <summary>
    /// Constrói a lista representativa de definições Phase 1,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase1Definitions() =>
    [
        // Block A — Cross-cutting
        ConfigurationDefinition.Create("notifications.enabled", "Notifications Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 100),
        ConfigurationDefinition.Create("notifications.email.enabled", "Email Notifications Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 110),
        ConfigurationDefinition.Create("ai.default_temperature", "AI Default Temperature", ConfigurationCategory.Functional, ConfigurationValueType.Decimal, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User], defaultValue: "0.7", validationRules: """{"min": 0.0, "max": 2.0}""", uiEditorType: "text", sortOrder: 200),
        ConfigurationDefinition.Create("governance.approval_required", "Governance Approval Required", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Team], defaultValue: "true", uiEditorType: "toggle", sortOrder: 300),
        ConfigurationDefinition.Create("platform.maintenance_mode", "Platform Maintenance Mode", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System], defaultValue: "false", isInheritable: false, uiEditorType: "toggle", sortOrder: 400),
        ConfigurationDefinition.Create("security.session_timeout_minutes", "Session Timeout (Minutes)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "60", validationRules: """{"min": 5, "max": 1440}""", uiEditorType: "text", sortOrder: 500),
        ConfigurationDefinition.Create("integration.webhook_secret", "Webhook Secret", ConfigurationCategory.SensitiveOperational, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], isSensitive: true, uiEditorType: "text", sortOrder: 600),

        // Block B — Instance
        ConfigurationDefinition.Create("instance.name", "Instance Name", ConfigurationCategory.Bootstrap, ConfigurationValueType.String, [ConfigurationScope.System], defaultValue: "NexTraceOne", uiEditorType: "text", sortOrder: 1000),
        ConfigurationDefinition.Create("instance.commercial_name", "Instance Commercial Name", ConfigurationCategory.Bootstrap, ConfigurationValueType.String, [ConfigurationScope.System], defaultValue: "NexTraceOne Platform", uiEditorType: "text", sortOrder: 1010),
        ConfigurationDefinition.Create("instance.default_language", "Instance Default Language", ConfigurationCategory.Bootstrap, ConfigurationValueType.String, [ConfigurationScope.System], defaultValue: "en", validationRules: """{"enum":["en","pt-BR","pt-PT","es"]}""", uiEditorType: "select", sortOrder: 1020),
        ConfigurationDefinition.Create("instance.default_timezone", "Instance Default Timezone", ConfigurationCategory.Bootstrap, ConfigurationValueType.String, [ConfigurationScope.System], defaultValue: "UTC", uiEditorType: "text", sortOrder: 1030),

        // Block C — Tenant
        ConfigurationDefinition.Create("tenant.display_name", "Tenant Display Name", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.Tenant], uiEditorType: "text", sortOrder: 1100),
        ConfigurationDefinition.Create("tenant.default_language", "Tenant Default Language", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "en", isInheritable: true, validationRules: """{"enum":["en","pt-BR","pt-PT","es"]}""", uiEditorType: "select", sortOrder: 1110),
        ConfigurationDefinition.Create("tenant.max_users", "Tenant Maximum Users", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "100", validationRules: """{"min":1,"max":10000}""", uiEditorType: "text", sortOrder: 1140),

        // Block D — Environment
        ConfigurationDefinition.Create("environment.classification", "Environment Classification", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.Environment], validationRules: """{"enum":["Development","Test","QA","PreProduction","Production","Lab"]}""", uiEditorType: "select", sortOrder: 1200),
        ConfigurationDefinition.Create("environment.is_production", "Environment Is Production", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.Environment], defaultValue: "false", isInheritable: false, uiEditorType: "toggle", sortOrder: 1210),
        ConfigurationDefinition.Create("environment.criticality", "Environment Criticality", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.Environment, ConfigurationScope.System], defaultValue: "medium", validationRules: """{"enum":["low","medium","high","critical"]}""", uiEditorType: "select", sortOrder: 1220),

        // Block F — Branding
        ConfigurationDefinition.Create("branding.logo_url", "Branding Logo URL", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], uiEditorType: "text", sortOrder: 1300),
        ConfigurationDefinition.Create("branding.accent_color", "Branding Accent Color", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "#3B82F6", validationRules: """{"pattern":"^#[0-9a-fA-F]{6}$"}""", uiEditorType: "text", sortOrder: 1320),

        // Block G — Feature Flags
        ConfigurationDefinition.Create("feature.module.catalog.enabled", "Feature: Service Catalog", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 1400),
        ConfigurationDefinition.Create("feature.module.contracts.enabled", "Feature: Contract Governance", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 1410),
        ConfigurationDefinition.Create("feature.preview.ai_agents.enabled", "Preview: AI Agents", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 1490),

        // Block H — Policies
        ConfigurationDefinition.Create("policy.environment.allow_automation", "Policy: Allow Automation", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.Environment, ConfigurationScope.System], defaultValue: "true", isInheritable: true, uiEditorType: "toggle", sortOrder: 1500),
        ConfigurationDefinition.Create("policy.environment.require_approval_for_changes", "Policy: Require Approval for Changes", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.Environment, ConfigurationScope.System], defaultValue: "false", isInheritable: true, uiEditorType: "toggle", sortOrder: 1530),
    ];

    // ── Instance definitions ───────────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldContainInstanceDefinitions()
    {
        var definitions = BuildPhase1Definitions();
        var instanceDefs = definitions.Where(d => d.Key.StartsWith("instance.")).ToList();

        instanceDefs.Should().NotBeEmpty();
        instanceDefs.Should().OnlyContain(d => d.Category == ConfigurationCategory.Bootstrap);
        instanceDefs.Should().OnlyContain(d => d.AllowedScopes.Contains(ConfigurationScope.System));
    }

    // ── Tenant definitions ─────────────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldContainTenantDefinitions()
    {
        var definitions = BuildPhase1Definitions();
        var tenantDefs = definitions.Where(d => d.Key.StartsWith("tenant.")).ToList();

        tenantDefs.Should().NotBeEmpty();
        tenantDefs.Should().OnlyContain(d => d.AllowedScopes.Contains(ConfigurationScope.Tenant));
    }

    // ── Environment definitions ────────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldContainEnvironmentDefinitions()
    {
        var definitions = BuildPhase1Definitions();
        var envDefs = definitions.Where(d => d.Key.StartsWith("environment.")).ToList();

        envDefs.Should().NotBeEmpty();
        envDefs.Should().OnlyContain(d => d.AllowedScopes.Contains(ConfigurationScope.Environment));
    }

    // ── Branding definitions ───────────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldContainBrandingDefinitions()
    {
        var definitions = BuildPhase1Definitions();
        var brandingDefs = definitions.Where(d => d.Key.StartsWith("branding.")).ToList();

        brandingDefs.Should().NotBeEmpty();
        brandingDefs.Should().OnlyContain(d =>
            d.AllowedScopes.Contains(ConfigurationScope.System) ||
            d.AllowedScopes.Contains(ConfigurationScope.Tenant));
    }

    // ── Feature flag definitions ───────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldContainFeatureFlagDefinitions()
    {
        var definitions = BuildPhase1Definitions();
        var featureDefs = definitions.Where(d => d.Key.StartsWith("feature.")).ToList();

        featureDefs.Should().NotBeEmpty();
        featureDefs.Should().OnlyContain(d => d.ValueType == ConfigurationValueType.Boolean);
        featureDefs.Should().OnlyContain(d => d.UiEditorType == "toggle");
    }

    // ── Policy definitions ─────────────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldContainPolicyDefinitions()
    {
        var definitions = BuildPhase1Definitions();
        var policyDefs = definitions.Where(d => d.Key.StartsWith("policy.")).ToList();

        policyDefs.Should().NotBeEmpty();
        policyDefs.Should().OnlyContain(d => d.AllowedScopes.Contains(ConfigurationScope.Environment));
        policyDefs.Should().OnlyContain(d => d.IsInheritable);
    }

    // ── Uniqueness and integrity ───────────────────────────────────────

    [Fact]
    public void BuildDefaultDefinitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase1Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("each configuration key must be unique");
    }

    [Fact]
    public void BuildDefaultDefinitions_AllShouldHaveAllowedScopes()
    {
        var definitions = BuildPhase1Definitions();

        definitions.Should().OnlyContain(d => d.AllowedScopes.Length > 0,
            "every definition must have at least one allowed scope");
    }

    // ── Specific definition rules ──────────────────────────────────────

    [Fact]
    public void EnvironmentIsProduction_ShouldBeNonInheritable()
    {
        var definitions = BuildPhase1Definitions();
        var envIsProd = definitions.Single(d => d.Key == "environment.is_production");

        envIsProd.IsInheritable.Should().BeFalse(
            "environment.is_production must not be inherited to prevent accidental production marking");
        envIsProd.DefaultValue.Should().Be("false");
        envIsProd.ValueType.Should().Be(ConfigurationValueType.Boolean);
    }

    [Fact]
    public void PlatformMaintenanceMode_ShouldBeNonInheritableSystemOnly()
    {
        var definitions = BuildPhase1Definitions();
        var maintenance = definitions.Single(d => d.Key == "platform.maintenance_mode");

        maintenance.IsInheritable.Should().BeFalse();
        maintenance.AllowedScopes.Should().BeEquivalentTo(new[] { ConfigurationScope.System });
        maintenance.DefaultValue.Should().Be("false");
    }

    [Fact]
    public void WebhookSecret_ShouldBeSensitive()
    {
        var definitions = BuildPhase1Definitions();
        var secret = definitions.Single(d => d.Key == "integration.webhook_secret");

        secret.IsSensitive.Should().BeTrue();
        secret.Category.Should().Be(ConfigurationCategory.SensitiveOperational);
    }
}
