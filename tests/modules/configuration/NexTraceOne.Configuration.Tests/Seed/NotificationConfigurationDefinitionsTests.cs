using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam as definições de configuração de notificações e comunicações
/// introduzidas na Fase 2 da parametrização.
/// Garante que todas as chaves de notificação estão bem formadas, com categorias,
/// tipos, escopos e valores padrão corretos.
/// </summary>
public sealed class NotificationConfigurationDefinitionsTests
{
    /// <summary>
    /// Constrói as definições Phase 2 de notificações e comunicações,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase2Definitions() =>
    [
        // Types, Categories & Severities
        ConfigurationDefinition.Create("notifications.types.enabled", "Enabled Notification Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: """["IncidentCreated","IncidentEscalated"]""", uiEditorType: "json-editor", sortOrder: 150),
        ConfigurationDefinition.Create("notifications.categories.enabled", "Enabled Notification Categories", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Incident","Approval"]""", uiEditorType: "json-editor", sortOrder: 151),
        ConfigurationDefinition.Create("notifications.severity.default", "Default Notification Severity", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Info", validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""", uiEditorType: "select", sortOrder: 152),
        ConfigurationDefinition.Create("notifications.severity.minimum_for_external", "Minimum Severity for External Channels", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "Warning", validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""", uiEditorType: "select", sortOrder: 153),
        ConfigurationDefinition.Create("notifications.mandatory.types", "Mandatory Notification Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["BreakGlassActivated"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 154),
        ConfigurationDefinition.Create("notifications.mandatory.severities", "Mandatory Notification Severities", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["Critical"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 155),

        // Channels
        ConfigurationDefinition.Create("notifications.channels.inapp.enabled", "In-App Notifications Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 160),
        ConfigurationDefinition.Create("notifications.channels.allowed_by_type", "Allowed Channels per Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 161),
        ConfigurationDefinition.Create("notifications.channels.mandatory_by_severity", "Mandatory Channels per Severity", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """{"Critical":["InApp","Email"]}""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 162),
        ConfigurationDefinition.Create("notifications.channels.mandatory_by_type", "Mandatory Channels per Type", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """{"BreakGlassActivated":["InApp","Email"]}""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 163),
        ConfigurationDefinition.Create("notifications.channels.disabled_in_environment", "Channels Disabled by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.Environment], defaultValue: "[]", isInheritable: false, uiEditorType: "json-editor", sortOrder: 164),

        // Templates
        ConfigurationDefinition.Create("notifications.templates.internal", "Internal Notification Templates", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 170),
        ConfigurationDefinition.Create("notifications.templates.email", "Email Notification Templates", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 171),
        ConfigurationDefinition.Create("notifications.templates.teams", "Teams Notification Templates", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 172),

        // Routing & Fallback
        ConfigurationDefinition.Create("notifications.routing.default_policy", "Default Routing Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"ownerFirst":true}""", uiEditorType: "json-editor", sortOrder: 175),
        ConfigurationDefinition.Create("notifications.routing.fallback_recipients", "Fallback Recipients", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "[]", uiEditorType: "json-editor", sortOrder: 176),
        ConfigurationDefinition.Create("notifications.routing.by_category", "Routing Rules per Category", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 177),
        ConfigurationDefinition.Create("notifications.routing.by_severity", "Routing Rules per Severity", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 178),

        // Preferences, Quiet Hours, Digest & Suppression
        ConfigurationDefinition.Create("notifications.preferences.default_by_tenant", "Default Preferences by Tenant", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"emailEnabled":true}""", uiEditorType: "json-editor", sortOrder: 180),
        ConfigurationDefinition.Create("notifications.preferences.default_by_role", "Default Preferences by Role", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role], defaultValue: "{}", uiEditorType: "json-editor", sortOrder: 181),
        ConfigurationDefinition.Create("notifications.quiet_hours.enabled", "Quiet Hours Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User], defaultValue: "true", uiEditorType: "toggle", sortOrder: 182),
        ConfigurationDefinition.Create("notifications.quiet_hours.bypass_categories", "Categories that Bypass Quiet Hours", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["Incident"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 183),
        ConfigurationDefinition.Create("notifications.digest.enabled", "Digest Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User], defaultValue: "false", uiEditorType: "toggle", sortOrder: 184),
        ConfigurationDefinition.Create("notifications.digest.period_hours", "Digest Period (Hours)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User], defaultValue: "24", validationRules: """{"min":1,"max":168}""", uiEditorType: "text", sortOrder: 185),
        ConfigurationDefinition.Create("notifications.digest.eligible_categories", "Digest Eligible Categories", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Informational"]""", uiEditorType: "json-editor", sortOrder: 186),
        ConfigurationDefinition.Create("notifications.suppress.enabled", "Suppression Rules Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 187),
        ConfigurationDefinition.Create("notifications.suppress.acknowledged_window_minutes", "Acknowledged Suppression Window", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "30", validationRules: """{"min":5,"max":1440}""", uiEditorType: "text", sortOrder: 188),
        ConfigurationDefinition.Create("notifications.acknowledge.required_categories", "Categories Requiring Acknowledgment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["Incident"]""", uiEditorType: "json-editor", sortOrder: 189),

        // Escalation, Dedup & Incident Linkage
        ConfigurationDefinition.Create("notifications.dedup.enabled", "Deduplication Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 190),
        ConfigurationDefinition.Create("notifications.dedup.window_minutes", "Deduplication Window (Minutes)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "5", validationRules: """{"min":1,"max":1440}""", uiEditorType: "text", sortOrder: 191),
        ConfigurationDefinition.Create("notifications.dedup.window_by_category", "Dedup Window per Category", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Incident":10}""", uiEditorType: "json-editor", sortOrder: 192),
        ConfigurationDefinition.Create("notifications.escalation.enabled", "Escalation Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 193),
        ConfigurationDefinition.Create("notifications.escalation.critical_threshold_minutes", "Critical Escalation Threshold", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "30", validationRules: """{"min":5,"max":1440}""", uiEditorType: "text", sortOrder: 194),
        ConfigurationDefinition.Create("notifications.escalation.action_required_threshold_minutes", "ActionRequired Escalation Threshold", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "120", validationRules: """{"min":15,"max":2880}""", uiEditorType: "text", sortOrder: 195),
        ConfigurationDefinition.Create("notifications.escalation.channels", "Escalation Channels", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["InApp","Email"]""", uiEditorType: "json-editor", sortOrder: 196),
        ConfigurationDefinition.Create("notifications.incident_linkage.enabled", "Incident Linkage Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 197),
        ConfigurationDefinition.Create("notifications.incident_linkage.auto_create_enabled", "Auto-Create Incident", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 198),
        ConfigurationDefinition.Create("notifications.incident_linkage.eligible_types", "Incident Linkage Eligible Types", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["IncidentCreated"]""", uiEditorType: "json-editor", sortOrder: 199),
        ConfigurationDefinition.Create("notifications.incident_linkage.correlation_window_minutes", "Incident Correlation Window", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "60", validationRules: """{"min":5,"max":1440}""", uiEditorType: "text", sortOrder: 200),
        ConfigurationDefinition.Create("notifications.grouping.window_minutes", "Grouping Window (Minutes)", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "60", validationRules: """{"min":5,"max":1440}""", uiEditorType: "text", sortOrder: 201),
    ];

    // ── All notification definitions have unique keys ───────────────────

    [Fact]
    public void Phase2Definitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase2Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("each notification configuration key must be unique");
    }

    // ── All notification definitions have unique sort orders ────────────

    [Fact]
    public void Phase2Definitions_ShouldHaveUniqueSortOrders()
    {
        var definitions = BuildPhase2Definitions();
        var sortOrders = definitions.Select(d => d.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("each notification definition must have a unique sort order");
    }

    // ── All notification definitions have at least one scope ────────────

    [Fact]
    public void Phase2Definitions_AllShouldHaveAllowedScopes()
    {
        var definitions = BuildPhase2Definitions();

        definitions.Should().OnlyContain(d => d.AllowedScopes.Length > 0,
            "every notification definition must have at least one allowed scope");
    }

    // ── All notification definitions are Functional category ────────────

    [Fact]
    public void Phase2Definitions_AllShouldBeFunctionalCategory()
    {
        var definitions = BuildPhase2Definitions();

        definitions.Should().OnlyContain(d => d.Category == ConfigurationCategory.Functional,
            "notification configuration definitions should be functional, not bootstrap or sensitive");
    }

    // ── All notification definitions start with "notifications." ────────

    [Fact]
    public void Phase2Definitions_AllKeysShouldStartWithNotifications()
    {
        var definitions = BuildPhase2Definitions();

        definitions.Should().OnlyContain(d => d.Key.StartsWith("notifications."),
            "all Phase 2 definitions should be in the notifications namespace");
    }

    // ── Mandatory types definition is system-only and non-inheritable ───

    [Fact]
    public void MandatoryTypes_ShouldBeSystemOnlyAndNonInheritable()
    {
        var definitions = BuildPhase2Definitions();
        var mandatory = definitions.Single(d => d.Key == "notifications.mandatory.types");

        mandatory.AllowedScopes.Should().BeEquivalentTo(new[] { ConfigurationScope.System });
        mandatory.IsInheritable.Should().BeFalse(
            "mandatory notification types should not be overridden by child scopes");
    }

    // ── Mandatory severities definition is system-only and non-inheritable ──

    [Fact]
    public void MandatorySeverities_ShouldBeSystemOnlyAndNonInheritable()
    {
        var definitions = BuildPhase2Definitions();
        var mandatory = definitions.Single(d => d.Key == "notifications.mandatory.severities");

        mandatory.AllowedScopes.Should().BeEquivalentTo(new[] { ConfigurationScope.System });
        mandatory.IsInheritable.Should().BeFalse();
    }

    // ── Channels mandatory definitions are system-only ──────────────────

    [Fact]
    public void MandatoryChannelsBySeverity_ShouldBeSystemOnly()
    {
        var definitions = BuildPhase2Definitions();
        var def = definitions.Single(d => d.Key == "notifications.channels.mandatory_by_severity");

        def.AllowedScopes.Should().BeEquivalentTo(new[] { ConfigurationScope.System });
        def.IsInheritable.Should().BeFalse();
        def.ValueType.Should().Be(ConfigurationValueType.Json);
    }

    // ── Quiet hours bypass categories is system-only ────────────────────

    [Fact]
    public void QuietHoursBypassCategories_ShouldBeSystemOnly()
    {
        var definitions = BuildPhase2Definitions();
        var def = definitions.Single(d => d.Key == "notifications.quiet_hours.bypass_categories");

        def.AllowedScopes.Should().BeEquivalentTo(new[] { ConfigurationScope.System });
        def.IsInheritable.Should().BeFalse();
    }

    // ── Escalation thresholds have correct default values ───────────────

    [Fact]
    public void EscalationThresholds_ShouldHaveCorrectDefaults()
    {
        var definitions = BuildPhase2Definitions();

        var critical = definitions.Single(d => d.Key == "notifications.escalation.critical_threshold_minutes");
        critical.DefaultValue.Should().Be("30");
        critical.ValueType.Should().Be(ConfigurationValueType.Integer);

        var actionRequired = definitions.Single(d => d.Key == "notifications.escalation.action_required_threshold_minutes");
        actionRequired.DefaultValue.Should().Be("120");
        actionRequired.ValueType.Should().Be(ConfigurationValueType.Integer);
    }

    // ── Dedup window has correct defaults ───────────────────────────────

    [Fact]
    public void DedupWindow_ShouldHaveCorrectDefaults()
    {
        var definitions = BuildPhase2Definitions();
        var dedup = definitions.Single(d => d.Key == "notifications.dedup.window_minutes");

        dedup.DefaultValue.Should().Be("5");
        dedup.ValueType.Should().Be(ConfigurationValueType.Integer);
    }

    // ── Incident linkage is disabled by default ─────────────────────────

    [Fact]
    public void IncidentLinkage_ShouldBeDisabledByDefault()
    {
        var definitions = BuildPhase2Definitions();

        var linkage = definitions.Single(d => d.Key == "notifications.incident_linkage.enabled");
        linkage.DefaultValue.Should().Be("false",
            "incident linkage should be opt-in due to safety concerns");

        var autoCreate = definitions.Single(d => d.Key == "notifications.incident_linkage.auto_create_enabled");
        autoCreate.DefaultValue.Should().Be("false",
            "auto-creation of incidents must be explicitly enabled");
    }

    // ── Digest disabled by default ──────────────────────────────────────

    [Fact]
    public void Digest_ShouldBeDisabledByDefault()
    {
        var definitions = BuildPhase2Definitions();
        var digest = definitions.Single(d => d.Key == "notifications.digest.enabled");

        digest.DefaultValue.Should().Be("false");
    }

    // ── Quiet hours enabled by default ──────────────────────────────────

    [Fact]
    public void QuietHours_ShouldBeEnabledByDefault()
    {
        var definitions = BuildPhase2Definitions();
        var qh = definitions.Single(d => d.Key == "notifications.quiet_hours.enabled");

        qh.DefaultValue.Should().Be("true");
        qh.AllowedScopes.Should().Contain(ConfigurationScope.User,
            "users should be able to configure their own quiet hours");
    }

    // ── Template definitions use JSON type ──────────────────────────────

    [Fact]
    public void TemplateDefinitions_ShouldUseJsonType()
    {
        var definitions = BuildPhase2Definitions();
        var templates = definitions.Where(d => d.Key.StartsWith("notifications.templates.")).ToList();

        templates.Should().HaveCount(3, "should have internal, email and teams templates");
        templates.Should().OnlyContain(d => d.ValueType == ConfigurationValueType.Json);
        templates.Should().OnlyContain(d => d.UiEditorType == "json-editor");
    }

    // ── Routing definitions support tenant scope ────────────────────────

    [Fact]
    public void RoutingDefinitions_ShouldSupportTenantScope()
    {
        var definitions = BuildPhase2Definitions();
        var routing = definitions.Where(d => d.Key.StartsWith("notifications.routing.")).ToList();

        routing.Should().NotBeEmpty();
        routing.Should().OnlyContain(d => d.AllowedScopes.Contains(ConfigurationScope.Tenant));
    }

    // ── Channels disabled by environment is environment-scoped only ─────

    [Fact]
    public void ChannelsDisabledInEnvironment_ShouldBeEnvironmentScoped()
    {
        var definitions = BuildPhase2Definitions();
        var def = definitions.Single(d => d.Key == "notifications.channels.disabled_in_environment");

        def.AllowedScopes.Should().BeEquivalentTo(new[] { ConfigurationScope.Environment });
        def.IsInheritable.Should().BeFalse();
    }

    // ── Suppression acknowledged window has validation rules ────────────

    [Fact]
    public void SuppressionWindow_ShouldHaveValidationRules()
    {
        var definitions = BuildPhase2Definitions();
        var def = definitions.Single(d => d.Key == "notifications.suppress.acknowledged_window_minutes");

        def.ValidationRules.Should().NotBeNullOrWhiteSpace();
        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
    }

    // ── All Boolean definitions have toggle editor ──────────────────────

    [Fact]
    public void BooleanDefinitions_ShouldHaveToggleEditor()
    {
        var definitions = BuildPhase2Definitions();
        var booleans = definitions.Where(d => d.ValueType == ConfigurationValueType.Boolean).ToList();

        booleans.Should().OnlyContain(d => d.UiEditorType == "toggle",
            "all boolean notification definitions should use toggle editor");
    }

    // ── All JSON definitions have json-editor ───────────────────────────

    [Fact]
    public void JsonDefinitions_ShouldHaveJsonEditor()
    {
        var definitions = BuildPhase2Definitions();
        var jsons = definitions.Where(d => d.ValueType == ConfigurationValueType.Json).ToList();

        jsons.Should().OnlyContain(d => d.UiEditorType == "json-editor",
            "all JSON notification definitions should use json-editor");
    }

    // ── Count of Phase 2 definitions ────────────────────────────────────

    [Fact]
    public void Phase2Definitions_ShouldHaveExpectedCount()
    {
        var definitions = BuildPhase2Definitions();

        definitions.Count.Should().BeGreaterThanOrEqualTo(38,
            "Phase 2 should deliver at least 38 notification configuration definitions");
    }
}
