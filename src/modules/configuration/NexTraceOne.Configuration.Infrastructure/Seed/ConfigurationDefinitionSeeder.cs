using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Configuration.Infrastructure.Persistence;

namespace NexTraceOne.Configuration.Infrastructure.Seed;

/// <summary>
/// Seeder idempotente para definições de configuração padrão da plataforma.
/// Apenas insere definições que ainda não existem (verificação por chave).
/// </summary>
public static class ConfigurationDefinitionSeeder
{
    /// <summary>
    /// Insere as definições de configuração iniciais se ainda não existirem.
    /// </summary>
    public static async Task SeedDefaultDefinitionsAsync(
        ConfigurationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existingKeys = await dbContext.Definitions
            .Select(d => d.Key)
            .ToHashSetAsync(cancellationToken);

        var definitions = BuildDefaultDefinitions();

        foreach (var definition in definitions)
        {
            if (!existingKeys.Contains(definition.Key))
            {
                await dbContext.Definitions.AddAsync(definition, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<ConfigurationDefinition> BuildDefaultDefinitions() =>
    [
        ConfigurationDefinition.Create(
            key: "notifications.enabled",
            displayName: "Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether the notification system is active globally or per tenant.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 100),

        ConfigurationDefinition.Create(
            key: "notifications.email.enabled",
            displayName: "Email Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether email notifications are sent.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 110),

        ConfigurationDefinition.Create(
            key: "notifications.teams.enabled",
            displayName: "Teams Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether Microsoft Teams notifications are sent.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 120),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.start",
            displayName: "Quiet Hours Start",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "Start time for notification quiet hours (HH:mm format).",
            defaultValue: "22:00",
            uiEditorType: "text",
            sortOrder: 130),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.end",
            displayName: "Quiet Hours End",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "End time for notification quiet hours (HH:mm format).",
            defaultValue: "08:00",
            uiEditorType: "text",
            sortOrder: 140),

        ConfigurationDefinition.Create(
            key: "ai.default_temperature",
            displayName: "AI Default Temperature",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "Default temperature parameter for AI model inference.",
            defaultValue: "0.7",
            validationRules: """{"min": 0.0, "max": 2.0}""",
            uiEditorType: "text",
            sortOrder: 200),

        ConfigurationDefinition.Create(
            key: "ai.max_tokens",
            displayName: "AI Max Tokens",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of tokens per AI inference request.",
            defaultValue: "4096",
            validationRules: """{"min": 256, "max": 128000}""",
            uiEditorType: "text",
            sortOrder: 210),

        ConfigurationDefinition.Create(
            key: "governance.approval_required",
            displayName: "Governance Approval Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Team],
            description: "Whether governance changes require approval before taking effect.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 300),

        ConfigurationDefinition.Create(
            key: "governance.max_waiver_days",
            displayName: "Maximum Waiver Duration (Days)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of days a governance waiver can be active.",
            defaultValue: "90",
            validationRules: """{"min": 1, "max": 365}""",
            uiEditorType: "text",
            sortOrder: 310),

        ConfigurationDefinition.Create(
            key: "platform.maintenance_mode",
            displayName: "Platform Maintenance Mode",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "When enabled, the platform enters maintenance mode with restricted access.",
            defaultValue: "false",
            isEditable: true,
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 400),

        ConfigurationDefinition.Create(
            key: "security.session_timeout_minutes",
            displayName: "Session Timeout (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Number of minutes of inactivity before a user session expires.",
            defaultValue: "60",
            validationRules: """{"min": 5, "max": 1440}""",
            uiEditorType: "text",
            sortOrder: 500),

        ConfigurationDefinition.Create(
            key: "security.mfa_required",
            displayName: "MFA Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether multi-factor authentication is required for all users.",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 510),

        ConfigurationDefinition.Create(
            key: "integration.webhook_secret",
            displayName: "Webhook Secret",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Shared secret for webhook signature verification.",
            isSensitive: true,
            uiEditorType: "text",
            sortOrder: 600),

        ConfigurationDefinition.Create(
            key: "finops.budget_alert_threshold",
            displayName: "Budget Alert Threshold (%)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Team],
            description: "Percentage of budget consumption that triggers an alert.",
            defaultValue: "80.0",
            validationRules: """{"min": 0, "max": 100}""",
            uiEditorType: "text",
            sortOrder: 700),
    ];
}
