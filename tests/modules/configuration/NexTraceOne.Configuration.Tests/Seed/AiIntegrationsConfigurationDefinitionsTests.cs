using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Seed;

/// <summary>
/// Testes que validam as definições de configuração de IA e integrações
/// introduzidas na Fase 7 da parametrização.
/// Garante que todas as chaves estão bem formadas, com categorias, tipos,
/// escopos e valores padrão corretos para o domínio de IA e integrações.
/// </summary>
public sealed class AiIntegrationsConfigurationDefinitionsTests
{
    /// <summary>
    /// Constrói as definições Phase 7 de IA e integrações,
    /// replicando os mesmos parâmetros do seeder para validação em memória.
    /// </summary>
    private static List<ConfigurationDefinition> BuildPhase7Definitions() =>
    [
        // Block A — AI Provider & Model Enablement
        ConfigurationDefinition.Create("ai.providers.enabled", "Enabled AI Providers", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["OpenAI","AzureOpenAI","Internal"]""", uiEditorType: "json-editor", sortOrder: 6000),
        ConfigurationDefinition.Create("ai.models.enabled", "Enabled AI Models", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["gpt-4o","gpt-4o-mini","gpt-3.5-turbo","internal-llm"]""", uiEditorType: "json-editor", sortOrder: 6010),
        ConfigurationDefinition.Create("ai.providers.default_by_capability", "Default Provider by Capability", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"chat":"OpenAI","analysis":"AzureOpenAI","classification":"Internal","draftGeneration":"OpenAI","retrievalAugmented":"AzureOpenAI","codeReview":"OpenAI"}""", uiEditorType: "json-editor", sortOrder: 6020),
        ConfigurationDefinition.Create("ai.models.default_by_capability", "Default Model by Capability", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"chat":"gpt-4o","analysis":"gpt-4o","classification":"internal-llm","draftGeneration":"gpt-4o","retrievalAugmented":"gpt-4o","codeReview":"gpt-4o-mini"}""", uiEditorType: "json-editor", sortOrder: 6030),
        ConfigurationDefinition.Create("ai.providers.fallback_order", "Provider Fallback Order", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["AzureOpenAI","OpenAI","Internal"]""", uiEditorType: "json-editor", sortOrder: 6040),
        ConfigurationDefinition.Create("ai.usage.allow_external", "Allow External AI Usage", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment], defaultValue: "true", uiEditorType: "toggle", sortOrder: 6050),
        ConfigurationDefinition.Create("ai.usage.blocked_environments", "AI Blocked Environments", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """[]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 6060),
        ConfigurationDefinition.Create("ai.usage.internal_only_capabilities", "Internal-Only AI Capabilities", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["classification"]""", uiEditorType: "json-editor", sortOrder: 6070),

        // Block B — AI Budgets, Quotas & Usage Policies
        ConfigurationDefinition.Create("ai.budget.by_user", "AI Token Budget by User", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"monthlyTokens":100000,"alertOnExceed":true}""", uiEditorType: "json-editor", sortOrder: 6100),
        ConfigurationDefinition.Create("ai.budget.by_team", "AI Token Budget by Team", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"monthlyTokens":500000,"alertOnExceed":true}""", uiEditorType: "json-editor", sortOrder: 6110),
        ConfigurationDefinition.Create("ai.budget.by_tenant", "AI Token Budget by Tenant", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"monthlyTokens":2000000,"alertOnExceed":true,"hardLimit":false}""", uiEditorType: "json-editor", sortOrder: 6120),
        ConfigurationDefinition.Create("ai.quota.by_capability", "AI Quota by Capability", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"chat":{"dailyTokens":50000},"analysis":{"dailyTokens":100000},"draftGeneration":{"dailyTokens":30000},"retrievalAugmented":{"dailyTokens":80000}}""", uiEditorType: "json-editor", sortOrder: 6130),
        ConfigurationDefinition.Create("ai.usage.limits_by_environment", "AI Usage Limits by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"dailyTokens":500000},"PreProduction":{"dailyTokens":100000},"Development":{"dailyTokens":50000}}""", uiEditorType: "json-editor", sortOrder: 6140),
        ConfigurationDefinition.Create("ai.budget.exceed_policy", "Budget Exceed Policy", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Warn", validationRules: """{"enum":["Warn","Block","Throttle"]}""", uiEditorType: "select", sortOrder: 6150),
        ConfigurationDefinition.Create("ai.budget.warning_thresholds", "AI Budget Warning Thresholds", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """[{"percent":70,"severity":"Low"},{"percent":85,"severity":"Medium"},{"percent":95,"severity":"High"},{"percent":100,"severity":"Critical"}]""", uiEditorType: "json-editor", sortOrder: 6160),

        // Block C — Retention, Audit, Prompts & Retrieval
        ConfigurationDefinition.Create("ai.retention.conversation_days", "AI Conversation Retention Days", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "90", validationRules: """{"min":1,"max":365}""", uiEditorType: "text", sortOrder: 6200),
        ConfigurationDefinition.Create("ai.retention.artifact_days", "AI Artifact Retention Days", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "180", validationRules: """{"min":1,"max":730}""", uiEditorType: "text", sortOrder: 6210),
        ConfigurationDefinition.Create("ai.audit.level", "AI Audit Level", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Standard", validationRules: """{"enum":["Minimal","Standard","Full"]}""", uiEditorType: "select", sortOrder: 6220),
        ConfigurationDefinition.Create("ai.audit.log_prompts", "Log AI Prompts", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 6230),
        ConfigurationDefinition.Create("ai.audit.log_responses", "Log AI Responses", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "false", uiEditorType: "toggle", sortOrder: 6240),
        ConfigurationDefinition.Create("ai.prompts.base_by_capability", "Base Prompts by Capability", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"chat":"You are NexTraceOne AI Assistant, a helpful operational intelligence assistant.","analysis":"You are an expert operational analyst. Analyze the data provided and give actionable insights.","classification":"Classify the following operational event into the appropriate category and severity.","draftGeneration":"Generate a professional draft based on the provided context and requirements."}""", uiEditorType: "json-editor", sortOrder: 6250),
        ConfigurationDefinition.Create("ai.prompts.allow_tenant_override", "Allow Tenant Prompt Override", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System], defaultValue: "false", isInheritable: false, uiEditorType: "toggle", sortOrder: 6260),
        ConfigurationDefinition.Create("ai.retrieval.top_k", "Retrieval Top-K", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "5", validationRules: """{"min":1,"max":50}""", uiEditorType: "text", sortOrder: 6270),
        ConfigurationDefinition.Create("ai.defaults.temperature", "Default Temperature", ConfigurationCategory.Functional, ConfigurationValueType.Decimal, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "0.7", validationRules: """{"min":0.0,"max":2.0}""", uiEditorType: "text", sortOrder: 6280),
        ConfigurationDefinition.Create("ai.defaults.max_tokens", "Default Max Tokens", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "4096", validationRules: """{"min":100,"max":128000}""", uiEditorType: "text", sortOrder: 6290),
        ConfigurationDefinition.Create("ai.retrieval.similarity_threshold", "Retrieval Similarity Threshold", ConfigurationCategory.Functional, ConfigurationValueType.Decimal, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "0.7", validationRules: """{"min":0.0,"max":1.0}""", uiEditorType: "text", sortOrder: 6300),
        ConfigurationDefinition.Create("ai.retrieval.source_allowlist", "Retrieval Source Allowlist", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """[]""", uiEditorType: "json-editor", sortOrder: 6310),
        ConfigurationDefinition.Create("ai.retrieval.source_denylist", "Retrieval Source Denylist", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """[]""", uiEditorType: "json-editor", sortOrder: 6320),
        ConfigurationDefinition.Create("ai.retrieval.context_by_environment", "Context Sources by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":{"telemetry":true,"documents":true,"incidents":true},"Development":{"telemetry":true,"documents":true,"incidents":false}}""", uiEditorType: "json-editor", sortOrder: 6330),

        // Block D — Connector Enablement, Schedules, Retries & Timeouts
        ConfigurationDefinition.Create("integrations.connectors.enabled", "Enabled Connectors", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """["AzureDevOps","GitHub","Jira","ServiceNow","PagerDuty","Datadog","Prometheus"]""", uiEditorType: "json-editor", sortOrder: 6400),
        ConfigurationDefinition.Create("integrations.connectors.enabled_by_environment", "Connectors Enabled by Environment", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Production":["AzureDevOps","GitHub","ServiceNow","PagerDuty","Datadog","Prometheus"],"Development":["AzureDevOps","GitHub","Jira"]}""", uiEditorType: "json-editor", sortOrder: 6410),
        ConfigurationDefinition.Create("integrations.schedule.default", "Default Sync Schedule", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "0 */6 * * *", uiEditorType: "text", sortOrder: 6420),
        ConfigurationDefinition.Create("integrations.schedule.by_connector", "Sync Schedule by Connector", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"AzureDevOps":"0 */4 * * *","GitHub":"0 */4 * * *","Jira":"0 */6 * * *","ServiceNow":"0 */2 * * *","PagerDuty":"*/30 * * * *","Datadog":"*/15 * * * *","Prometheus":"*/5 * * * *"}""", uiEditorType: "json-editor", sortOrder: 6430),
        ConfigurationDefinition.Create("integrations.retry.max_attempts", "Max Retry Attempts", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "3", validationRules: """{"min":0,"max":10}""", uiEditorType: "text", sortOrder: 6440),
        ConfigurationDefinition.Create("integrations.retry.backoff_seconds", "Retry Backoff Seconds", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "30", validationRules: """{"min":5,"max":600}""", uiEditorType: "text", sortOrder: 6450),
        ConfigurationDefinition.Create("integrations.retry.exponential_backoff", "Exponential Backoff Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 6460),
        ConfigurationDefinition.Create("integrations.timeout.default_seconds", "Default Integration Timeout", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "120", validationRules: """{"min":10,"max":3600}""", uiEditorType: "text", sortOrder: 6470),
        ConfigurationDefinition.Create("integrations.timeout.by_connector", "Timeout by Connector", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"AzureDevOps":180,"GitHub":120,"Jira":120,"ServiceNow":180,"PagerDuty":60,"Datadog":90,"Prometheus":60}""", uiEditorType: "json-editor", sortOrder: 6480),
        ConfigurationDefinition.Create("integrations.execution.max_concurrent", "Max Concurrent Executions", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "5", validationRules: """{"min":1,"max":20}""", uiEditorType: "text", sortOrder: 6490),

        // Block E — Filters, Mappings, Import/Export & Sync Policy
        ConfigurationDefinition.Create("integrations.sync.filter_policy", "Sync Filter Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"excludeArchived":true,"excludeDeleted":true,"maxAgeHours":720}""", uiEditorType: "json-editor", sortOrder: 6500),
        ConfigurationDefinition.Create("integrations.sync.mapping_policy", "Sync Mapping Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"autoMapByName":true,"strictTypeValidation":true,"unmappedFieldAction":"Ignore"}""", uiEditorType: "json-editor", sortOrder: 6510),
        ConfigurationDefinition.Create("integrations.import.policy", "Import Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"allowOverwrite":false,"requireValidation":true,"onConflict":"Skip","maxBatchSize":1000}""", uiEditorType: "json-editor", sortOrder: 6520),
        ConfigurationDefinition.Create("integrations.export.policy", "Export Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"includeMetadata":true,"defaultFormat":"JSON","maxRecords":10000,"sanitizeSensitive":true}""", uiEditorType: "json-editor", sortOrder: 6530),
        ConfigurationDefinition.Create("integrations.sync.overwrite_behavior", "Sync Overwrite Behavior", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "Merge", validationRules: """{"enum":["Overwrite","Merge","Skip"]}""", uiEditorType: "select", sortOrder: 6540),
        ConfigurationDefinition.Create("integrations.sync.pre_validation_enabled", "Pre-Sync Validation Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 6550),
        ConfigurationDefinition.Create("integrations.freshness.staleness_threshold_hours", "Staleness Threshold Hours", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "24", validationRules: """{"min":1,"max":168}""", uiEditorType: "text", sortOrder: 6560),
        ConfigurationDefinition.Create("integrations.freshness.by_connector", "Freshness Thresholds by Connector", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"AzureDevOps":12,"GitHub":12,"Jira":24,"ServiceNow":6,"PagerDuty":1,"Datadog":1,"Prometheus":1}""", uiEditorType: "json-editor", sortOrder: 6570),

        // Block F — Failure Reaction, Notification & Governance
        ConfigurationDefinition.Create("integrations.failure.notification_policy", "Integration Failure Notification Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"notifyOnFirstFailure":true,"notifyOnConsecutiveFailures":3,"notifyOnAuthFailure":true,"notifyOnStaleness":true,"digestFrequency":"Hourly"}""", uiEditorType: "json-editor", sortOrder: 6600),
        ConfigurationDefinition.Create("integrations.failure.severity_mapping", "Failure Severity Mapping", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"authFailure":"Critical","syncFailure":"High","timeoutFailure":"Medium","validationFailure":"Low","staleData":"Medium"}""", uiEditorType: "json-editor", sortOrder: 6610),
        ConfigurationDefinition.Create("integrations.failure.escalation_policy", "Failure Escalation Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"Critical":{"escalateAfterMinutes":15,"recipient":"platform-admin"},"High":{"escalateAfterMinutes":60,"recipient":"integration-owner"},"Medium":{"escalateAfterMinutes":240},"Low":{"escalateAfterMinutes":1440}}""", uiEditorType: "json-editor", sortOrder: 6620),
        ConfigurationDefinition.Create("integrations.failure.auto_disable_enabled", "Auto-Disable on Failure Enabled", ConfigurationCategory.Functional, ConfigurationValueType.Boolean, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "true", uiEditorType: "toggle", sortOrder: 6630),
        ConfigurationDefinition.Create("integrations.failure.auto_disable_threshold", "Auto-Disable Failure Threshold", ConfigurationCategory.Functional, ConfigurationValueType.Integer, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "5", validationRules: """{"min":2,"max":50}""", uiEditorType: "text", sortOrder: 6640),
        ConfigurationDefinition.Create("integrations.failure.auth_reaction_policy", "Auth Failure Reaction Policy", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: """{"pauseSync":true,"notifyOwner":true,"autoRetryAfterMinutes":60,"maxAuthRetries":3}""", uiEditorType: "json-editor", sortOrder: 6650),
        ConfigurationDefinition.Create("integrations.owner.fallback_recipient", "Integration Fallback Owner", ConfigurationCategory.Functional, ConfigurationValueType.String, [ConfigurationScope.System, ConfigurationScope.Tenant], defaultValue: "platform-admin", uiEditorType: "text", sortOrder: 6660),
        ConfigurationDefinition.Create("integrations.governance.blocked_in_production", "Integration Operations Blocked in Production", ConfigurationCategory.Functional, ConfigurationValueType.Json, [ConfigurationScope.System], defaultValue: """["bulkDelete","schemaOverwrite","forceReSync"]""", isInheritable: false, uiEditorType: "json-editor", sortOrder: 6670),
    ];

    // ── Structural Tests ───────────────────────────────────────────────

    [Fact]
    public void Phase7Definitions_ShouldHaveUniqueKeys()
    {
        var definitions = BuildPhase7Definitions();
        var keys = definitions.Select(d => d.Key).ToList();

        keys.Should().OnlyHaveUniqueItems("every definition must have a unique key");
    }

    [Fact]
    public void Phase7Definitions_ShouldHaveUniqueSortOrders()
    {
        var definitions = BuildPhase7Definitions();
        var sortOrders = definitions.Select(d => d.SortOrder).ToList();

        sortOrders.Should().OnlyHaveUniqueItems("every definition must have a unique sort order");
    }

    [Fact]
    public void Phase7Definitions_ShouldAllBeFunctionalCategory()
    {
        var definitions = BuildPhase7Definitions();

        definitions.Should().OnlyContain(
            d => d.Category == ConfigurationCategory.Functional,
            "all Phase 7 definitions belong to the Functional category");
    }

    [Fact]
    public void Phase7Definitions_ShouldHaveCorrectKeyPrefix()
    {
        var definitions = BuildPhase7Definitions();

        definitions.Should().OnlyContain(
            d => d.Key.StartsWith("ai.") || d.Key.StartsWith("integrations."),
            "all Phase 7 definitions must start with ai.* or integrations.*");
    }

    [Fact]
    public void Phase7Definitions_SortOrdersShouldBeInPhase7Range()
    {
        var definitions = BuildPhase7Definitions();

        definitions.Should().OnlyContain(
            d => d.SortOrder >= 6000 && d.SortOrder <= 6999,
            "all Phase 7 sort orders must be in range 6000–6999");
    }

    [Fact]
    public void Phase7Definitions_ShouldHave55Definitions()
    {
        var definitions = BuildPhase7Definitions();
        definitions.Should().HaveCount(55, "Phase 7 delivers exactly 55 definitions");
    }

    // ── AI Provider & Model Tests ──────────────────────────────────────

    [Fact]
    public void EnabledProviders_ShouldIncludeInternalProvider()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.providers.enabled");

        def.DefaultValue.Should().Contain("Internal");
    }

    [Fact]
    public void EnabledModels_ShouldIncludeGpt4o()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.models.enabled");

        def.DefaultValue.Should().Contain("gpt-4o");
    }

    [Fact]
    public void FallbackOrder_ShouldBeJsonArray()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.providers.fallback_order");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().StartWith("[");
    }

    [Fact]
    public void AllowExternal_ShouldSupportEnvironmentScope()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.usage.allow_external");

        def.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
    }

    [Fact]
    public void BlockedEnvironments_ShouldBeSystemOnlyNotInheritable()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.usage.blocked_environments");

        def.AllowedScopes.Should().ContainSingle()
            .Which.Should().Be(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse();
    }

    // ── AI Budget & Quota Tests ────────────────────────────────────────

    [Fact]
    public void BudgetByUser_ShouldHaveMonthlyTokensDefault()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.budget.by_user");

        def.DefaultValue.Should().Contain("monthlyTokens");
    }

    [Fact]
    public void BudgetExceedPolicy_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.budget.exceed_policy");

        def.ValidationRules.Should().Contain("Warn");
        def.ValidationRules.Should().Contain("Block");
        def.ValidationRules.Should().Contain("Throttle");
        def.UiEditorType.Should().Be("select");
    }

    [Fact]
    public void BudgetWarningThresholds_ShouldBeJsonArray()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.budget.warning_thresholds");

        def.ValueType.Should().Be(ConfigurationValueType.Json);
        def.DefaultValue.Should().StartWith("[");
    }

    // ── Retention, Audit, Prompts & Retrieval Tests ────────────────────

    [Fact]
    public void ConversationRetention_ShouldHaveRangeValidation()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.retention.conversation_days");

        def.ValidationRules.Should().Contain("min");
        def.ValidationRules.Should().Contain("max");
    }

    [Fact]
    public void AuditLevel_ShouldDefaultToStandard()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.audit.level");

        def.DefaultValue.Should().Be("Standard");
        def.UiEditorType.Should().Be("select");
    }

    [Fact]
    public void LogPrompts_ShouldDefaultToFalse()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.audit.log_prompts");

        def.DefaultValue.Should().Be("false");
        def.UiEditorType.Should().Be("toggle");
    }

    [Fact]
    public void BasePrompts_ShouldContainChatCapability()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.prompts.base_by_capability");

        def.DefaultValue.Should().Contain("chat");
        def.DefaultValue.Should().Contain("NexTraceOne");
    }

    [Fact]
    public void TenantPromptOverride_ShouldBeSystemOnlyNotInheritable()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.prompts.allow_tenant_override");

        def.AllowedScopes.Should().ContainSingle()
            .Which.Should().Be(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse();
    }

    [Fact]
    public void RetrievalTopK_ShouldHaveRangeValidation()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.retrieval.top_k");

        def.DefaultValue.Should().Be("5");
        def.ValidationRules.Should().Contain("min");
    }

    [Fact]
    public void DefaultTemperature_ShouldBeDecimalType()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.defaults.temperature");

        def.ValueType.Should().Be(ConfigurationValueType.Decimal);
        def.DefaultValue.Should().Be("0.7");
    }

    [Fact]
    public void DefaultMaxTokens_ShouldHaveValidRange()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.defaults.max_tokens");

        def.DefaultValue.Should().Be("4096");
        def.ValidationRules.Should().Contain("128000");
    }

    [Fact]
    public void SimilarityThreshold_ShouldBeDecimalWithRange()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "ai.retrieval.similarity_threshold");

        def.ValueType.Should().Be(ConfigurationValueType.Decimal);
        def.ValidationRules.Should().Contain("1.0");
    }

    // ── Connector & Integration Tests ──────────────────────────────────

    [Fact]
    public void EnabledConnectors_ShouldIncludeStandardConnectors()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.connectors.enabled");

        def.DefaultValue.Should().Contain("GitHub");
        def.DefaultValue.Should().Contain("AzureDevOps");
    }

    [Fact]
    public void RetryMaxAttempts_ShouldHaveValidRange()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.retry.max_attempts");

        def.DefaultValue.Should().Be("3");
        def.ValidationRules.Should().Contain("max");
    }

    [Fact]
    public void BackoffSeconds_ShouldHaveMinimumOf5()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.retry.backoff_seconds");

        def.ValidationRules.Should().Contain("\"min\":5");
    }

    [Fact]
    public void DefaultTimeout_ShouldBe120Seconds()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.timeout.default_seconds");

        def.DefaultValue.Should().Be("120");
    }

    [Fact]
    public void ExponentialBackoff_ShouldDefaultToTrue()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.retry.exponential_backoff");

        def.DefaultValue.Should().Be("true");
        def.UiEditorType.Should().Be("toggle");
    }

    // ── Sync, Filter, Import/Export Tests ──────────────────────────────

    [Fact]
    public void SyncOverwriteBehavior_ShouldHaveEnumValidation()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.sync.overwrite_behavior");

        def.ValidationRules.Should().Contain("Overwrite");
        def.ValidationRules.Should().Contain("Merge");
        def.UiEditorType.Should().Be("select");
    }

    [Fact]
    public void ImportPolicy_ShouldNotAllowOverwriteByDefault()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.import.policy");

        def.DefaultValue.Should().Contain("\"allowOverwrite\":false");
    }

    [Fact]
    public void StalenessThreshold_ShouldDefaultTo24Hours()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.freshness.staleness_threshold_hours");

        def.DefaultValue.Should().Be("24");
    }

    // ── Failure Reaction & Governance Tests ─────────────────────────────

    [Fact]
    public void FailureNotificationPolicy_ShouldNotifyOnAuthFailure()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.failure.notification_policy");

        def.DefaultValue.Should().Contain("\"notifyOnAuthFailure\":true");
    }

    [Fact]
    public void FailureSeverityMapping_ShouldMapAuthFailureToCritical()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.failure.severity_mapping");

        def.DefaultValue.Should().Contain("\"authFailure\":\"Critical\"");
    }

    [Fact]
    public void AutoDisableThreshold_ShouldHaveMinimumOf2()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.failure.auto_disable_threshold");

        def.ValidationRules.Should().Contain("\"min\":2");
    }

    [Fact]
    public void IntegrationFallbackOwner_ShouldDefaultToPlatformAdmin()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.owner.fallback_recipient");

        def.DefaultValue.Should().Be("platform-admin");
    }

    [Fact]
    public void BlockedInProduction_ShouldBeSystemOnlyNotInheritable()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.governance.blocked_in_production");

        def.AllowedScopes.Should().ContainSingle()
            .Which.Should().Be(ConfigurationScope.System);
        def.IsInheritable.Should().BeFalse();
    }

    [Fact]
    public void BlockedInProduction_ShouldIncludeDangerousOperations()
    {
        var definitions = BuildPhase7Definitions();
        var def = definitions.Single(d => d.Key == "integrations.governance.blocked_in_production");

        def.DefaultValue.Should().Contain("bulkDelete");
        def.DefaultValue.Should().Contain("schemaOverwrite");
    }
}
