using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Configuration.Infrastructure.Persistence;

namespace NexTraceOne.Configuration.Infrastructure.Seed;

/// <summary>
/// Seeder idempotente para definições de configuração padrão da plataforma.
/// Apenas insere definições que ainda não existem (verificação por chave).
/// Implementa <see cref="IConfigurationDefinitionSeeder"/> para ser injetável via DI.
///
/// Execução segura em todos os ambientes (Development, Staging, Production).
/// Comportamento em primeira execução: insere todas as 345+ definições.
/// Comportamento em re-execuções: ignora definições já existentes (IsNoOp).
/// </summary>
public sealed class ConfigurationDefinitionSeeder(ConfigurationDbContext dbContext)
    : IConfigurationDefinitionSeeder
{
    /// <inheritdoc />
    public Task<SeedingResult> SeedAsync(CancellationToken cancellationToken = default)
        => SeedDefaultDefinitionsAsync(dbContext, cancellationToken);

    /// <summary>
    /// Insere as definições de configuração iniciais se ainda não existirem.
    /// Retorna um <see cref="SeedingResult"/> com o número de definições inseridas e ignoradas.
    /// </summary>
    public static async Task<SeedingResult> SeedDefaultDefinitionsAsync(
        ConfigurationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existingKeys = await dbContext.Definitions
            .Select(d => d.Key)
            .ToHashSetAsync(cancellationToken);

        var definitions = BuildDefaultDefinitions();
        var added = 0;
        var skipped = 0;

        foreach (var definition in definitions)
        {
            if (existingKeys.Contains(definition.Key))
            {
                skipped++;
            }
            else
            {
                await dbContext.Definitions.AddAsync(definition, cancellationToken);
                existingKeys.Add(definition.Key);
                added++;
            }
        }

        if (added > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new SeedingResult(added, skipped);
    }

    private static List<ConfigurationDefinition> BuildDefaultDefinitions() =>
    [
        ConfigurationDefinition.Create(
            key: "notifications.enabled",
            displayName: "config.notifications.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 100),

        ConfigurationDefinition.Create(
            key: "notifications.email.enabled",
            displayName: "config.notifications.email.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.email.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 110),

        ConfigurationDefinition.Create(
            key: "notifications.teams.enabled",
            displayName: "config.notifications.teams.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.teams.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 120),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.start",
            displayName: "config.notifications.quiet_hours.start.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.quiet_hours.start.description",
            defaultValue: "22:00",
            uiEditorType: "text",
            sortOrder: 130),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.end",
            displayName: "config.notifications.quiet_hours.end.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.quiet_hours.end.description",
            defaultValue: "08:00",
            uiEditorType: "text",
            sortOrder: 140),

        // ── PHASE 2: Notification & Communication Parameterization ─────────────

        // --- Types, Categories & Severities ---

        ConfigurationDefinition.Create(
            key: "notifications.types.enabled",
            displayName: "config.notifications.types.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.notifications.types.enabled.description",
            defaultValue: """["IncidentCreated","IncidentEscalated","IncidentResolved","AnomalyDetected","HealthDegradation","ApprovalPending","ApprovalApproved","ApprovalRejected","ApprovalExpiring","ContractPublished","BreakingChangeDetected","ContractValidationFailed","BreakGlassActivated","JitAccessPending","JitAccessGranted","UserRoleChanged","AccessReviewPending","ComplianceCheckFailed","PolicyViolated","EvidenceExpiring","BudgetExceeded","BudgetThresholdReached","IntegrationFailed","SyncFailed","ConnectorAuthFailed","AiProviderUnavailable","TokenBudgetExceeded","AiGenerationFailed","AiActionBlockedByPolicy"]""",
            uiEditorType: "json-editor",
            sortOrder: 150),

        ConfigurationDefinition.Create(
            key: "notifications.categories.enabled",
            displayName: "config.notifications.categories.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.categories.enabled.description",
            defaultValue: """["Incident","Approval","Change","Contract","Security","Compliance","FinOps","AI","Integration","Platform","Informational"]""",
            uiEditorType: "json-editor",
            sortOrder: 151),

        ConfigurationDefinition.Create(
            key: "notifications.severity.default",
            displayName: "config.notifications.severity.default.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.severity.default.description",
            defaultValue: "Info",
            validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""",
            uiEditorType: "select",
            sortOrder: 152),

        ConfigurationDefinition.Create(
            key: "notifications.severity.minimum_for_external",
            displayName: "config.notifications.severity.minimum_for_external.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.notifications.severity.minimum_for_external.description",
            defaultValue: "Warning",
            validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""",
            uiEditorType: "select",
            sortOrder: 153),

        ConfigurationDefinition.Create(
            key: "notifications.mandatory.types",
            displayName: "config.notifications.mandatory.types.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.notifications.mandatory.types.description",
            defaultValue: """["BreakGlassActivated","IncidentCreated","IncidentEscalated","ApprovalPending","ComplianceCheckFailed"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 154),

        ConfigurationDefinition.Create(
            key: "notifications.mandatory.severities",
            displayName: "config.notifications.mandatory.severities.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.notifications.mandatory.severities.description",
            defaultValue: """["Critical"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 155),

        // --- Channels Allowed & Mandatory ---

        ConfigurationDefinition.Create(
            key: "notifications.channels.inapp.enabled",
            displayName: "config.notifications.channels.inapp.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.channels.inapp.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 160),

        ConfigurationDefinition.Create(
            key: "notifications.channels.allowed_by_type",
            displayName: "config.notifications.channels.allowed_by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.channels.allowed_by_type.description",
            defaultValue: """{}""",
            uiEditorType: "json-editor",
            sortOrder: 161),

        ConfigurationDefinition.Create(
            key: "notifications.channels.mandatory_by_severity",
            displayName: "config.notifications.channels.mandatory_by_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.notifications.channels.mandatory_by_severity.description",
            defaultValue: """{"Critical":["InApp","Email","MicrosoftTeams"],"Warning":["InApp","Email"]}""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 162),

        ConfigurationDefinition.Create(
            key: "notifications.channels.mandatory_by_type",
            displayName: "config.notifications.channels.mandatory_by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.notifications.channels.mandatory_by_type.description",
            defaultValue: """{"BreakGlassActivated":["InApp","Email","MicrosoftTeams"],"ApprovalPending":["InApp","Email"],"ComplianceCheckFailed":["InApp","Email"]}""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 163),

        ConfigurationDefinition.Create(
            key: "notifications.channels.disabled_in_environment",
            displayName: "config.notifications.channels.disabled_in_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.Environment],
            description: "config.notifications.channels.disabled_in_environment.description",
            defaultValue: "[]",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 164),

        // --- Templates ---

        ConfigurationDefinition.Create(
            key: "notifications.templates.internal",
            displayName: "config.notifications.templates.internal.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.templates.internal.description",
            defaultValue: """{"IncidentCreated":{"title":"Incident created — {ServiceName}","message":"A new incident with severity {IncidentSeverity} has been created for service {ServiceName}.","placeholders":["ServiceName","IncidentSeverity"]},"ApprovalPending":{"title":"Approval required — {EntityName}","message":"A new approval has been requested by {RequestedBy} for {EntityName}.","placeholders":["EntityName","RequestedBy"]},"BreakGlassActivated":{"title":"Break-glass access activated","message":"Emergency break-glass access was activated by {ActivatedBy}.","placeholders":["ActivatedBy"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 170),

        ConfigurationDefinition.Create(
            key: "notifications.templates.email",
            displayName: "config.notifications.templates.email.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.templates.email.description",
            defaultValue: """{"default":{"subject":"[NexTraceOne] {Title}","bodyHtml":"<h2>{Title}</h2><p>{Message}</p><p><a href='{ActionUrl}'>View details</a></p>","placeholders":["Title","Message","ActionUrl"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 171),

        ConfigurationDefinition.Create(
            key: "notifications.templates.teams",
            displayName: "config.notifications.templates.teams.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.templates.teams.description",
            defaultValue: """{"default":{"cardTitle":"NexTraceOne — {Title}","cardBody":"{Message}","actionUrl":"{ActionUrl}","placeholders":["Title","Message","ActionUrl"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 172),

        // --- Routing & Fallback ---

        ConfigurationDefinition.Create(
            key: "notifications.routing.default_policy",
            displayName: "config.notifications.routing.default_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.routing.default_policy.description",
            defaultValue: """{"ownerFirst":true,"adminFallback":true,"approverRouting":false}""",
            uiEditorType: "json-editor",
            sortOrder: 175),

        ConfigurationDefinition.Create(
            key: "notifications.routing.fallback_recipients",
            displayName: "config.notifications.routing.fallback_recipients.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.routing.fallback_recipients.description",
            defaultValue: "[]",
            uiEditorType: "json-editor",
            sortOrder: 176),

        ConfigurationDefinition.Create(
            key: "notifications.routing.by_category",
            displayName: "config.notifications.routing.by_category.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.routing.by_category.description",
            defaultValue: """{"Incident":{"recipientType":"owner","fallbackToAdmin":true},"Approval":{"recipientType":"approver","fallbackToAdmin":true},"Security":{"recipientType":"admin","fallbackToAdmin":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 177),

        ConfigurationDefinition.Create(
            key: "notifications.routing.by_severity",
            displayName: "config.notifications.routing.by_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.routing.by_severity.description",
            defaultValue: """{"Critical":{"notifyAdmins":true,"broadcastToTeam":true},"Warning":{"notifyAdmins":false,"broadcastToTeam":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 178),

        // --- Preferences, Quiet Hours, Digest & Suppression ---

        ConfigurationDefinition.Create(
            key: "notifications.preferences.default_by_tenant",
            displayName: "config.notifications.preferences.default_by_tenant.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.preferences.default_by_tenant.description",
            defaultValue: """{"emailEnabled":true,"teamsEnabled":true,"digestEnabled":false}""",
            uiEditorType: "json-editor",
            sortOrder: 180),

        ConfigurationDefinition.Create(
            key: "notifications.preferences.default_by_role",
            displayName: "config.notifications.preferences.default_by_role.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role],
            description: "config.notifications.preferences.default_by_role.description",
            defaultValue: "{}",
            uiEditorType: "json-editor",
            sortOrder: 181),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.enabled",
            displayName: "config.notifications.quiet_hours.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.quiet_hours.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 182),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.bypass_categories",
            displayName: "config.notifications.quiet_hours.bypass_categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.notifications.quiet_hours.bypass_categories.description",
            defaultValue: """["Incident","Security"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 183),

        ConfigurationDefinition.Create(
            key: "notifications.digest.enabled",
            displayName: "config.notifications.digest.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.digest.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 184),

        ConfigurationDefinition.Create(
            key: "notifications.digest.period_hours",
            displayName: "config.notifications.digest.period_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.digest.period_hours.description",
            defaultValue: "24",
            validationRules: """{"min":1,"max":168}""",
            uiEditorType: "text",
            sortOrder: 185),

        ConfigurationDefinition.Create(
            key: "notifications.digest.eligible_categories",
            displayName: "config.notifications.digest.eligible_categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.digest.eligible_categories.description",
            defaultValue: """["Informational","Change","Integration","Platform"]""",
            uiEditorType: "json-editor",
            sortOrder: 186),

        ConfigurationDefinition.Create(
            key: "notifications.suppress.enabled",
            displayName: "config.notifications.suppress.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.suppress.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 187),

        ConfigurationDefinition.Create(
            key: "notifications.suppress.acknowledged_window_minutes",
            displayName: "config.notifications.suppress.acknowledged_window_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.suppress.acknowledged_window_minutes.description",
            defaultValue: "30",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 188),

        ConfigurationDefinition.Create(
            key: "notifications.acknowledge.required_categories",
            displayName: "config.notifications.acknowledge.required_categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.acknowledge.required_categories.description",
            defaultValue: """["Incident","Security","Compliance"]""",
            uiEditorType: "json-editor",
            sortOrder: 189),

        // --- Escalation, Dedup & Incident Linkage ---

        ConfigurationDefinition.Create(
            key: "notifications.dedup.enabled",
            displayName: "config.notifications.dedup.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.dedup.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 190),

        ConfigurationDefinition.Create(
            key: "notifications.dedup.window_minutes",
            displayName: "config.notifications.dedup.window_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.dedup.window_minutes.description",
            defaultValue: "5",
            validationRules: """{"min":1,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 191),

        ConfigurationDefinition.Create(
            key: "notifications.dedup.window_by_category",
            displayName: "config.notifications.dedup.window_by_category.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.dedup.window_by_category.description",
            defaultValue: """{"Incident":10,"Security":10,"Integration":15}""",
            uiEditorType: "json-editor",
            sortOrder: 192),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.enabled",
            displayName: "config.notifications.escalation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.escalation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 193),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.critical_threshold_minutes",
            displayName: "config.notifications.escalation.critical_threshold_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.escalation.critical_threshold_minutes.description",
            defaultValue: "30",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 194),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.action_required_threshold_minutes",
            displayName: "config.notifications.escalation.action_required_threshold_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.escalation.action_required_threshold_minutes.description",
            defaultValue: "120",
            validationRules: """{"min":15,"max":2880}""",
            uiEditorType: "text",
            sortOrder: 195),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.channels",
            displayName: "config.notifications.escalation.channels.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.escalation.channels.description",
            defaultValue: """["InApp","Email","MicrosoftTeams"]""",
            uiEditorType: "json-editor",
            sortOrder: 196),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.enabled",
            displayName: "config.notifications.incident_linkage.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.incident_linkage.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 197),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.auto_create_enabled",
            displayName: "config.notifications.incident_linkage.auto_create_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.incident_linkage.auto_create_enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 198),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.eligible_types",
            displayName: "config.notifications.incident_linkage.eligible_types.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.incident_linkage.eligible_types.description",
            defaultValue: """["IncidentCreated","IncidentEscalated","HealthDegradation","AnomalyDetected"]""",
            uiEditorType: "json-editor",
            sortOrder: 199),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.correlation_window_minutes",
            displayName: "config.notifications.incident_linkage.correlation_window_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.incident_linkage.correlation_window_minutes.description",
            defaultValue: "60",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 200),

        ConfigurationDefinition.Create(
            key: "notifications.grouping.window_minutes",
            displayName: "config.notifications.grouping.window_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.grouping.window_minutes.description",
            defaultValue: "60",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 201),

        // ── PHASE 2: Retenção e Rate Limiting ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "notifications.retention.days",
            displayName: "config.notifications.retention.days.label",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.retention.days.description",
            defaultValue: "90",
            validationRules: """{"min":7,"max":730}""",
            uiEditorType: "text",
            sortOrder: 202),

        ConfigurationDefinition.Create(
            key: "notifications.retention.purge_enabled",
            displayName: "config.notifications.retention.purge_enabled.label",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.retention.purge_enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 203),

        ConfigurationDefinition.Create(
            key: "notifications.rate_limit.enabled",
            displayName: "config.notifications.rate_limit.enabled.label",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.rate_limit.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 204),

        ConfigurationDefinition.Create(
            key: "notifications.rate_limit.max_per_user_per_hour",
            displayName: "config.notifications.rate_limit.max_per_user_per_hour.label",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.notifications.rate_limit.max_per_user_per_hour.description",
            defaultValue: "100",
            validationRules: """{"min":1,"max":1000}""",
            uiEditorType: "text",
            sortOrder: 205),

        // ── END PHASE 2 ───────────────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "ai.default_temperature",
            displayName: "config.ai.default_temperature.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.ai.default_temperature.description",
            defaultValue: "0.7",
            validationRules: """{"min": 0.0, "max": 2.0}""",
            uiEditorType: "text",
            sortOrder: 200),

        ConfigurationDefinition.Create(
            key: "ai.max_tokens",
            displayName: "config.ai.max_tokens.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.max_tokens.description",
            defaultValue: "4096",
            validationRules: """{"min": 256, "max": 128000}""",
            uiEditorType: "text",
            sortOrder: 210),

        ConfigurationDefinition.Create(
            key: "governance.approval_required",
            displayName: "config.governance.approval_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Team],
            description: "config.governance.approval_required.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 300),

        ConfigurationDefinition.Create(
            key: "governance.max_waiver_days",
            displayName: "config.governance.max_waiver_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.max_waiver_days.description",
            defaultValue: "90",
            validationRules: """{"min": 1, "max": 365}""",
            uiEditorType: "text",
            sortOrder: 310),

        ConfigurationDefinition.Create(
            key: "platform.maintenance_mode",
            displayName: "config.platform.maintenance_mode.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "config.platform.maintenance_mode.description",
            defaultValue: "false",
            isEditable: true,
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 400),

        ConfigurationDefinition.Create(
            key: "security.session_timeout_minutes",
            displayName: "config.security.session_timeout_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.session_timeout_minutes.description",
            defaultValue: "60",
            validationRules: """{"min": 5, "max": 1440}""",
            uiEditorType: "text",
            sortOrder: 500),

        ConfigurationDefinition.Create(
            key: "security.mfa_required",
            displayName: "config.security.mfa_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.mfa_required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 510),

        ConfigurationDefinition.Create(
            key: "integration.webhook_secret",
            displayName: "config.integration.webhook_secret.label",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integration.webhook_secret.description",
            isSensitive: true,
            uiEditorType: "text",
            sortOrder: 600),

        ConfigurationDefinition.Create(
            key: "finops.budget_alert_threshold",
            displayName: "config.finops.budget_alert_threshold.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Team],
            description: "config.finops.budget_alert_threshold.description",
            defaultValue: "80.0",
            validationRules: """{"min": 0, "max": 100}""",
            uiEditorType: "text",
            sortOrder: 700,
            isDeprecated: true),

        // ── BLOCK B — Instance Configuration ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "instance.name",
            displayName: "config.instance.name.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.name.description",
            defaultValue: "NexTraceOne",
            uiEditorType: "text",
            sortOrder: 1000),

        ConfigurationDefinition.Create(
            key: "instance.commercial_name",
            displayName: "config.instance.commercial_name.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.commercial_name.description",
            defaultValue: "NexTraceOne Platform",
            uiEditorType: "text",
            sortOrder: 1010),

        ConfigurationDefinition.Create(
            key: "instance.default_language",
            displayName: "config.instance.default_language.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.default_language.description",
            defaultValue: "en",
            validationRules: """{"enum":["en","pt-BR","pt-PT","es"]}""",
            uiEditorType: "select",
            sortOrder: 1020),

        ConfigurationDefinition.Create(
            key: "instance.default_timezone",
            displayName: "config.instance.default_timezone.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.default_timezone.description",
            defaultValue: "UTC",
            uiEditorType: "text",
            sortOrder: 1030),

        ConfigurationDefinition.Create(
            key: "instance.date_format",
            displayName: "config.instance.date_format.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.date_format.description",
            defaultValue: "yyyy-MM-dd",
            uiEditorType: "text",
            sortOrder: 1040),

        ConfigurationDefinition.Create(
            key: "instance.support_url",
            displayName: "config.instance.support_url.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.support_url.description",
            uiEditorType: "text",
            sortOrder: 1050),

        ConfigurationDefinition.Create(
            key: "instance.terms_url",
            displayName: "config.instance.terms_url.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.terms_url.description",
            uiEditorType: "text",
            sortOrder: 1060),

        ConfigurationDefinition.Create(
            key: "instance.privacy_url",
            displayName: "config.instance.privacy_url.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.instance.privacy_url.description",
            uiEditorType: "text",
            sortOrder: 1070),

        // ── BLOCK C — Tenant Configuration ────────────────────────────────

        ConfigurationDefinition.Create(
            key: "tenant.display_name",
            displayName: "config.tenant.display_name.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Tenant],
            description: "config.tenant.display_name.description",
            uiEditorType: "text",
            sortOrder: 1100),

        ConfigurationDefinition.Create(
            key: "tenant.default_language",
            displayName: "config.tenant.default_language.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.tenant.default_language.description",
            defaultValue: "en",
            isInheritable: true,
            validationRules: """{"enum":["en","pt-BR","pt-PT","es"]}""",
            uiEditorType: "select",
            sortOrder: 1110),

        ConfigurationDefinition.Create(
            key: "tenant.default_timezone",
            displayName: "config.tenant.default_timezone.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.tenant.default_timezone.description",
            defaultValue: "UTC",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 1120),

        ConfigurationDefinition.Create(
            key: "tenant.contact_email",
            displayName: "config.tenant.contact_email.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Tenant],
            description: "config.tenant.contact_email.description",
            uiEditorType: "text",
            sortOrder: 1130),

        ConfigurationDefinition.Create(
            key: "tenant.max_users",
            displayName: "config.tenant.max_users.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.tenant.max_users.description",
            defaultValue: "100",
            validationRules: """{"min":1,"max":10000}""",
            uiEditorType: "text",
            sortOrder: 1140),

        ConfigurationDefinition.Create(
            key: "tenant.max_environments",
            displayName: "config.tenant.max_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.tenant.max_environments.description",
            defaultValue: "10",
            validationRules: """{"min":1,"max":50}""",
            uiEditorType: "text",
            sortOrder: 1150),

        // ── BLOCK D — Environment Configuration ───────────────────────────

        ConfigurationDefinition.Create(
            key: "environment.classification",
            displayName: "config.environment.classification.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment],
            description: "config.environment.classification.description",
            validationRules: """{"enum":["Development","Test","QA","PreProduction","Production","Lab"]}""",
            uiEditorType: "select",
            sortOrder: 1200),

        ConfigurationDefinition.Create(
            key: "environment.is_production",
            displayName: "config.environment.is_production.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment],
            description: "config.environment.is_production.description",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 1210),

        ConfigurationDefinition.Create(
            key: "environment.criticality",
            displayName: "config.environment.criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.environment.criticality.description",
            defaultValue: "medium",
            validationRules: """{"enum":["low","medium","high","critical"]}""",
            uiEditorType: "select",
            sortOrder: 1220),

        ConfigurationDefinition.Create(
            key: "environment.lifecycle_order",
            displayName: "config.environment.lifecycle_order.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.Environment],
            description: "config.environment.lifecycle_order.description",
            defaultValue: "0",
            validationRules: """{"min":0,"max":100}""",
            uiEditorType: "text",
            sortOrder: 1230),

        ConfigurationDefinition.Create(
            key: "environment.description",
            displayName: "config.environment.description.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment],
            description: "config.environment.description.description",
            uiEditorType: "text",
            sortOrder: 1240),

        ConfigurationDefinition.Create(
            key: "environment.active",
            displayName: "config.environment.active.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment],
            description: "config.environment.active.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1250),

        // ── BLOCK F — Branding & Experience Defaults ──────────────────────
        // REMOVED: All branding.* parameters (logo_url, logo_dark_url, accent_color,
        // favicon_url, welcome_message, footer_text) were removed to preserve the
        // NexTraceOne visual identity. The platform's brand (logo, colors, layout)
        // must not be customizable by users or tenants.

        // ── Login Page Branding ──

        ConfigurationDefinition.Create(
            key: "branding.login_logo_url",
            displayName: "config.branding.login_logo_url.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.branding.login_logo_url.description",
            uiEditorType: "text",
            sortOrder: 1360),

        ConfigurationDefinition.Create(
            key: "branding.login_heading",
            displayName: "config.branding.login_heading.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.branding.login_heading.description",
            uiEditorType: "text",
            sortOrder: 1361),

        ConfigurationDefinition.Create(
            key: "branding.login_subheading",
            displayName: "config.branding.login_subheading.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.branding.login_subheading.description",
            uiEditorType: "text",
            sortOrder: 1362),

        ConfigurationDefinition.Create(
            key: "branding.login_background_url",
            displayName: "config.branding.login_background_url.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.branding.login_background_url.description",
            uiEditorType: "text",
            sortOrder: 1363),

        ConfigurationDefinition.Create(
            key: "branding.login_sso_button_text",
            displayName: "config.branding.login_sso_button_text.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.branding.login_sso_button_text.description",
            uiEditorType: "text",
            sortOrder: 1364),

        ConfigurationDefinition.Create(
            key: "branding.login_help_text",
            displayName: "config.branding.login_help_text.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.branding.login_help_text.description",
            uiEditorType: "text",
            sortOrder: 1365),

        // ── Identity Protection ──

        ConfigurationDefinition.Create(
            key: "branding.powered_by_visible",
            displayName: "config.branding.powered_by_visible.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "config.branding.powered_by_visible.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1370),

        // ── Custom Navigation Links ──

        ConfigurationDefinition.Create(
            key: "platform.custom_links.enabled",
            displayName: "config.platform.custom_links.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.custom_links.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 1380),

        ConfigurationDefinition.Create(
            key: "platform.custom_links.items",
            displayName: "config.platform.custom_links.items.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.custom_links.items.description",
            defaultValue: "[]",
            uiEditorType: "json-editor",
            sortOrder: 1381),

        ConfigurationDefinition.Create(
            key: "platform.custom_links.max_items",
            displayName: "config.platform.custom_links.max_items.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System],
            description: "config.platform.custom_links.max_items.description",
            defaultValue: "5",
            uiEditorType: "number",
            sortOrder: 1382),

        // ── Default Homepage ──

        ConfigurationDefinition.Create(
            key: "platform.homepage.default_route",
            displayName: "config.platform.homepage.default_route.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role],
            description: "config.platform.homepage.default_route.description",
            defaultValue: "/",
            validationRules: """{"pattern":"^/[a-zA-Z0-9/_-]*$"}""",
            uiEditorType: "text",
            sortOrder: 1390),

        ConfigurationDefinition.Create(
            key: "platform.help.url",
            displayName: "config.platform.help.url.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.help.url.description",
            uiEditorType: "text",
            sortOrder: 1391),

        ConfigurationDefinition.Create(
            key: "platform.help.enabled",
            displayName: "config.platform.help.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.help.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1392),

        // ── BLOCK G — Feature Flags ───────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "feature.module.catalog.enabled",
            displayName: "config.feature.module.catalog.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.catalog.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1400),

        ConfigurationDefinition.Create(
            key: "feature.module.contracts.enabled",
            displayName: "config.feature.module.contracts.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.contracts.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1410),

        ConfigurationDefinition.Create(
            key: "feature.module.changes.enabled",
            displayName: "config.feature.module.changes.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.changes.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1420),

        ConfigurationDefinition.Create(
            key: "feature.module.operations.enabled",
            displayName: "config.feature.module.operations.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.operations.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1430),

        ConfigurationDefinition.Create(
            key: "feature.module.ai.enabled",
            displayName: "config.feature.module.ai.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.ai.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1440),

        ConfigurationDefinition.Create(
            key: "feature.module.governance.enabled",
            displayName: "config.feature.module.governance.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.governance.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1450),

        ConfigurationDefinition.Create(
            key: "feature.module.finops.enabled",
            displayName: "config.feature.module.finops.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.finops.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1460),

        ConfigurationDefinition.Create(
            key: "feature.module.integrations.enabled",
            displayName: "config.feature.module.integrations.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.integrations.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1470),

        ConfigurationDefinition.Create(
            key: "feature.module.analytics.enabled",
            displayName: "config.feature.module.analytics.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.module.analytics.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1480),

        ConfigurationDefinition.Create(
            key: "feature.preview.ai_agents.enabled",
            displayName: "config.feature.preview.ai_agents.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.preview.ai_agents.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 1490),

        ConfigurationDefinition.Create(
            key: "feature.preview.environment_comparison.enabled",
            displayName: "config.feature.preview.environment_comparison.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.feature.preview.environment_comparison.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1495),

        // ── BLOCK H — Environment Policies ────────────────────────────────

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_automation",
            displayName: "config.policy.environment.allow_automation.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.allow_automation.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1500),

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_promotion_target",
            displayName: "config.policy.environment.allow_promotion_target.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.allow_promotion_target.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1510),

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_promotion_source",
            displayName: "config.policy.environment.allow_promotion_source.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.allow_promotion_source.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1520),

        ConfigurationDefinition.Create(
            key: "policy.environment.require_approval_for_changes",
            displayName: "config.policy.environment.require_approval_for_changes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.require_approval_for_changes.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1530),

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_drift_analysis",
            displayName: "config.policy.environment.allow_drift_analysis.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.allow_drift_analysis.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1540),

        ConfigurationDefinition.Create(
            key: "policy.environment.restrict_sensitive_features",
            displayName: "config.policy.environment.restrict_sensitive_features.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.restrict_sensitive_features.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1550),

        ConfigurationDefinition.Create(
            key: "policy.environment.change_freeze.enabled",
            displayName: "config.policy.environment.change_freeze.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.change_freeze.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1560),

        ConfigurationDefinition.Create(
            key: "policy.environment.change_freeze.reason",
            displayName: "config.policy.environment.change_freeze.reason.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.policy.environment.change_freeze.reason.description",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 1570),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 3 — WORKFLOW, APPROVALS & PROMOTION GOVERNANCE
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Workflow Types & Templates ────────────────────────

        ConfigurationDefinition.Create(
            key: "workflow.types.enabled",
            displayName: "config.workflow.types.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.types.enabled.description",
            defaultValue: """["ReleaseApproval","PromotionApproval","WaiverApproval","GovernanceReview"]""",
            uiEditorType: "json-editor",
            sortOrder: 2000),

        ConfigurationDefinition.Create(
            key: "workflow.templates.default",
            displayName: "config.workflow.templates.default.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.templates.default.description",
            defaultValue: """{"name":"Standard Approval","stages":[{"name":"Review","order":1,"requiredApprovals":1,"approvalRule":"SingleApprover"}]}""",
            uiEditorType: "json-editor",
            sortOrder: 2010),

        ConfigurationDefinition.Create(
            key: "workflow.templates.by_change_level",
            displayName: "config.workflow.templates.by_change_level.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.templates.by_change_level.description",
            defaultValue: """{"1":"Standard","2":"Enhanced","3":"FullGovernance"}""",
            uiEditorType: "json-editor",
            sortOrder: 2020),

        ConfigurationDefinition.Create(
            key: "workflow.templates.active_version",
            displayName: "config.workflow.templates.active_version.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.templates.active_version.description",
            defaultValue: "1",
            validationRules: """{"min":1,"max":9999}""",
            uiEditorType: "text",
            sortOrder: 2030),

        // ── Block B — Stages, Sequencing & Quorum ──────────────────────

        ConfigurationDefinition.Create(
            key: "workflow.stages.max_count",
            displayName: "config.workflow.stages.max_count.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.stages.max_count.description",
            defaultValue: "10",
            validationRules: """{"min":1,"max":50}""",
            uiEditorType: "text",
            sortOrder: 2100),

        ConfigurationDefinition.Create(
            key: "workflow.stages.allow_parallel",
            displayName: "config.workflow.stages.allow_parallel.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.stages.allow_parallel.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2110),

        ConfigurationDefinition.Create(
            key: "workflow.quorum.default_rule",
            displayName: "config.workflow.quorum.default_rule.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.quorum.default_rule.description",
            defaultValue: "SingleApprover",
            validationRules: """{"enum":["SingleApprover","Majority","Unanimous"]}""",
            uiEditorType: "select",
            sortOrder: 2120),

        ConfigurationDefinition.Create(
            key: "workflow.quorum.minimum_approvers",
            displayName: "config.workflow.quorum.minimum_approvers.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.quorum.minimum_approvers.description",
            defaultValue: "1",
            validationRules: """{"min":1,"max":20}""",
            uiEditorType: "text",
            sortOrder: 2130),

        ConfigurationDefinition.Create(
            key: "workflow.stages.allow_optional",
            displayName: "config.workflow.stages.allow_optional.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.stages.allow_optional.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2140),

        // ── Block C — Approvers, Fallback & Escalation ─────────────────

        ConfigurationDefinition.Create(
            key: "workflow.approvers.policy",
            displayName: "config.workflow.approvers.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.approvers.policy.description",
            defaultValue: """{"strategy":"ByOwnership","roles":["TechLead","Architect"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2200),

        ConfigurationDefinition.Create(
            key: "workflow.approvers.fallback",
            displayName: "config.workflow.approvers.fallback.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.approvers.fallback.description",
            defaultValue: """{"enabled":true,"fallbackRoles":["PlatformAdmin"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2210),

        ConfigurationDefinition.Create(
            key: "workflow.approvers.self_approval_allowed",
            displayName: "config.workflow.approvers.self_approval_allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.approvers.self_approval_allowed.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2220),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.enabled",
            displayName: "config.workflow.escalation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.escalation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2230),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.delay_minutes",
            displayName: "config.workflow.escalation.delay_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.escalation.delay_minutes.description",
            defaultValue: "240",
            validationRules: """{"min":15,"max":10080}""",
            uiEditorType: "text",
            sortOrder: 2240),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.target_roles",
            displayName: "config.workflow.escalation.target_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.escalation.target_roles.description",
            defaultValue: """["PlatformAdmin","Architect"]""",
            uiEditorType: "json-editor",
            sortOrder: 2250),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.by_criticality",
            displayName: "config.workflow.escalation.by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.escalation.by_criticality.description",
            defaultValue: """{"critical":{"delayMinutes":60,"targets":["PlatformAdmin"]},"high":{"delayMinutes":120,"targets":["TechLead"]},"medium":{"delayMinutes":240,"targets":["TechLead"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 2260),

        // ── Block D — SLAs, Deadlines, Timeout & Expiration ────────────

        ConfigurationDefinition.Create(
            key: "workflow.sla.default_hours",
            displayName: "config.workflow.sla.default_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.sla.default_hours.description",
            defaultValue: "48",
            validationRules: """{"min":1,"max":720}""",
            uiEditorType: "text",
            sortOrder: 2300),

        ConfigurationDefinition.Create(
            key: "workflow.sla.by_type",
            displayName: "config.workflow.sla.by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.sla.by_type.description",
            defaultValue: """{"ReleaseApproval":24,"PromotionApproval":8,"WaiverApproval":48,"GovernanceReview":72}""",
            uiEditorType: "json-editor",
            sortOrder: 2310),

        ConfigurationDefinition.Create(
            key: "workflow.sla.by_environment",
            displayName: "config.workflow.sla.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.sla.by_environment.description",
            defaultValue: """{"Production":4,"PreProduction":8}""",
            uiEditorType: "json-editor",
            sortOrder: 2320),

        ConfigurationDefinition.Create(
            key: "workflow.timeout.approval_hours",
            displayName: "config.workflow.timeout.approval_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.timeout.approval_hours.description",
            defaultValue: "72",
            validationRules: """{"min":1,"max":720}""",
            uiEditorType: "text",
            sortOrder: 2330),

        ConfigurationDefinition.Create(
            key: "workflow.expiry.hours",
            displayName: "config.workflow.expiry.hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.expiry.hours.description",
            defaultValue: "168",
            validationRules: """{"min":1,"max":2160}""",
            uiEditorType: "text",
            sortOrder: 2340),

        ConfigurationDefinition.Create(
            key: "workflow.expiry.action",
            displayName: "config.workflow.expiry.action.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.expiry.action.description",
            defaultValue: "Cancel",
            validationRules: """{"enum":["Cancel","Escalate","Notify"]}""",
            uiEditorType: "select",
            sortOrder: 2350),

        ConfigurationDefinition.Create(
            key: "workflow.resubmission.allowed",
            displayName: "config.workflow.resubmission.allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.resubmission.allowed.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2360),

        ConfigurationDefinition.Create(
            key: "workflow.resubmission.max_attempts",
            displayName: "config.workflow.resubmission.max_attempts.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.resubmission.max_attempts.description",
            defaultValue: "3",
            validationRules: """{"min":1,"max":10}""",
            uiEditorType: "text",
            sortOrder: 2370),

        // ── Block E — Gates, Checklists & Auto-Approval ────────────────

        ConfigurationDefinition.Create(
            key: "workflow.gates.enabled",
            displayName: "config.workflow.gates.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.gates.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2400),

        ConfigurationDefinition.Create(
            key: "workflow.gates.by_environment",
            displayName: "config.workflow.gates.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.gates.by_environment.description",
            defaultValue: """{"Production":["SecurityScan","TestCoverage","ApprovalComplete","EvidencePack"],"PreProduction":["TestCoverage","ApprovalComplete"],"Development":[]}""",
            uiEditorType: "json-editor",
            sortOrder: 2410),

        ConfigurationDefinition.Create(
            key: "workflow.gates.by_criticality",
            displayName: "config.workflow.gates.by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.gates.by_criticality.description",
            defaultValue: """{"critical":["SecurityScan","TestCoverage","ApprovalComplete","EvidencePack"],"high":["TestCoverage","ApprovalComplete"],"medium":["ApprovalComplete"],"low":[]}""",
            uiEditorType: "json-editor",
            sortOrder: 2420),

        ConfigurationDefinition.Create(
            key: "workflow.checklist.by_type",
            displayName: "config.workflow.checklist.by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.checklist.by_type.description",
            defaultValue: """{"ReleaseApproval":["ChangeDescriptionReviewed","RiskAssessed","RollbackPlanDefined"],"PromotionApproval":["TargetEnvironmentVerified","DependenciesChecked"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2430),

        ConfigurationDefinition.Create(
            key: "workflow.checklist.by_environment",
            displayName: "config.workflow.checklist.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.checklist.by_environment.description",
            defaultValue: """{"Production":["ProductionReadinessConfirmed","MonitoringVerified","RollbackTested"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2440),

        ConfigurationDefinition.Create(
            key: "workflow.auto_approval.enabled",
            displayName: "config.workflow.auto_approval.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.auto_approval.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2450),

        ConfigurationDefinition.Create(
            key: "workflow.auto_approval.conditions",
            displayName: "config.workflow.auto_approval.conditions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.auto_approval.conditions.description",
            defaultValue: """{"maxChangeLevel":1,"excludeEnvironments":["Production","PreProduction"],"requireAllGatesPassed":true}""",
            uiEditorType: "json-editor",
            sortOrder: 2460),

        ConfigurationDefinition.Create(
            key: "workflow.auto_approval.blocked_environments",
            displayName: "config.workflow.auto_approval.blocked_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.workflow.auto_approval.blocked_environments.description",
            defaultValue: """["Production"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 2470),

        ConfigurationDefinition.Create(
            key: "workflow.evidence_pack.required",
            displayName: "config.workflow.evidence_pack.required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.workflow.evidence_pack.required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2480),

        ConfigurationDefinition.Create(
            key: "workflow.rejection.require_reason",
            displayName: "config.workflow.rejection.require_reason.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.rejection.require_reason.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2490),

        // ── Block F — Promotion Governance ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "promotion.paths.allowed",
            displayName: "config.promotion.paths.allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.promotion.paths.allowed.description",
            defaultValue: """[{"source":"Development","targets":["Test"]},{"source":"Test","targets":["QA"]},{"source":"QA","targets":["PreProduction"]},{"source":"PreProduction","targets":["Production"]}]""",
            uiEditorType: "json-editor",
            sortOrder: 2500),

        ConfigurationDefinition.Create(
            key: "promotion.production.extra_approvers_required",
            displayName: "config.promotion.production.extra_approvers_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.promotion.production.extra_approvers_required.description",
            defaultValue: "1",
            validationRules: """{"min":0,"max":10}""",
            uiEditorType: "text",
            sortOrder: 2510),

        ConfigurationDefinition.Create(
            key: "promotion.production.extra_gates",
            displayName: "config.promotion.production.extra_gates.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.promotion.production.extra_gates.description",
            defaultValue: """["SecurityScan","ComplianceCheck","PerformanceBaseline"]""",
            uiEditorType: "json-editor",
            sortOrder: 2520),

        ConfigurationDefinition.Create(
            key: "promotion.restrictions.by_criticality",
            displayName: "config.promotion.restrictions.by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.promotion.restrictions.by_criticality.description",
            defaultValue: """{"critical":{"requireAdditionalApprovers":2,"requireEvidencePack":true},"high":{"requireAdditionalApprovers":1,"requireEvidencePack":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 2530),

        ConfigurationDefinition.Create(
            key: "promotion.rollback.recommendation_enabled",
            displayName: "config.promotion.rollback.recommendation_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.promotion.rollback.recommendation_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2540),

        // ── Block G — Release Windows & Freeze Policies ────────────────

        ConfigurationDefinition.Create(
            key: "promotion.release_window.enabled",
            displayName: "config.promotion.release_window.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.release_window.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2600),

        ConfigurationDefinition.Create(
            key: "promotion.release_window.schedule",
            displayName: "config.promotion.release_window.schedule.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.release_window.schedule.description",
            defaultValue: """{"days":["Monday","Tuesday","Wednesday","Thursday","Friday"],"startTimeUtc":"06:00","endTimeUtc":"18:00"}""",
            uiEditorType: "json-editor",
            sortOrder: 2610),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.enabled",
            displayName: "config.promotion.freeze.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.freeze.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2620),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.windows",
            displayName: "config.promotion.freeze.windows.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.freeze.windows.description",
            defaultValue: "[]",
            uiEditorType: "json-editor",
            sortOrder: 2630),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.override_allowed",
            displayName: "config.promotion.freeze.override_allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "config.promotion.freeze.override_allowed.description",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 2640),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.override_roles",
            displayName: "config.promotion.freeze.override_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.promotion.freeze.override_roles.description",
            defaultValue: """["PlatformAdmin"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 2650),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 4 — GOVERNANCE, COMPLIANCE, WAIVERS & PACKS
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Policy Catalog & Compliance Profiles ─────────────

        ConfigurationDefinition.Create(
            key: "governance.policies.enabled",
            displayName: "config.governance.policies.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.policies.enabled.description",
            defaultValue: """["SecurityBaseline","ApiVersioning","DocumentationCoverage","TestCoverage","OwnershipRequired"]""",
            uiEditorType: "json-editor",
            sortOrder: 3000),

        ConfigurationDefinition.Create(
            key: "governance.policies.severity",
            displayName: "config.governance.policies.severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.policies.severity.description",
            defaultValue: """{"SecurityBaseline":"Critical","ApiVersioning":"High","DocumentationCoverage":"Medium","TestCoverage":"High","OwnershipRequired":"Critical"}""",
            uiEditorType: "json-editor",
            sortOrder: 3010),

        ConfigurationDefinition.Create(
            key: "governance.policies.criticality",
            displayName: "config.governance.policies.criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.policies.criticality.description",
            defaultValue: """{"SecurityBaseline":"Blocking","ApiVersioning":"NonBlocking","DocumentationCoverage":"Advisory","TestCoverage":"NonBlocking","OwnershipRequired":"Blocking"}""",
            uiEditorType: "json-editor",
            sortOrder: 3020),

        ConfigurationDefinition.Create(
            key: "governance.policies.category_map",
            displayName: "config.governance.policies.category_map.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.policies.category_map.description",
            defaultValue: """{"SecurityBaseline":"Security","ApiVersioning":"Quality","DocumentationCoverage":"Documentation","TestCoverage":"Quality","OwnershipRequired":"Operational"}""",
            uiEditorType: "json-editor",
            sortOrder: 3030),

        ConfigurationDefinition.Create(
            key: "governance.policies.applicability",
            displayName: "config.governance.policies.applicability.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.policies.applicability.description",
            defaultValue: """{"SecurityBaseline":{"applies_to":"all"},"ApiVersioning":{"applies_to":["REST","SOAP"]},"TestCoverage":{"applies_to":"all"}}""",
            uiEditorType: "json-editor",
            sortOrder: 3040),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.enabled",
            displayName: "config.governance.compliance.profiles.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.compliance.profiles.enabled.description",
            defaultValue: """["Standard","Enhanced","Strict"]""",
            uiEditorType: "json-editor",
            sortOrder: 3050),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.default",
            displayName: "config.governance.compliance.profiles.default.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.governance.compliance.profiles.default.description",
            defaultValue: "Standard",
            validationRules: """{"enum":["Standard","Enhanced","Strict"]}""",
            uiEditorType: "select",
            sortOrder: 3060),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.policies_map",
            displayName: "config.governance.compliance.profiles.policies_map.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.compliance.profiles.policies_map.description",
            defaultValue: """{"Standard":["SecurityBaseline","OwnershipRequired"],"Enhanced":["SecurityBaseline","OwnershipRequired","ApiVersioning","TestCoverage"],"Strict":["SecurityBaseline","OwnershipRequired","ApiVersioning","TestCoverage","DocumentationCoverage"]}""",
            uiEditorType: "json-editor",
            sortOrder: 3070),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.by_environment",
            displayName: "config.governance.compliance.profiles.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.compliance.profiles.by_environment.description",
            defaultValue: """{"Production":"Strict","PreProduction":"Enhanced","Development":"Standard"}""",
            uiEditorType: "json-editor",
            sortOrder: 3080),

        // ── Block B — Evidence Requirements ────────────────────────────

        ConfigurationDefinition.Create(
            key: "governance.evidence.types_accepted",
            displayName: "config.governance.evidence.types_accepted.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.evidence.types_accepted.description",
            defaultValue: """["Document","Screenshot","TestReport","ScanResult","AuditLog","Attestation"]""",
            uiEditorType: "json-editor",
            sortOrder: 3100),

        ConfigurationDefinition.Create(
            key: "governance.evidence.required_by_policy",
            displayName: "config.governance.evidence.required_by_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.evidence.required_by_policy.description",
            defaultValue: """{"SecurityBaseline":{"mandatory":true,"types":["ScanResult"],"minCount":1},"TestCoverage":{"mandatory":true,"types":["TestReport"],"minCount":1}}""",
            uiEditorType: "json-editor",
            sortOrder: 3110),

        ConfigurationDefinition.Create(
            key: "governance.evidence.expiry_days",
            displayName: "config.governance.evidence.expiry_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.governance.evidence.expiry_days.description",
            defaultValue: "90",
            validationRules: """{"min":1,"max":730}""",
            uiEditorType: "text",
            sortOrder: 3120),

        ConfigurationDefinition.Create(
            key: "governance.evidence.expiry_by_criticality",
            displayName: "config.governance.evidence.expiry_by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.evidence.expiry_by_criticality.description",
            defaultValue: """{"critical":30,"high":60,"medium":90,"low":180}""",
            uiEditorType: "json-editor",
            sortOrder: 3130),

        ConfigurationDefinition.Create(
            key: "governance.evidence.expired_action",
            displayName: "config.governance.evidence.expired_action.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.evidence.expired_action.description",
            defaultValue: "Notify",
            validationRules: """{"enum":["Notify","Block","Degrade"]}""",
            uiEditorType: "select",
            sortOrder: 3140),

        ConfigurationDefinition.Create(
            key: "governance.evidence.required_by_environment",
            displayName: "config.governance.evidence.required_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.evidence.required_by_environment.description",
            defaultValue: """{"Production":{"mandatory":true,"minCount":1},"PreProduction":{"mandatory":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 3150),

        // ── Block C — Waiver Rules ─────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "governance.waiver.eligible_policies",
            displayName: "config.governance.waiver.eligible_policies.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.eligible_policies.description",
            defaultValue: """["ApiVersioning","DocumentationCoverage","TestCoverage"]""",
            uiEditorType: "json-editor",
            sortOrder: 3200),

        ConfigurationDefinition.Create(
            key: "governance.waiver.blocked_severities",
            displayName: "config.governance.waiver.blocked_severities.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.governance.waiver.blocked_severities.description",
            defaultValue: """["Critical"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 3210),

        ConfigurationDefinition.Create(
            key: "governance.waiver.validity_days_default",
            displayName: "config.governance.waiver.validity_days_default.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.validity_days_default.description",
            defaultValue: "30",
            validationRules: """{"min":1,"max":365}""",
            uiEditorType: "text",
            sortOrder: 3220),

        ConfigurationDefinition.Create(
            key: "governance.waiver.validity_days_max",
            displayName: "config.governance.waiver.validity_days_max.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.validity_days_max.description",
            defaultValue: "90",
            validationRules: """{"min":1,"max":365}""",
            uiEditorType: "text",
            sortOrder: 3230),

        ConfigurationDefinition.Create(
            key: "governance.waiver.require_approval",
            displayName: "config.governance.waiver.require_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.require_approval.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3240),

        ConfigurationDefinition.Create(
            key: "governance.waiver.require_evidence",
            displayName: "config.governance.waiver.require_evidence.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.require_evidence.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3250),

        ConfigurationDefinition.Create(
            key: "governance.waiver.allowed_environments",
            displayName: "config.governance.waiver.allowed_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.allowed_environments.description",
            defaultValue: """["Development","Test","QA"]""",
            uiEditorType: "json-editor",
            sortOrder: 3260),

        ConfigurationDefinition.Create(
            key: "governance.waiver.blocked_environments",
            displayName: "config.governance.waiver.blocked_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.governance.waiver.blocked_environments.description",
            defaultValue: """["Production"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 3270),

        ConfigurationDefinition.Create(
            key: "governance.waiver.renewal.allowed",
            displayName: "config.governance.waiver.renewal.allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.renewal.allowed.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3280),

        ConfigurationDefinition.Create(
            key: "governance.waiver.renewal.max_count",
            displayName: "config.governance.waiver.renewal.max_count.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.waiver.renewal.max_count.description",
            defaultValue: "2",
            validationRules: """{"min":0,"max":10}""",
            uiEditorType: "text",
            sortOrder: 3290),

        // ── Block D — Governance Packs & Bindings ──────────────────────

        ConfigurationDefinition.Create(
            key: "governance.packs.enabled",
            displayName: "config.governance.packs.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.packs.enabled.description",
            defaultValue: """["CoreGovernance","ApiGovernance","SecurityHardening"]""",
            uiEditorType: "json-editor",
            sortOrder: 3300),

        ConfigurationDefinition.Create(
            key: "governance.packs.active_version",
            displayName: "config.governance.packs.active_version.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.packs.active_version.description",
            defaultValue: "1",
            validationRules: """{"min":1,"max":9999}""",
            uiEditorType: "text",
            sortOrder: 3310),

        ConfigurationDefinition.Create(
            key: "governance.packs.binding_policy",
            displayName: "config.governance.packs.binding_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.packs.binding_policy.description",
            defaultValue: """{"bindBy":["tenant","environment","systemType"],"precedence":"most_specific_wins"}""",
            uiEditorType: "json-editor",
            sortOrder: 3320),

        ConfigurationDefinition.Create(
            key: "governance.packs.by_environment",
            displayName: "config.governance.packs.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.packs.by_environment.description",
            defaultValue: """{"Production":["CoreGovernance","SecurityHardening"],"PreProduction":["CoreGovernance"],"Development":["CoreGovernance"]}""",
            uiEditorType: "json-editor",
            sortOrder: 3330),

        ConfigurationDefinition.Create(
            key: "governance.packs.by_system_type",
            displayName: "config.governance.packs.by_system_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.packs.by_system_type.description",
            defaultValue: """{"REST":["ApiGovernance","CoreGovernance"],"SOAP":["ApiGovernance","CoreGovernance"],"Event":["CoreGovernance"],"Background":["CoreGovernance"]}""",
            uiEditorType: "json-editor",
            sortOrder: 3340),

        ConfigurationDefinition.Create(
            key: "governance.packs.overlap_resolution",
            displayName: "config.governance.packs.overlap_resolution.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.packs.overlap_resolution.description",
            defaultValue: "MostRestrictive",
            validationRules: """{"enum":["MostRestrictive","MostSpecific","Merge"]}""",
            uiEditorType: "select",
            sortOrder: 3350),

        // ── Block E — Scorecards, Thresholds & Risk Matrix ─────────────

        ConfigurationDefinition.Create(
            key: "governance.scorecard.enabled",
            displayName: "config.governance.scorecard.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.scorecard.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3400),

        ConfigurationDefinition.Create(
            key: "governance.scorecard.thresholds",
            displayName: "config.governance.scorecard.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.scorecard.thresholds.description",
            defaultValue: """{"Excellent":90,"Good":70,"Fair":50,"Poor":0}""",
            uiEditorType: "json-editor",
            sortOrder: 3410),

        ConfigurationDefinition.Create(
            key: "governance.scorecard.weights",
            displayName: "config.governance.scorecard.weights.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.scorecard.weights.description",
            defaultValue: """{"Security":30,"Quality":25,"Operational":25,"Documentation":20}""",
            uiEditorType: "json-editor",
            sortOrder: 3420),

        ConfigurationDefinition.Create(
            key: "governance.scorecard.thresholds_by_environment",
            displayName: "config.governance.scorecard.thresholds_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.scorecard.thresholds_by_environment.description",
            defaultValue: """{"Production":{"Excellent":95,"Good":80,"Fair":60,"Poor":0},"Development":{"Excellent":80,"Good":60,"Fair":40,"Poor":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 3430),

        ConfigurationDefinition.Create(
            key: "governance.risk.matrix",
            displayName: "config.governance.risk.matrix.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.risk.matrix.description",
            defaultValue: """{"High_High":"Critical","High_Medium":"High","High_Low":"Medium","Medium_High":"High","Medium_Medium":"Medium","Medium_Low":"Low","Low_High":"Medium","Low_Medium":"Low","Low_Low":"Low"}""",
            uiEditorType: "json-editor",
            sortOrder: 3440),

        ConfigurationDefinition.Create(
            key: "governance.risk.thresholds",
            displayName: "config.governance.risk.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.governance.risk.thresholds.description",
            defaultValue: """{"Critical":90,"High":70,"Medium":40,"Low":0}""",
            uiEditorType: "json-editor",
            sortOrder: 3450),

        ConfigurationDefinition.Create(
            key: "governance.risk.labels",
            displayName: "config.governance.risk.labels.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.risk.labels.description",
            defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626"},"High":{"label":"High","color":"#F59E0B"},"Medium":{"label":"Medium","color":"#3B82F6"},"Low":{"label":"Low","color":"#10B981"}}""",
            uiEditorType: "json-editor",
            sortOrder: 3460),

        ConfigurationDefinition.Create(
            key: "governance.risk.thresholds_by_criticality",
            displayName: "config.governance.risk.thresholds_by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.risk.thresholds_by_criticality.description",
            defaultValue: """{"critical":{"Critical":80,"High":60,"Medium":30,"Low":0},"standard":{"Critical":90,"High":70,"Medium":40,"Low":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 3470),

        // ── Block F — Minimum Requirements by System/API Type ──────────

        ConfigurationDefinition.Create(
            key: "governance.requirements.by_system_type",
            displayName: "config.governance.requirements.by_system_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.requirements.by_system_type.description",
            defaultValue: """{"REST":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"mandatoryPack":"ApiGovernance","minScore":70},"SOAP":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"mandatoryPack":"ApiGovernance","minScore":70},"Event":{"mandatoryPolicies":["SecurityBaseline"],"mandatoryPack":"CoreGovernance","minScore":60},"Background":{"mandatoryPolicies":["SecurityBaseline"],"mandatoryPack":"CoreGovernance","minScore":50}}""",
            uiEditorType: "json-editor",
            sortOrder: 3500),

        ConfigurationDefinition.Create(
            key: "governance.requirements.by_api_type",
            displayName: "config.governance.requirements.by_api_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.requirements.by_api_type.description",
            defaultValue: """{"Public":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning","DocumentationCoverage"],"minScore":80},"Internal":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"minScore":60},"Partner":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning","DocumentationCoverage"],"minScore":75}}""",
            uiEditorType: "json-editor",
            sortOrder: 3510),

        ConfigurationDefinition.Create(
            key: "governance.requirements.mandatory_evidence_by_classification",
            displayName: "config.governance.requirements.mandatory_evidence_by_classification.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.requirements.mandatory_evidence_by_classification.description",
            defaultValue: """{"critical":{"types":["ScanResult","TestReport","Attestation"],"minCount":2},"standard":{"types":["ScanResult"],"minCount":1}}""",
            uiEditorType: "json-editor",
            sortOrder: 3520),

        ConfigurationDefinition.Create(
            key: "governance.requirements.min_compliance_profile",
            displayName: "config.governance.requirements.min_compliance_profile.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.requirements.min_compliance_profile.description",
            defaultValue: """{"critical":"Strict","standard":"Standard"}""",
            uiEditorType: "json-editor",
            sortOrder: 3530),

        ConfigurationDefinition.Create(
            key: "governance.requirements.promotion_gates",
            displayName: "config.governance.requirements.promotion_gates.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.governance.requirements.promotion_gates.description",
            defaultValue: """{"Production":{"minScore":70,"requiredProfile":"Enhanced","allBlockingPoliciesMet":true},"PreProduction":{"minScore":50,"allBlockingPoliciesMet":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 3540),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 5 — CATALOG, CONTRACTS, APIS & CHANGE GOVERNANCE
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Contract Types, Versioning & Breaking Change ──────

        ConfigurationDefinition.Create(
            key: "catalog.contract.types_enabled",
            displayName: "config.catalog.contract.types_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.types_enabled.description",
            defaultValue: """["REST","SOAP","GraphQL","gRPC","AsyncAPI","Event","SharedSchema"]""",
            uiEditorType: "json-editor",
            sortOrder: 4000),

        ConfigurationDefinition.Create(
            key: "catalog.contract.api_types_enabled",
            displayName: "config.catalog.contract.api_types_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.api_types_enabled.description",
            defaultValue: """["Public","Internal","Partner","ThirdParty"]""",
            uiEditorType: "json-editor",
            sortOrder: 4010),

        ConfigurationDefinition.Create(
            key: "catalog.contract.versioning_policy",
            displayName: "config.catalog.contract.versioning_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.versioning_policy.description",
            defaultValue: """{"REST":"SemVer","SOAP":"Sequential","GraphQL":"SemVer","gRPC":"SemVer","AsyncAPI":"SemVer","Event":"SemVer","SharedSchema":"SemVer"}""",
            uiEditorType: "json-editor",
            sortOrder: 4020),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_change_policy",
            displayName: "config.catalog.contract.breaking_change_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.breaking_change_policy.description",
            defaultValue: """{"REST":"RequireApproval","SOAP":"Block","GraphQL":"RequireApproval","gRPC":"RequireApproval","AsyncAPI":"Warn","Event":"Warn","SharedSchema":"Block"}""",
            uiEditorType: "json-editor",
            sortOrder: 4030),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_change_severity",
            displayName: "config.catalog.contract.breaking_change_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.breaking_change_severity.description",
            defaultValue: "High",
            validationRules: """{"enum":["Critical","High","Medium","Low"]}""",
            uiEditorType: "select",
            sortOrder: 4040),

        ConfigurationDefinition.Create(
            key: "catalog.contract.version_increment_rules",
            displayName: "config.catalog.contract.version_increment_rules.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.version_increment_rules.description",
            defaultValue: """{"breakingChange":"major","newFeature":"minor","bugfix":"patch","documentation":"patch"}""",
            uiEditorType: "json-editor",
            sortOrder: 4050),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_promotion_restriction",
            displayName: "config.catalog.contract.breaking_promotion_restriction.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.contract.breaking_promotion_restriction.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4060),

        ConfigurationDefinition.Create(
            key: "catalog.contract.max_active_versions",
            displayName: "config.catalog.contract.max_active_versions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.contract.max_active_versions.description",
            defaultValue: "2",
            validationRules: """{"min":1,"max":10}""",
            uiEditorType: "text",
            sortOrder: 4070),

        ConfigurationDefinition.Create(
            key: "catalog.contract.require_approval_on_change",
            displayName: "config.catalog.contract.require_approval_on_change.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.contract.require_approval_on_change.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4071),

        ConfigurationDefinition.Create(
            key: "catalog.service.require_approval_on_registration",
            displayName: "config.catalog.service.require_approval_on_registration.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.service.require_approval_on_registration.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4072),

        // ── Block B — Validation, Linting, Rulesets & Templates ────────

        ConfigurationDefinition.Create(
            key: "catalog.validation.lint_severity_defaults",
            displayName: "config.catalog.validation.lint_severity_defaults.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.validation.lint_severity_defaults.description",
            defaultValue: """{"missingDescription":"warn","missingExample":"info","unusedSchema":"warn","invalidReference":"error","securitySchemeUndefined":"error"}""",
            uiEditorType: "json-editor",
            sortOrder: 4100),

        ConfigurationDefinition.Create(
            key: "catalog.validation.rulesets_by_contract_type",
            displayName: "config.catalog.validation.rulesets_by_contract_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.validation.rulesets_by_contract_type.description",
            defaultValue: """{"REST":["openapi-standard","security-best-practices"],"SOAP":["wsdl-compliance"],"GraphQL":["graphql-best-practices"],"gRPC":["protobuf-lint"],"AsyncAPI":["asyncapi-standard"],"Event":["event-schema-validation"],"SharedSchema":["schema-consistency"]}""",
            uiEditorType: "json-editor",
            sortOrder: 4110),

        ConfigurationDefinition.Create(
            key: "catalog.validation.blocking_vs_warning",
            displayName: "config.catalog.validation.blocking_vs_warning.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.validation.blocking_vs_warning.description",
            defaultValue: """{"blocking":["invalidReference","securitySchemeUndefined"],"warning":["missingDescription","missingExample","unusedSchema"]}""",
            uiEditorType: "json-editor",
            sortOrder: 4120),

        ConfigurationDefinition.Create(
            key: "catalog.validation.min_validations_by_type",
            displayName: "config.catalog.validation.min_validations_by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.validation.min_validations_by_type.description",
            defaultValue: """{"REST":{"schemaValid":true,"securityDefined":true,"pathsDocumented":true},"SOAP":{"wsdlValid":true},"GraphQL":{"schemaValid":true},"gRPC":{"protoValid":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4130),

        ConfigurationDefinition.Create(
            key: "catalog.templates.by_contract_type",
            displayName: "config.catalog.templates.by_contract_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.templates.by_contract_type.description",
            defaultValue: """{"REST":"openapi-3.1-standard","SOAP":"wsdl-2.0-standard","GraphQL":"graphql-standard","gRPC":"protobuf-standard","AsyncAPI":"asyncapi-2.6-standard","Event":"cloudevents-standard","SharedSchema":"json-schema-standard"}""",
            uiEditorType: "json-editor",
            sortOrder: 4140),

        ConfigurationDefinition.Create(
            key: "catalog.templates.metadata_defaults",
            displayName: "config.catalog.templates.metadata_defaults.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.templates.metadata_defaults.description",
            defaultValue: """{"license":"Proprietary","termsOfService":"","contact":"","externalDocs":""}""",
            uiEditorType: "json-editor",
            sortOrder: 4150),

        // ── Block C — Minimum Requirements & Publication ────────────────

        ConfigurationDefinition.Create(
            key: "catalog.requirements.owner_required",
            displayName: "config.catalog.requirements.owner_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.owner_required.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4200),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.changelog_required",
            displayName: "config.catalog.requirements.changelog_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.changelog_required.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4210),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.glossary_required",
            displayName: "config.catalog.requirements.glossary_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.glossary_required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 4220),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.use_cases_required",
            displayName: "config.catalog.requirements.use_cases_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.use_cases_required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 4230),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.min_documentation",
            displayName: "config.catalog.requirements.min_documentation.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.min_documentation.description",
            defaultValue: """{"descriptionMinLength":20,"operationDescriptions":true,"responseExamples":false,"errorDocumentation":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4240),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.min_catalog_fields",
            displayName: "config.catalog.requirements.min_catalog_fields.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.min_catalog_fields.description",
            defaultValue: """{"name":true,"description":true,"owner":true,"team":true,"domain":false,"tier":false,"lifecycle":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4250),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.min_contract_fields",
            displayName: "config.catalog.requirements.min_contract_fields.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.min_contract_fields.description",
            defaultValue: """{"title":true,"version":true,"description":true,"servers":true,"securityScheme":true,"contact":false}""",
            uiEditorType: "json-editor",
            sortOrder: 4260),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.by_contract_type",
            displayName: "config.catalog.requirements.by_contract_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.by_contract_type.description",
            defaultValue: """{"REST":{"securityScheme":true,"pathDescriptions":true},"SOAP":{"wsdlValid":true},"GraphQL":{"schemaValid":true},"gRPC":{"protoValid":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4270),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.by_environment",
            displayName: "config.catalog.requirements.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.by_environment.description",
            defaultValue: """{"Production":{"ownerRequired":true,"changelogRequired":true,"minDocumentation":true,"allBlockingValidationsPass":true},"Development":{"ownerRequired":false,"changelogRequired":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4280),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.by_criticality",
            displayName: "config.catalog.requirements.by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.requirements.by_criticality.description",
            defaultValue: """{"critical":{"ownerRequired":true,"changelogRequired":true,"glossaryRequired":true,"useCasesRequired":true},"standard":{"ownerRequired":true,"changelogRequired":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4290),

        // ── Block D — Publication & Promotion Policy ───────────────────

        ConfigurationDefinition.Create(
            key: "catalog.publication.pre_publish_review",
            displayName: "config.catalog.publication.pre_publish_review.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.publication.pre_publish_review.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4300),

        ConfigurationDefinition.Create(
            key: "catalog.publication.visibility_defaults",
            displayName: "config.catalog.publication.visibility_defaults.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.publication.visibility_defaults.description",
            defaultValue: """{"Internal":"team","Public":"organization","Partner":"restricted"}""",
            uiEditorType: "json-editor",
            sortOrder: 4310),

        ConfigurationDefinition.Create(
            key: "catalog.publication.portal_defaults",
            displayName: "config.catalog.publication.portal_defaults.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.publication.portal_defaults.description",
            defaultValue: """{"autoPublishToPortal":true,"includeExamples":true,"includeChangelog":true,"includeTryItOut":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4320),

        ConfigurationDefinition.Create(
            key: "catalog.publication.promotion_readiness",
            displayName: "config.catalog.publication.promotion_readiness.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.publication.promotion_readiness.description",
            defaultValue: """{"allBlockingValidationsPass":true,"ownerAssigned":true,"changelogUpdated":true,"noUnresolvedBreakingChanges":true,"minGovernanceScore":60}""",
            uiEditorType: "json-editor",
            sortOrder: 4330),

        ConfigurationDefinition.Create(
            key: "catalog.publication.gating_by_environment",
            displayName: "config.catalog.publication.gating_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.publication.gating_by_environment.description",
            defaultValue: """{"Production":{"requireApproval":true,"requireAllGatesPass":true},"PreProduction":{"requireApproval":false,"requireAllGatesPass":true},"Development":{"requireApproval":false,"requireAllGatesPass":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4340),

        // ── Block E — Import/Export Policy ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "catalog.import.types_allowed",
            displayName: "config.catalog.import.types_allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.import.types_allowed.description",
            defaultValue: """{"fileUpload":["OpenAPI","WSDL","GraphQL","Protobuf","AsyncAPI","JSONSchema"],"urlImport":["OpenAPI","AsyncAPI"],"gitSync":["OpenAPI","AsyncAPI","Protobuf"]}""",
            uiEditorType: "json-editor",
            sortOrder: 4400),

        ConfigurationDefinition.Create(
            key: "catalog.export.types_allowed",
            displayName: "config.catalog.export.types_allowed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.export.types_allowed.description",
            defaultValue: """["OpenAPI-JSON","OpenAPI-YAML","WSDL","GraphQL-SDL","Protobuf","AsyncAPI-YAML","Markdown","HTML"]""",
            uiEditorType: "json-editor",
            sortOrder: 4410),

        ConfigurationDefinition.Create(
            key: "catalog.import.overwrite_policy",
            displayName: "config.catalog.import.overwrite_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.import.overwrite_policy.description",
            defaultValue: "AskUser",
            validationRules: """{"enum":["Merge","Overwrite","Block","AskUser"]}""",
            uiEditorType: "select",
            sortOrder: 4420),

        ConfigurationDefinition.Create(
            key: "catalog.import.validation_on_import",
            displayName: "config.catalog.import.validation_on_import.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.import.validation_on_import.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4430),

        // ── Block F — Change Types, Criticality & Blast Radius ─────────

        ConfigurationDefinition.Create(
            key: "change.types_enabled",
            displayName: "config.change.types_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.types_enabled.description",
            defaultValue: """["Feature","Bugfix","Hotfix","Refactor","Config","Infrastructure","Rollback"]""",
            uiEditorType: "json-editor",
            sortOrder: 4500),

        ConfigurationDefinition.Create(
            key: "change.criticality_defaults",
            displayName: "config.change.criticality_defaults.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.criticality_defaults.description",
            defaultValue: """{"Feature":"Medium","Bugfix":"Medium","Hotfix":"Critical","Refactor":"Low","Config":"Low","Infrastructure":"High","Rollback":"Critical"}""",
            uiEditorType: "json-editor",
            sortOrder: 4510),

        ConfigurationDefinition.Create(
            key: "change.risk_classification",
            displayName: "config.change.risk_classification.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.risk_classification.description",
            defaultValue: """{"Hotfix":{"baseRisk":"High","requiresApproval":true},"Infrastructure":{"baseRisk":"High","requiresApproval":true},"Feature":{"baseRisk":"Medium","requiresApproval":false},"Rollback":{"baseRisk":"High","requiresApproval":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4520),

        ConfigurationDefinition.Create(
            key: "change.blast_radius.thresholds",
            displayName: "config.change.blast_radius.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.blast_radius.thresholds.description",
            defaultValue: """{"Critical":90,"High":70,"Medium":40,"Low":0}""",
            uiEditorType: "json-editor",
            sortOrder: 4530),

        ConfigurationDefinition.Create(
            key: "change.blast_radius.categories",
            displayName: "config.change.blast_radius.categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.blast_radius.categories.description",
            defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626","action":"RequireApproval"},"High":{"label":"High","color":"#F59E0B","action":"RequireReview"},"Medium":{"label":"Medium","color":"#3B82F6","action":"Notify"},"Low":{"label":"Low","color":"#10B981","action":"AutoApprove"}}""",
            uiEditorType: "json-editor",
            sortOrder: 4540),

        ConfigurationDefinition.Create(
            key: "change.blast_radius.environment_weights",
            displayName: "config.change.blast_radius.environment_weights.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.blast_radius.environment_weights.description",
            defaultValue: """{"Production":1.0,"PreProduction":0.6,"Staging":0.4,"Development":0.2}""",
            uiEditorType: "json-editor",
            sortOrder: 4550),

        ConfigurationDefinition.Create(
            key: "change.severity_criteria",
            displayName: "config.change.severity_criteria.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.severity_criteria.description",
            defaultValue: """{"affectedServicesHigh":5,"affectedDependenciesHigh":10,"crossDomainChange":true,"dataSchemaChange":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4560),

        // ── Block G — Release Scoring, Evidence Pack & Rollback ────────

        ConfigurationDefinition.Create(
            key: "change.release_score.weights",
            displayName: "config.change.release_score.weights.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release_score.weights.description",
            defaultValue: """{"testCoverage":20,"codeReview":15,"blastRadius":20,"historicalSuccess":15,"documentationComplete":10,"governanceCompliance":10,"evidencePack":10}""",
            uiEditorType: "json-editor",
            sortOrder: 4600),

        ConfigurationDefinition.Create(
            key: "change.release_score.thresholds",
            displayName: "config.change.release_score.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release_score.thresholds.description",
            defaultValue: """{"HighConfidence":80,"Moderate":60,"LowConfidence":40,"Block":0}""",
            uiEditorType: "json-editor",
            sortOrder: 4610),

        ConfigurationDefinition.Create(
            key: "change.evidence_pack.required",
            displayName: "config.change.evidence_pack.required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.evidence_pack.required.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4620),

        ConfigurationDefinition.Create(
            key: "change.evidence_pack.requirements",
            displayName: "config.change.evidence_pack.requirements.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.evidence_pack.requirements.description",
            defaultValue: """{"Production":{"testReport":true,"securityScan":true,"approvalRecord":true,"rollbackPlan":true},"PreProduction":{"testReport":true,"securityScan":false,"approvalRecord":false},"Development":{"testReport":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4630),

        ConfigurationDefinition.Create(
            key: "change.evidence_pack.by_criticality",
            displayName: "config.change.evidence_pack.by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.evidence_pack.by_criticality.description",
            defaultValue: """{"Critical":{"securityScan":true,"approvalRecord":true,"rollbackPlan":true,"impactAnalysis":true},"High":{"securityScan":true,"approvalRecord":true,"rollbackPlan":true},"Medium":{"testReport":true},"Low":{}}""",
            uiEditorType: "json-editor",
            sortOrder: 4640),

        ConfigurationDefinition.Create(
            key: "change.rollback.recommendation_policy",
            displayName: "config.change.rollback.recommendation_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.rollback.recommendation_policy.description",
            defaultValue: """{"autoRecommendOnScoreBelow":40,"autoRecommendOnIncidentCorrelation":true,"requireRollbackPlanForProduction":true,"requireRollbackPlanForCriticalChanges":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4650),

        ConfigurationDefinition.Create(
            key: "change.release_calendar.window_policy",
            displayName: "config.change.release_calendar.window_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release_calendar.window_policy.description",
            defaultValue: """{"Hotfix":{"allowOutsideWindow":true,"requireApproval":true},"Feature":{"allowOutsideWindow":false,"requireApproval":false},"Infrastructure":{"allowOutsideWindow":false,"requireApproval":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4660),

        ConfigurationDefinition.Create(
            key: "change.release_calendar.by_environment",
            displayName: "config.change.release_calendar.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release_calendar.by_environment.description",
            defaultValue: """{"Production":{"allowedDays":["Monday","Tuesday","Wednesday","Thursday"],"blockedHours":{"start":"18:00","end":"08:00"},"requireWindow":true},"PreProduction":{"requireWindow":false},"Development":{"requireWindow":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4670),

        ConfigurationDefinition.Create(
            key: "change.incident_correlation.enabled",
            displayName: "config.change.incident_correlation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.incident_correlation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4680),

        ConfigurationDefinition.Create(
            key: "change.incident_correlation.window_hours",
            displayName: "config.change.incident_correlation.window_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.incident_correlation.window_hours.description",
            defaultValue: "24",
            validationRules: """{"min":1,"max":168}""",
            uiEditorType: "text",
            sortOrder: 4690),

        ConfigurationDefinition.Create(
            key: "change.release.train.max_items_per_train",
            displayName: "config.change.release.train.max_items_per_train.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release.train.max_items_per_train.description",
            defaultValue: "50",
            validationRules: """{"min":1,"max":500}""",
            uiEditorType: "text",
            sortOrder: 4700),

        ConfigurationDefinition.Create(
            key: "change.release.train.auto_close_on_all_deployed",
            displayName: "config.change.release.train.auto_close_on_all_deployed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release.train.auto_close_on_all_deployed.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4710),

        ConfigurationDefinition.Create(
            key: "change.release.notes.ai_generation_model",
            displayName: "config.change.release.notes.ai_generation_model.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release.notes.ai_generation_model.description",
            defaultValue: "local-default",
            uiEditorType: "text",
            sortOrder: 4720),

        ConfigurationDefinition.Create(
            key: "change.release.gates.max_pending_days_before_alert",
            displayName: "config.change.release.gates.max_pending_days_before_alert.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.gates.max_pending_days_before_alert.description",
            defaultValue: "3",
            validationRules: """{"min":1,"max":30}""",
            uiEditorType: "text",
            sortOrder: 4730),

        ConfigurationDefinition.Create(
            key: "change.release.freeze_window.default_duration_hours",
            displayName: "config.change.release.freeze_window.default_duration_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.freeze_window.default_duration_hours.description",
            defaultValue: "72",
            validationRules: """{"min":1,"max":720}""",
            uiEditorType: "text",
            sortOrder: 4740),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 6 — OPERATIONS, INCIDENTS, FINOPS & BENCHMARKING
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Incident Taxonomy, Severity, Criticality & SLA ───

        ConfigurationDefinition.Create(
            key: "incidents.taxonomy.categories",
            displayName: "config.incidents.taxonomy.categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.taxonomy.categories.description",
            defaultValue: """["Infrastructure","Application","Security","Data","Network","ThirdParty"]""",
            uiEditorType: "json-editor",
            sortOrder: 5000),

        ConfigurationDefinition.Create(
            key: "incidents.taxonomy.types",
            displayName: "config.incidents.taxonomy.types.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.taxonomy.types.description",
            defaultValue: """["Outage","Degradation","Latency","ErrorSpike","SecurityBreach","DataLoss","ConfigDrift"]""",
            uiEditorType: "json-editor",
            sortOrder: 5010),

        ConfigurationDefinition.Create(
            key: "incidents.severity.defaults_by_type",
            displayName: "config.incidents.severity.defaults_by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.severity.defaults_by_type.description",
            defaultValue: """{"Outage":"Critical","Degradation":"High","Latency":"Medium","ErrorSpike":"High","SecurityBreach":"Critical","DataLoss":"Critical","ConfigDrift":"Low"}""",
            uiEditorType: "json-editor",
            sortOrder: 5020),

        ConfigurationDefinition.Create(
            key: "incidents.severity.defaults_by_category",
            displayName: "config.incidents.severity.defaults_by_category.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.severity.defaults_by_category.description",
            defaultValue: """{"Infrastructure":"High","Application":"Medium","Security":"Critical","Data":"High","Network":"High","ThirdParty":"Medium"}""",
            uiEditorType: "json-editor",
            sortOrder: 5030),

        ConfigurationDefinition.Create(
            key: "incidents.criticality.defaults",
            displayName: "config.incidents.criticality.defaults.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.criticality.defaults.description",
            defaultValue: """{"Outage_Infrastructure":"Critical","SecurityBreach_Security":"Critical","Degradation_Application":"High","Latency_Network":"Medium","ConfigDrift_Application":"Low"}""",
            uiEditorType: "json-editor",
            sortOrder: 5040),

        ConfigurationDefinition.Create(
            key: "incidents.severity.mapping",
            displayName: "config.incidents.severity.mapping.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.severity.mapping.description",
            defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626","weight":4},"High":{"label":"High","color":"#F59E0B","weight":3},"Medium":{"label":"Medium","color":"#3B82F6","weight":2},"Low":{"label":"Low","color":"#10B981","weight":1}}""",
            uiEditorType: "json-editor",
            sortOrder: 5050),

        ConfigurationDefinition.Create(
            key: "incidents.sla.by_severity",
            displayName: "config.incidents.sla.by_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.incidents.sla.by_severity.description",
            defaultValue: """{"Critical":{"acknowledgementMinutes":5,"firstResponseMinutes":15,"resolutionMinutes":240},"High":{"acknowledgementMinutes":15,"firstResponseMinutes":60,"resolutionMinutes":480},"Medium":{"acknowledgementMinutes":60,"firstResponseMinutes":240,"resolutionMinutes":1440},"Low":{"acknowledgementMinutes":240,"firstResponseMinutes":480,"resolutionMinutes":4320}}""",
            uiEditorType: "json-editor",
            sortOrder: 5060),

        ConfigurationDefinition.Create(
            key: "incidents.sla.by_environment",
            displayName: "config.incidents.sla.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.sla.by_environment.description",
            defaultValue: """{"Production":{"multiplier":1.0},"PreProduction":{"multiplier":2.0},"Staging":{"multiplier":3.0},"Development":{"multiplier":5.0}}""",
            uiEditorType: "json-editor",
            sortOrder: 5070),

        ConfigurationDefinition.Create(
            key: "incidents.sla.production_behavior",
            displayName: "config.incidents.sla.production_behavior.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.sla.production_behavior.description",
            defaultValue: """{"Critical":{"autoEscalate":true,"pageOnCall":true,"requirePostMortem":true},"High":{"autoEscalate":true,"pageOnCall":false,"requirePostMortem":true},"Medium":{"autoEscalate":false},"Low":{"autoEscalate":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 5080),

        // ── Block B — Owners, Classification, Correlation & Auto-Incident ─

        ConfigurationDefinition.Create(
            key: "incidents.owner.default_by_category",
            displayName: "config.incidents.owner.default_by_category.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.owner.default_by_category.description",
            defaultValue: """{"Infrastructure":"platform-ops","Application":"service-owner","Security":"security-team","Data":"data-engineering","Network":"network-ops","ThirdParty":"vendor-management"}""",
            uiEditorType: "json-editor",
            sortOrder: 5100),

        ConfigurationDefinition.Create(
            key: "incidents.owner.fallback",
            displayName: "config.incidents.owner.fallback.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.owner.fallback.description",
            defaultValue: "platform-admin",
            uiEditorType: "text",
            sortOrder: 5110),

        ConfigurationDefinition.Create(
            key: "incidents.classification.auto_enabled",
            displayName: "config.incidents.classification.auto_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.classification.auto_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5120),

        ConfigurationDefinition.Create(
            key: "incidents.correlation.policy",
            displayName: "config.incidents.correlation.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.correlation.policy.description",
            defaultValue: """{"correlateByService":true,"correlateByEnvironment":true,"correlateBySeverity":false,"correlationWindowMinutes":30,"correlationKeyFields":["service","environment","alertType"]}""",
            uiEditorType: "json-editor",
            sortOrder: 5130),

        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.enabled",
            displayName: "config.incidents.auto_creation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.incidents.auto_creation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5140),

        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.policy",
            displayName: "config.incidents.auto_creation.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.auto_creation.policy.description",
            defaultValue: """{"minSeverityForAutoCreate":"High","maxAutoIncidentsPerHour":10,"requireCorrelationMatch":true,"blockedCategories":[]}""",
            uiEditorType: "json-editor",
            sortOrder: 5150),

        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.blocked_environments",
            displayName: "config.incidents.auto_creation.blocked_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.incidents.auto_creation.blocked_environments.description",
            defaultValue: """[]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 5160),

        ConfigurationDefinition.Create(
            key: "incidents.enrichment.enabled",
            displayName: "config.incidents.enrichment.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.enrichment.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5170),

        // ── Block C — Playbooks, Runbooks & Operational Automation ─────

        ConfigurationDefinition.Create(
            key: "operations.playbook.defaults_by_type",
            displayName: "config.operations.playbook.defaults_by_type.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.playbook.defaults_by_type.description",
            defaultValue: """{"Outage":"playbook-outage-standard","Degradation":"playbook-degradation-triage","SecurityBreach":"playbook-security-response","DataLoss":"playbook-data-recovery","Latency":"playbook-performance-investigation"}""",
            uiEditorType: "json-editor",
            sortOrder: 5200),

        ConfigurationDefinition.Create(
            key: "operations.runbook.defaults_by_category",
            displayName: "config.operations.runbook.defaults_by_category.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.runbook.defaults_by_category.description",
            defaultValue: """{"Infrastructure":"runbook-infra-ops","Application":"runbook-app-debug","Security":"runbook-sec-incident","Network":"runbook-network-diag"}""",
            uiEditorType: "json-editor",
            sortOrder: 5210),

        ConfigurationDefinition.Create(
            key: "operations.playbook.required_by_environment",
            displayName: "config.operations.playbook.required_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.playbook.required_by_environment.description",
            defaultValue: """{"Production":true,"PreProduction":false,"Development":false}""",
            uiEditorType: "json-editor",
            sortOrder: 5220),

        ConfigurationDefinition.Create(
            key: "operations.playbook.required_by_criticality",
            displayName: "config.operations.playbook.required_by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.playbook.required_by_criticality.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5230),

        ConfigurationDefinition.Create(
            key: "operations.automation.enabled_by_environment",
            displayName: "config.operations.automation.enabled_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.automation.enabled_by_environment.description",
            defaultValue: """{"Production":{"autoRestart":false,"autoScale":false,"autoRemediate":false},"PreProduction":{"autoRestart":true,"autoScale":true,"autoRemediate":false},"Development":{"autoRestart":true,"autoScale":true,"autoRemediate":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5240),

        ConfigurationDefinition.Create(
            key: "operations.automation.blocked_in_production",
            displayName: "config.operations.automation.blocked_in_production.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.operations.automation.blocked_in_production.description",
            defaultValue: """["autoRemediate","autoDeleteResources","autoModifyData"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 5250),

        ConfigurationDefinition.Create(
            key: "operations.automation.by_severity",
            displayName: "config.operations.automation.by_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.automation.by_severity.description",
            defaultValue: """{"Critical":["autoNotify","autoEscalate"],"High":["autoNotify","autoEscalate","autoRestart"],"Medium":["autoNotify","autoRestart"],"Low":["autoNotify"]}""",
            uiEditorType: "json-editor",
            sortOrder: 5260),

        ConfigurationDefinition.Create(
            key: "operations.postincident.template_enabled",
            displayName: "config.operations.postincident.template_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.postincident.template_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5270),

        // ── Block D — FinOps Budgets & Thresholds ──────────────────────

        ConfigurationDefinition.Create(
            key: "finops.budget.default_currency",
            displayName: "config.finops.budget.default_currency.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.default_currency.description",
            defaultValue: "USD",
            validationRules: """{"maxLength":3,"minLength":3}""",
            uiEditorType: "text",
            sortOrder: 5300),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_tenant",
            displayName: "config.finops.budget.by_tenant.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.by_tenant.description",
            defaultValue: """{"default":{"monthlyBudget":10000,"alertOnExceed":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5310),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_team",
            displayName: "config.finops.budget.by_team.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.by_team.description",
            defaultValue: """{"default":{"monthlyBudget":5000,"alertOnExceed":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5320),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_service",
            displayName: "config.finops.budget.by_service.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.by_service.description",
            defaultValue: """{"default":{"monthlyBudget":2000,"alertOnExceed":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5330),

        ConfigurationDefinition.Create(
            key: "finops.budget.alert_thresholds",
            displayName: "config.finops.budget.alert_thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.finops.budget.alert_thresholds.description",
            defaultValue: """[{"percent":80,"severity":"Low","action":"Notify"},{"percent":90,"severity":"Medium","action":"Notify"},{"percent":100,"severity":"High","action":"NotifyAndBlock"},{"percent":120,"severity":"Critical","action":"Escalate"}]""",
            uiEditorType: "json-editor",
            sortOrder: 5340),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_environment",
            displayName: "config.finops.budget.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.by_environment.description",
            defaultValue: """{"Production":{"monthlyBudget":8000,"hardLimit":true},"PreProduction":{"monthlyBudget":3000,"hardLimit":false},"Development":{"monthlyBudget":1000,"hardLimit":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 5350),

        ConfigurationDefinition.Create(
            key: "finops.budget.periodicity",
            displayName: "config.finops.budget.periodicity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.periodicity.description",
            defaultValue: "Monthly",
            validationRules: """{"enum":["Monthly","Quarterly","Yearly"]}""",
            uiEditorType: "select",
            sortOrder: 5360),

        ConfigurationDefinition.Create(
            key: "finops.budget.rollover_enabled",
            displayName: "config.finops.budget.rollover_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.rollover_enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 5370),

        // ── Block E2 — Release Budget Gate ─────────────────────────────

        ConfigurationDefinition.Create(
            key: "finops.release.budget_gate.enabled",
            displayName: "config.finops.release.budget_gate.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.finops.release.budget_gate.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 5380),

        ConfigurationDefinition.Create(
            key: "finops.release.budget_gate.block_on_exceed",
            displayName: "config.finops.release.budget_gate.block_on_exceed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.finops.release.budget_gate.block_on_exceed.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5382),

        ConfigurationDefinition.Create(
            key: "finops.release.budget_gate.require_approval",
            displayName: "config.finops.release.budget_gate.require_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.finops.release.budget_gate.require_approval.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5384),

        ConfigurationDefinition.Create(
            key: "finops.release.budget_gate.approvers",
            displayName: "config.finops.release.budget_gate.approvers.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.release.budget_gate.approvers.description",
            defaultValue: "[]",
            uiEditorType: "json-editor",
            sortOrder: 5386),

        // ── Block F — Anomaly, Waste & Financial Recommendations ───────

        ConfigurationDefinition.Create(
            key: "finops.anomaly.detection_enabled",
            displayName: "config.finops.anomaly.detection_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.anomaly.detection_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5400),

        ConfigurationDefinition.Create(
            key: "finops.anomaly.thresholds",
            displayName: "config.finops.anomaly.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.anomaly.thresholds.description",
            defaultValue: """{"warning":20,"high":50,"critical":100}""",
            uiEditorType: "json-editor",
            sortOrder: 5410),

        ConfigurationDefinition.Create(
            key: "finops.anomaly.comparison_window_days",
            displayName: "config.finops.anomaly.comparison_window_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.anomaly.comparison_window_days.description",
            defaultValue: "30",
            validationRules: """{"min":7,"max":90}""",
            uiEditorType: "text",
            sortOrder: 5420),

        ConfigurationDefinition.Create(
            key: "finops.waste.detection_enabled",
            displayName: "config.finops.waste.detection_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.waste.detection_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5430),

        ConfigurationDefinition.Create(
            key: "finops.waste.thresholds",
            displayName: "config.finops.waste.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.waste.thresholds.description",
            defaultValue: """{"idleResourcePercent":90,"underutilizationPercent":20,"unusedDaysThreshold":14,"percentileThreshold":75,"overProvisionedCostRatio":3.0,"idleCostlyRatio":2.0,"mediumSeverityFraction":0.5}""",
            uiEditorType: "json-editor",
            sortOrder: 5440),

        ConfigurationDefinition.Create(
            key: "finops.waste.categories",
            displayName: "config.finops.waste.categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.waste.categories.description",
            defaultValue: """["IdleResources","Overprovisioned","UnattachedStorage","UnusedLicenses","OrphanedResources","OverlappingServices"]""",
            uiEditorType: "json-editor",
            sortOrder: 5450),

        ConfigurationDefinition.Create(
            key: "finops.recommendation.policy",
            displayName: "config.finops.recommendation.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.recommendation.policy.description",
            defaultValue: """{"autoRecommend":true,"minSavingsThreshold":50,"savingsRatePct":35,"showInDashboard":true,"notifyOnHighSavings":true,"highSavingsThreshold":500}""",
            uiEditorType: "json-editor",
            sortOrder: 5460),

        ConfigurationDefinition.Create(
            key: "finops.notification.policy",
            displayName: "config.finops.notification.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.notification.policy.description",
            defaultValue: """{"notifyOnAnomaly":true,"notifyOnBudgetBreach":true,"notifyOnWasteDetected":true,"notifyOnRecommendation":false,"digestFrequency":"Weekly"}""",
            uiEditorType: "json-editor",
            sortOrder: 5470),

        ConfigurationDefinition.Create(
            key: "finops.anomaly.by_criticality",
            displayName: "config.finops.anomaly.by_criticality.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.anomaly.by_criticality.description",
            defaultValue: """{"critical":{"warningDeviation":10,"autoEscalate":true},"standard":{"warningDeviation":20,"autoEscalate":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 5480),

        // ── FinOps Efficiency Bands & Thresholds ──────────────────────

        ConfigurationDefinition.Create(
            key: "finops.efficiency.trend_threshold_pct",
            displayName: "config.finops.efficiency.trend_threshold_pct.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.efficiency.trend_threshold_pct.description",
            defaultValue: "5.0",
            validationRules: """{"min": 0.1, "max": 100}""",
            uiEditorType: "text",
            sortOrder: 5490),

        ConfigurationDefinition.Create(
            key: "finops.efficiency.cost_bands",
            displayName: "config.finops.efficiency.cost_bands.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.efficiency.cost_bands.description",
            defaultValue: """{"Wasteful":15000,"Inefficient":10000,"Acceptable":5000}""",
            uiEditorType: "json-editor",
            sortOrder: 5495),

        ConfigurationDefinition.Create(
            key: "finops.efficiency.burn_rate_thresholds",
            displayName: "config.finops.efficiency.burn_rate_thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.efficiency.burn_rate_thresholds.description",
            defaultValue: """{"elevated":1.5,"critical":2.0}""",
            uiEditorType: "json-editor",
            sortOrder: 5500),

        // ── Block F — Benchmarking Weights, Thresholds & Formulas ──────

        ConfigurationDefinition.Create(
            key: "benchmarking.score.weights",
            displayName: "config.benchmarking.score.weights.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.score.weights.description",
            defaultValue: """{"reliability":25,"performance":20,"costEfficiency":20,"security":15,"operationalExcellence":10,"documentation":10}""",
            uiEditorType: "json-editor",
            sortOrder: 5500),

        ConfigurationDefinition.Create(
            key: "benchmarking.score.thresholds",
            displayName: "config.benchmarking.score.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.benchmarking.score.thresholds.description",
            defaultValue: """{"Excellent":90,"Good":70,"NeedsImprovement":50,"Critical":0}""",
            uiEditorType: "json-editor",
            sortOrder: 5510),

        ConfigurationDefinition.Create(
            key: "benchmarking.score.bands",
            displayName: "config.benchmarking.score.bands.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.score.bands.description",
            defaultValue: """{"Excellent":{"label":"Excellent","color":"#10B981","minScore":90},"Good":{"label":"Good","color":"#3B82F6","minScore":70},"NeedsImprovement":{"label":"Needs Improvement","color":"#F59E0B","minScore":50},"Critical":{"label":"Critical","color":"#DC2626","minScore":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 5520),

        ConfigurationDefinition.Create(
            key: "benchmarking.formula.components",
            displayName: "config.benchmarking.formula.components.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.formula.components.description",
            defaultValue: """{"reliability":{"uptimeWeight":0.5,"mttrWeight":0.3,"incidentRateWeight":0.2},"performance":{"p99LatencyWeight":0.4,"throughputWeight":0.3,"errorRateWeight":0.3},"costEfficiency":{"budgetAdherenceWeight":0.5,"wasteReductionWeight":0.3,"optimizationAdoptionWeight":0.2}}""",
            uiEditorType: "json-editor",
            sortOrder: 5530),

        ConfigurationDefinition.Create(
            key: "benchmarking.score.by_dimension",
            displayName: "config.benchmarking.score.by_dimension.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.score.by_dimension.description",
            defaultValue: """{"reliability":{"uptime":50,"mttr":30,"incidentRate":20},"performance":{"latency":40,"throughput":30,"errorRate":30},"costEfficiency":{"budgetAdherence":50,"waste":30,"optimization":20},"security":{"vulnerabilities":40,"compliance":30,"patchCurrency":30},"operationalExcellence":{"automation":40,"documentation":30,"changeSuccess":30},"documentation":{"coverage":50,"freshness":30,"quality":20}}""",
            uiEditorType: "json-editor",
            sortOrder: 5540),

        ConfigurationDefinition.Create(
            key: "benchmarking.thresholds.by_environment",
            displayName: "config.benchmarking.thresholds.by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.thresholds.by_environment.description",
            defaultValue: """{"Production":{"Excellent":95,"Good":80,"NeedsImprovement":60,"Critical":0},"Development":{"Excellent":80,"Good":60,"NeedsImprovement":40,"Critical":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 5550),

        ConfigurationDefinition.Create(
            key: "benchmarking.missing_data.policy",
            displayName: "config.benchmarking.missing_data.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.missing_data.policy.description",
            defaultValue: "UseDefault",
            validationRules: """{"enum":["SkipDimension","UseDefault","Penalize"]}""",
            uiEditorType: "select",
            sortOrder: 5560),

        ConfigurationDefinition.Create(
            key: "benchmarking.missing_data.default_score",
            displayName: "config.benchmarking.missing_data.default_score.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmarking.missing_data.default_score.description",
            defaultValue: "50",
            validationRules: """{"min":0,"max":100}""",
            uiEditorType: "text",
            sortOrder: 5570),

        // ── Block G — Functional Health/Anomaly/Drift Thresholds ───────

        ConfigurationDefinition.Create(
            key: "operations.health.anomaly_thresholds",
            displayName: "config.operations.health.anomaly_thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.operations.health.anomaly_thresholds.description",
            defaultValue: """{"errorRateWarning":1.0,"errorRateCritical":5.0,"latencyP99Warning":500,"latencyP99Critical":2000,"availabilityWarning":99.5,"availabilityCritical":99.0}""",
            uiEditorType: "json-editor",
            sortOrder: 5600),

        ConfigurationDefinition.Create(
            key: "operations.health.drift_detection_enabled",
            displayName: "config.operations.health.drift_detection_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.health.drift_detection_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5610),

        ConfigurationDefinition.Create(
            key: "operations.health.drift_thresholds",
            displayName: "config.operations.health.drift_thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.health.drift_thresholds.description",
            defaultValue: """{"minor":{"maxDriftedConfigs":5},"major":{"maxDriftedConfigs":15},"critical":{"maxDriftedConfigs":30}}""",
            uiEditorType: "json-editor",
            sortOrder: 5620),

        // ══════════════════════════════════════════════════════════════════
        // ██  PHASE 7 — AI & INTEGRATIONS PARAMETERIZATION               ██
        // ══════════════════════════════════════════════════════════════════

        // ── Block A — AI Provider & Model Enablement ───────────────────

        ConfigurationDefinition.Create(
            key: "ai.providers.enabled",
            displayName: "config.ai.providers.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.providers.enabled.description",
            defaultValue: """["OpenAI","AzureOpenAI","Internal"]""",
            uiEditorType: "json-editor",
            sortOrder: 6000),

        ConfigurationDefinition.Create(
            key: "ai.models.enabled",
            displayName: "config.ai.models.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.models.enabled.description",
            defaultValue: """["gpt-4o","gpt-4o-mini","gpt-3.5-turbo","internal-llm"]""",
            uiEditorType: "json-editor",
            sortOrder: 6010),

        ConfigurationDefinition.Create(
            key: "ai.providers.default_by_capability",
            displayName: "config.ai.providers.default_by_capability.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.providers.default_by_capability.description",
            defaultValue: """{"chat":"OpenAI","analysis":"AzureOpenAI","classification":"Internal","draftGeneration":"OpenAI","retrievalAugmented":"AzureOpenAI","codeReview":"OpenAI"}""",
            uiEditorType: "json-editor",
            sortOrder: 6020),

        ConfigurationDefinition.Create(
            key: "ai.models.default_by_capability",
            displayName: "config.ai.models.default_by_capability.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.models.default_by_capability.description",
            defaultValue: """{"chat":"gpt-4o","analysis":"gpt-4o","classification":"internal-llm","draftGeneration":"gpt-4o","retrievalAugmented":"gpt-4o","codeReview":"gpt-4o-mini"}""",
            uiEditorType: "json-editor",
            sortOrder: 6030),

        ConfigurationDefinition.Create(
            key: "ai.providers.fallback_order",
            displayName: "config.ai.providers.fallback_order.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.providers.fallback_order.description",
            defaultValue: """["AzureOpenAI","OpenAI","Internal"]""",
            uiEditorType: "json-editor",
            sortOrder: 6040),

        ConfigurationDefinition.Create(
            key: "ai.usage.allow_external",
            displayName: "config.ai.usage.allow_external.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.usage.allow_external.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6050),

        ConfigurationDefinition.Create(
            key: "ai.usage.blocked_environments",
            displayName: "config.ai.usage.blocked_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.ai.usage.blocked_environments.description",
            defaultValue: """[]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 6060),

        ConfigurationDefinition.Create(
            key: "ai.usage.internal_only_capabilities",
            displayName: "config.ai.usage.internal_only_capabilities.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.usage.internal_only_capabilities.description",
            defaultValue: """["classification"]""",
            uiEditorType: "json-editor",
            sortOrder: 6070),

        // ── Block B — AI Budgets, Quotas & Usage Policies ──────────────

        ConfigurationDefinition.Create(
            key: "ai.budget.by_user",
            displayName: "config.ai.budget.by_user.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.budget.by_user.description",
            defaultValue: """{"monthlyTokens":100000,"alertOnExceed":true}""",
            uiEditorType: "json-editor",
            sortOrder: 6100),

        ConfigurationDefinition.Create(
            key: "ai.budget.by_team",
            displayName: "config.ai.budget.by_team.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.budget.by_team.description",
            defaultValue: """{"monthlyTokens":500000,"alertOnExceed":true}""",
            uiEditorType: "json-editor",
            sortOrder: 6110),

        ConfigurationDefinition.Create(
            key: "ai.budget.by_tenant",
            displayName: "config.ai.budget.by_tenant.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.budget.by_tenant.description",
            defaultValue: """{"monthlyTokens":2000000,"alertOnExceed":true,"hardLimit":false}""",
            uiEditorType: "json-editor",
            sortOrder: 6120),

        ConfigurationDefinition.Create(
            key: "ai.quota.by_capability",
            displayName: "config.ai.quota.by_capability.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.quota.by_capability.description",
            defaultValue: """{"chat":{"dailyTokens":50000},"analysis":{"dailyTokens":100000},"draftGeneration":{"dailyTokens":30000},"retrievalAugmented":{"dailyTokens":80000}}""",
            uiEditorType: "json-editor",
            sortOrder: 6130),

        ConfigurationDefinition.Create(
            key: "ai.usage.limits_by_environment",
            displayName: "config.ai.usage.limits_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.usage.limits_by_environment.description",
            defaultValue: """{"Production":{"dailyTokens":500000},"PreProduction":{"dailyTokens":100000},"Development":{"dailyTokens":50000}}""",
            uiEditorType: "json-editor",
            sortOrder: 6140),

        ConfigurationDefinition.Create(
            key: "ai.budget.exceed_policy",
            displayName: "config.ai.budget.exceed_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.budget.exceed_policy.description",
            defaultValue: "Warn",
            validationRules: """{"enum":["Warn","Block","Throttle"]}""",
            uiEditorType: "select",
            sortOrder: 6150),

        ConfigurationDefinition.Create(
            key: "ai.budget.warning_thresholds",
            displayName: "config.ai.budget.warning_thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.budget.warning_thresholds.description",
            defaultValue: """[{"percent":70,"severity":"Low"},{"percent":85,"severity":"Medium"},{"percent":95,"severity":"High"},{"percent":100,"severity":"Critical"}]""",
            uiEditorType: "json-editor",
            sortOrder: 6160),

        // ── Block C — Retention, Audit, Prompts & Retrieval ────────────

        ConfigurationDefinition.Create(
            key: "ai.retention.conversation_days",
            displayName: "config.ai.retention.conversation_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retention.conversation_days.description",
            defaultValue: "90",
            validationRules: """{"min":1,"max":365}""",
            uiEditorType: "text",
            sortOrder: 6200),

        ConfigurationDefinition.Create(
            key: "ai.retention.artifact_days",
            displayName: "config.ai.retention.artifact_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retention.artifact_days.description",
            defaultValue: "180",
            validationRules: """{"min":1,"max":730}""",
            uiEditorType: "text",
            sortOrder: 6210),

        ConfigurationDefinition.Create(
            key: "ai.audit.level",
            displayName: "config.ai.audit.level.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.audit.level.description",
            defaultValue: "Standard",
            validationRules: """{"enum":["Minimal","Standard","Full"]}""",
            uiEditorType: "select",
            sortOrder: 6220),

        ConfigurationDefinition.Create(
            key: "ai.audit.log_prompts",
            displayName: "config.ai.audit.log_prompts.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.audit.log_prompts.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 6230),

        ConfigurationDefinition.Create(
            key: "ai.audit.log_responses",
            displayName: "config.ai.audit.log_responses.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.audit.log_responses.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 6240),

        ConfigurationDefinition.Create(
            key: "ai.prompts.base_by_capability",
            displayName: "config.ai.prompts.base_by_capability.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.prompts.base_by_capability.description",
            defaultValue: """{"chat":"You are NexTraceOne AI Assistant, a helpful operational intelligence assistant.","analysis":"You are an expert operational analyst. Analyze the data provided and give actionable insights.","classification":"Classify the following operational event into the appropriate category and severity.","draftGeneration":"Generate a professional draft based on the provided context and requirements."}""",
            uiEditorType: "json-editor",
            sortOrder: 6250),

        ConfigurationDefinition.Create(
            key: "ai.prompts.allow_tenant_override",
            displayName: "config.ai.prompts.allow_tenant_override.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "config.ai.prompts.allow_tenant_override.description",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 6260),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.top_k",
            displayName: "config.ai.retrieval.top_k.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retrieval.top_k.description",
            defaultValue: "5",
            validationRules: """{"min":1,"max":50}""",
            uiEditorType: "text",
            sortOrder: 6270),

        ConfigurationDefinition.Create(
            key: "ai.defaults.temperature",
            displayName: "config.ai.defaults.temperature.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.defaults.temperature.description",
            defaultValue: "0.7",
            validationRules: """{"min":0.0,"max":2.0}""",
            uiEditorType: "text",
            sortOrder: 6280),

        ConfigurationDefinition.Create(
            key: "ai.defaults.max_tokens",
            displayName: "config.ai.defaults.max_tokens.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.defaults.max_tokens.description",
            defaultValue: "4096",
            validationRules: """{"min":100,"max":128000}""",
            uiEditorType: "text",
            sortOrder: 6290),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.similarity_threshold",
            displayName: "config.ai.retrieval.similarity_threshold.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retrieval.similarity_threshold.description",
            defaultValue: "0.7",
            validationRules: """{"min":0.0,"max":1.0}""",
            uiEditorType: "text",
            sortOrder: 6300),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.source_allowlist",
            displayName: "config.ai.retrieval.source_allowlist.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retrieval.source_allowlist.description",
            defaultValue: """[]""",
            uiEditorType: "json-editor",
            sortOrder: 6310),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.source_denylist",
            displayName: "config.ai.retrieval.source_denylist.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retrieval.source_denylist.description",
            defaultValue: """[]""",
            uiEditorType: "json-editor",
            sortOrder: 6320),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.context_by_environment",
            displayName: "config.ai.retrieval.context_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.retrieval.context_by_environment.description",
            defaultValue: """{"Production":{"telemetry":true,"documents":true,"incidents":true},"Development":{"telemetry":true,"documents":true,"incidents":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 6330),

        // ── Block D — Connector Enablement, Schedules, Retries & Timeouts ──

        ConfigurationDefinition.Create(
            key: "integrations.connectors.enabled",
            displayName: "config.integrations.connectors.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.connectors.enabled.description",
            defaultValue: """["AzureDevOps","GitHub","Jira","ServiceNow","PagerDuty","Datadog","Prometheus"]""",
            uiEditorType: "json-editor",
            sortOrder: 6400),

        ConfigurationDefinition.Create(
            key: "integrations.connectors.enabled_by_environment",
            displayName: "config.integrations.connectors.enabled_by_environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.connectors.enabled_by_environment.description",
            defaultValue: """{"Production":["AzureDevOps","GitHub","ServiceNow","PagerDuty","Datadog","Prometheus"],"Development":["AzureDevOps","GitHub","Jira"]}""",
            uiEditorType: "json-editor",
            sortOrder: 6410),

        ConfigurationDefinition.Create(
            key: "integrations.schedule.default",
            displayName: "config.integrations.schedule.default.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.schedule.default.description",
            defaultValue: "0 */6 * * *",
            uiEditorType: "text",
            sortOrder: 6420),

        ConfigurationDefinition.Create(
            key: "integrations.schedule.by_connector",
            displayName: "config.integrations.schedule.by_connector.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.schedule.by_connector.description",
            defaultValue: """{"AzureDevOps":"0 */4 * * *","GitHub":"0 */4 * * *","Jira":"0 */6 * * *","ServiceNow":"0 */2 * * *","PagerDuty":"*/30 * * * *","Datadog":"*/15 * * * *","Prometheus":"*/5 * * * *"}""",
            uiEditorType: "json-editor",
            sortOrder: 6430),

        ConfigurationDefinition.Create(
            key: "integrations.retry.max_attempts",
            displayName: "config.integrations.retry.max_attempts.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.retry.max_attempts.description",
            defaultValue: "3",
            validationRules: """{"min":0,"max":10}""",
            uiEditorType: "text",
            sortOrder: 6440),

        ConfigurationDefinition.Create(
            key: "integrations.retry.backoff_seconds",
            displayName: "config.integrations.retry.backoff_seconds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.retry.backoff_seconds.description",
            defaultValue: "30",
            validationRules: """{"min":5,"max":600}""",
            uiEditorType: "text",
            sortOrder: 6450),

        ConfigurationDefinition.Create(
            key: "integrations.retry.exponential_backoff",
            displayName: "config.integrations.retry.exponential_backoff.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.retry.exponential_backoff.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6460),

        ConfigurationDefinition.Create(
            key: "integrations.timeout.default_seconds",
            displayName: "config.integrations.timeout.default_seconds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.timeout.default_seconds.description",
            defaultValue: "120",
            validationRules: """{"min":10,"max":3600}""",
            uiEditorType: "text",
            sortOrder: 6470),

        ConfigurationDefinition.Create(
            key: "integrations.timeout.by_connector",
            displayName: "config.integrations.timeout.by_connector.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.timeout.by_connector.description",
            defaultValue: """{"AzureDevOps":180,"GitHub":120,"Jira":120,"ServiceNow":180,"PagerDuty":60,"Datadog":90,"Prometheus":60}""",
            uiEditorType: "json-editor",
            sortOrder: 6480),

        ConfigurationDefinition.Create(
            key: "integrations.execution.max_concurrent",
            displayName: "config.integrations.execution.max_concurrent.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.execution.max_concurrent.description",
            defaultValue: "5",
            validationRules: """{"min":1,"max":20}""",
            uiEditorType: "text",
            sortOrder: 6490),

        // ── Block E — Filters, Mappings, Import/Export & Sync Policy ───

        ConfigurationDefinition.Create(
            key: "integrations.sync.filter_policy",
            displayName: "config.integrations.sync.filter_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.sync.filter_policy.description",
            defaultValue: """{"excludeArchived":true,"excludeDeleted":true,"maxAgeHours":720}""",
            uiEditorType: "json-editor",
            sortOrder: 6500),

        ConfigurationDefinition.Create(
            key: "integrations.sync.mapping_policy",
            displayName: "config.integrations.sync.mapping_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.sync.mapping_policy.description",
            defaultValue: """{"autoMapByName":true,"strictTypeValidation":true,"unmappedFieldAction":"Ignore"}""",
            uiEditorType: "json-editor",
            sortOrder: 6510),

        ConfigurationDefinition.Create(
            key: "integrations.import.policy",
            displayName: "config.integrations.import.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.import.policy.description",
            defaultValue: """{"allowOverwrite":false,"requireValidation":true,"onConflict":"Skip","maxBatchSize":1000}""",
            uiEditorType: "json-editor",
            sortOrder: 6520),

        ConfigurationDefinition.Create(
            key: "integrations.export.policy",
            displayName: "config.integrations.export.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.export.policy.description",
            defaultValue: """{"includeMetadata":true,"defaultFormat":"JSON","maxRecords":10000,"sanitizeSensitive":true}""",
            uiEditorType: "json-editor",
            sortOrder: 6530),

        ConfigurationDefinition.Create(
            key: "integrations.sync.overwrite_behavior",
            displayName: "config.integrations.sync.overwrite_behavior.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.sync.overwrite_behavior.description",
            defaultValue: "Merge",
            validationRules: """{"enum":["Overwrite","Merge","Skip"]}""",
            uiEditorType: "select",
            sortOrder: 6540),

        ConfigurationDefinition.Create(
            key: "integrations.sync.pre_validation_enabled",
            displayName: "config.integrations.sync.pre_validation_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.sync.pre_validation_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6550),

        ConfigurationDefinition.Create(
            key: "integrations.freshness.staleness_threshold_hours",
            displayName: "config.integrations.freshness.staleness_threshold_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.freshness.staleness_threshold_hours.description",
            defaultValue: "24",
            validationRules: """{"min":1,"max":168}""",
            uiEditorType: "text",
            sortOrder: 6560),

        ConfigurationDefinition.Create(
            key: "integrations.freshness.by_connector",
            displayName: "config.integrations.freshness.by_connector.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.freshness.by_connector.description",
            defaultValue: """{"AzureDevOps":12,"GitHub":12,"Jira":24,"ServiceNow":6,"PagerDuty":1,"Datadog":1,"Prometheus":1}""",
            uiEditorType: "json-editor",
            sortOrder: 6570),

        // ── Block F — Failure Reaction, Notification & Governance ───────

        ConfigurationDefinition.Create(
            key: "integrations.failure.notification_policy",
            displayName: "config.integrations.failure.notification_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.failure.notification_policy.description",
            defaultValue: """{"notifyOnFirstFailure":true,"notifyOnConsecutiveFailures":3,"notifyOnAuthFailure":true,"notifyOnStaleness":true,"digestFrequency":"Hourly"}""",
            uiEditorType: "json-editor",
            sortOrder: 6600),

        ConfigurationDefinition.Create(
            key: "integrations.failure.severity_mapping",
            displayName: "config.integrations.failure.severity_mapping.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.failure.severity_mapping.description",
            defaultValue: """{"authFailure":"Critical","syncFailure":"High","timeoutFailure":"Medium","validationFailure":"Low","staleData":"Medium"}""",
            uiEditorType: "json-editor",
            sortOrder: 6610),

        ConfigurationDefinition.Create(
            key: "integrations.failure.escalation_policy",
            displayName: "config.integrations.failure.escalation_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.failure.escalation_policy.description",
            defaultValue: """{"Critical":{"escalateAfterMinutes":15,"recipient":"platform-admin"},"High":{"escalateAfterMinutes":60,"recipient":"integration-owner"},"Medium":{"escalateAfterMinutes":240},"Low":{"escalateAfterMinutes":1440}}""",
            uiEditorType: "json-editor",
            sortOrder: 6620),

        ConfigurationDefinition.Create(
            key: "integrations.failure.auto_disable_enabled",
            displayName: "config.integrations.failure.auto_disable_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.failure.auto_disable_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6630),

        ConfigurationDefinition.Create(
            key: "integrations.failure.auto_disable_threshold",
            displayName: "config.integrations.failure.auto_disable_threshold.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.failure.auto_disable_threshold.description",
            defaultValue: "5",
            validationRules: """{"min":2,"max":50}""",
            uiEditorType: "text",
            sortOrder: 6640),

        ConfigurationDefinition.Create(
            key: "integrations.failure.auth_reaction_policy",
            displayName: "config.integrations.failure.auth_reaction_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.failure.auth_reaction_policy.description",
            defaultValue: """{"pauseSync":true,"notifyOwner":true,"autoRetryAfterMinutes":60,"maxAuthRetries":3}""",
            uiEditorType: "json-editor",
            sortOrder: 6650),

        ConfigurationDefinition.Create(
            key: "integrations.owner.fallback_recipient",
            displayName: "config.integrations.owner.fallback_recipient.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.owner.fallback_recipient.description",
            defaultValue: "platform-admin",
            uiEditorType: "text",
            sortOrder: 6660),

        ConfigurationDefinition.Create(
            key: "integrations.governance.blocked_in_production",
            displayName: "config.integrations.governance.blocked_in_production.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "config.integrations.governance.blocked_in_production.description",
            defaultValue: """["bulkDelete","schemaOverwrite","forceReSync"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 6670),

        // ── PARAMETERIZATION-MODULE v2.0 — 113 Governance Parameters ──────────────

        // ── Service Catalog ────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "catalog.service.creation.approval_required",
            displayName: "config.catalog.service.creation.approval_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.service.creation.approval_required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7000),

        ConfigurationDefinition.Create(
            key: "catalog.service.creation.approval_roles",
            displayName: "config.catalog.service.creation.approval_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.creation.approval_roles.description",
            defaultValue: """["TechLead","Architect","PlatformAdmin"]""",
            uiEditorType: "json-editor",
            sortOrder: 7010),

        ConfigurationDefinition.Create(
            key: "catalog.service.creation.approval_min_approvers",
            displayName: "config.catalog.service.creation.approval_min_approvers.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.creation.approval_min_approvers.description",
            defaultValue: "1",
            uiEditorType: "text",
            sortOrder: 7020),

        ConfigurationDefinition.Create(
            key: "catalog.service.creation.auto_approve_conditions",
            displayName: "config.catalog.service.creation.auto_approve_conditions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.creation.auto_approve_conditions.description",
            defaultValue: """{"enabled":false}""",
            uiEditorType: "json-editor",
            sortOrder: 7030),

        ConfigurationDefinition.Create(
            key: "catalog.service.creation.mandatory_fields",
            displayName: "config.catalog.service.creation.mandatory_fields.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.creation.mandatory_fields.description",
            defaultValue: """["name","domain","team","owner","description"]""",
            uiEditorType: "json-editor",
            sortOrder: 7040),

        ConfigurationDefinition.Create(
            key: "catalog.service.creation.require_template",
            displayName: "config.catalog.service.creation.require_template.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.creation.require_template.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7050),

        ConfigurationDefinition.Create(
            key: "catalog.service.creation.require_contract",
            displayName: "config.catalog.service.creation.require_contract.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.service.creation.require_contract.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7060),

        ConfigurationDefinition.Create(
            key: "catalog.service.deactivation.approval_required",
            displayName: "config.catalog.service.deactivation.approval_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.deactivation.approval_required.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7070),

        ConfigurationDefinition.Create(
            key: "catalog.service.deactivation.require_dependency_check",
            displayName: "config.catalog.service.deactivation.require_dependency_check.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.deactivation.require_dependency_check.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7080),

        // ── Service Interface ──────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "catalog.service_interface.require_contract_for_types",
            displayName: "config.catalog.service_interface.require_contract_for_types.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.service_interface.require_contract_for_types.description",
            defaultValue: """["RestApi","Soap","GraphQL","Webhook"]""",
            uiEditorType: "json-editor",
            sortOrder: 7085),

        ConfigurationDefinition.Create(
            key: "catalog.service_interface.allowed_types",
            displayName: "config.catalog.service_interface.allowed_types.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service_interface.allowed_types.description",
            defaultValue: """["RestApi","Soap","GraphQL","Kafka","AsyncApi","Grpc","Webhook","BackgroundJob","ScheduledJob","WebSocket","ServerSentEvents","Database","InternalEvent"]""",
            uiEditorType: "json-editor",
            sortOrder: 7086),

        ConfigurationDefinition.Create(
            key: "catalog.service_interface.deprecation_notice_days",
            displayName: "config.catalog.service_interface.deprecation_notice_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service_interface.deprecation_notice_days.description",
            defaultValue: "30",
            uiEditorType: "text",
            sortOrder: 7087),

        ConfigurationDefinition.Create(
            key: "catalog.service_interface.slo_enforcement_enabled",
            displayName: "config.catalog.service_interface.slo_enforcement_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.service_interface.slo_enforcement_enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7088),

        // ── Service Taxonomy: Data Classification & Regulatory Scope ─────────────
        ConfigurationDefinition.Create(
            key: "catalog.service.data_classification.values",
            displayName: "config.catalog.service.data_classification.values.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.data_classification.values.description",
            defaultValue: """["Public","Internal","Confidential","Restricted","TopSecret"]""",
            uiEditorType: "json-editor",
            sortOrder: 7090),

        ConfigurationDefinition.Create(
            key: "catalog.service.regulatory_scope.values",
            displayName: "config.catalog.service.regulatory_scope.values.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.service.regulatory_scope.values.description",
            defaultValue: """["None","LGPD","GDPR","PCI-DSS","SOX","HIPAA","ISO27001"]""",
            uiEditorType: "json-editor",
            sortOrder: 7091),

        // ── Contracts ──────────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "catalog.contract.creation.approval_required",
            displayName: "config.catalog.contract.creation.approval_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.contract.creation.approval_required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7100),

        ConfigurationDefinition.Create(
            key: "catalog.contract.creation.approval_roles",
            displayName: "config.catalog.contract.creation.approval_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.creation.approval_roles.description",
            defaultValue: """["Architect","TechLead"]""",
            uiEditorType: "json-editor",
            sortOrder: 7110),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_change.block_deploy",
            displayName: "config.catalog.contract.breaking_change.block_deploy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.contract.breaking_change.block_deploy.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7120),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_change.override_roles",
            displayName: "config.catalog.contract.breaking_change.override_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.breaking_change.override_roles.description",
            defaultValue: """["Architect","PlatformAdmin"]""",
            uiEditorType: "json-editor",
            sortOrder: 7130),

        ConfigurationDefinition.Create(
            key: "catalog.contract.validation.auto_lint_on_save",
            displayName: "config.catalog.contract.validation.auto_lint_on_save.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.validation.auto_lint_on_save.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7140),

        ConfigurationDefinition.Create(
            key: "catalog.contract.validation.block_on_lint_errors",
            displayName: "config.catalog.contract.validation.block_on_lint_errors.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.contract.validation.block_on_lint_errors.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7150),

        ConfigurationDefinition.Create(
            key: "catalog.contract.publication.require_examples",
            displayName: "config.catalog.contract.publication.require_examples.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.publication.require_examples.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7160),

        ConfigurationDefinition.Create(
            key: "catalog.contract.deprecation.grace_period_days",
            displayName: "config.catalog.contract.deprecation.grace_period_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.contract.deprecation.grace_period_days.description",
            defaultValue: "90",
            uiEditorType: "text",
            sortOrder: 7170),

        // ── Change Governance — Release ────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "change.release.approval_required",
            displayName: "config.change.release.approval_required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.approval_required.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7200),

        ConfigurationDefinition.Create(
            key: "change.release.approval_roles",
            displayName: "config.change.release.approval_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.approval_roles.description",
            defaultValue: """["TechLead","Architect"]""",
            uiEditorType: "json-editor",
            sortOrder: 7210),

        ConfigurationDefinition.Create(
            key: "change.release.approval_min_approvers",
            displayName: "config.change.release.approval_min_approvers.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.approval_min_approvers.description",
            defaultValue: "1",
            uiEditorType: "text",
            sortOrder: 7220),

        ConfigurationDefinition.Create(
            key: "change.release.external_validation.enabled",
            displayName: "config.change.release.external_validation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.external_validation.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7230),

        ConfigurationDefinition.Create(
            key: "change.release.external_validation.provider",
            displayName: "config.change.release.external_validation.provider.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.external_validation.provider.description",
            defaultValue: "",
            uiEditorType: "text",
            sortOrder: 7240),

        ConfigurationDefinition.Create(
            key: "change.release.external_validation.endpoint_url",
            displayName: "config.change.release.external_validation.endpoint_url.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.external_validation.endpoint_url.description",
            defaultValue: "",
            uiEditorType: "text",
            sortOrder: 7250),

        ConfigurationDefinition.Create(
            key: "change.release.external_validation.timeout_seconds",
            displayName: "config.change.release.external_validation.timeout_seconds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release.external_validation.timeout_seconds.description",
            defaultValue: "30",
            uiEditorType: "text",
            sortOrder: 7260),

        ConfigurationDefinition.Create(
            key: "change.release.external_validation.on_failure_action",
            displayName: "config.change.release.external_validation.on_failure_action.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.external_validation.on_failure_action.description",
            defaultValue: "block",
            uiEditorType: "select",
            sortOrder: 7270),

        ConfigurationDefinition.Create(
            key: "change.release.external_validation.required_checks",
            displayName: "config.change.release.external_validation.required_checks.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.external_validation.required_checks.description",
            defaultValue: """["build_success","tests_passed"]""",
            uiEditorType: "json-editor",
            sortOrder: 7280),

        // ── Change Governance — Release Intelligence Parameters ─────────────────
        ConfigurationDefinition.Create(
            key: "change.release.min_confidence_score_for_promotion",
            displayName: "config.change.release.min_confidence_score_for_promotion.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.min_confidence_score_for_promotion.description",
            defaultValue: "0.6",
            uiEditorType: "text",
            sortOrder: 7285),

        ConfigurationDefinition.Create(
            key: "change.release.observation_window_minutes",
            displayName: "config.change.release.observation_window_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.observation_window_minutes.description",
            defaultValue: "60",
            uiEditorType: "text",
            sortOrder: 7286),

        ConfigurationDefinition.Create(
            key: "change.release.rollback.max_migrated_consumers_percent",
            displayName: "config.change.release.rollback.max_migrated_consumers_percent.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.rollback.max_migrated_consumers_percent.description",
            defaultValue: "30",
            uiEditorType: "text",
            sortOrder: 7287),

        ConfigurationDefinition.Create(
            key: "change.release.evidence_pack.expiry_days",
            displayName: "config.change.release.evidence_pack.expiry_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release.evidence_pack.expiry_days.description",
            defaultValue: "90",
            uiEditorType: "text",
            sortOrder: 7288),

        ConfigurationDefinition.Create(
            key: "change.release.auto_generate_notes_on_review",
            displayName: "config.change.release.auto_generate_notes_on_review.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.release.auto_generate_notes_on_review.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7289),

        ConfigurationDefinition.Create(
            key: "change.release.blast_radius.cab_approval_threshold",
            displayName: "config.change.release.blast_radius.cab_approval_threshold.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.release.blast_radius.cab_approval_threshold.description",
            defaultValue: "0.7",
            uiEditorType: "text",
            sortOrder: 7290),

        // ── Change Governance — Deploy ─────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "change.deploy.require_release_approval",
            displayName: "config.change.deploy.require_release_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.require_release_approval.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7300),

        ConfigurationDefinition.Create(
            key: "change.deploy.pre_deploy_checks",
            displayName: "config.change.deploy.pre_deploy_checks.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.pre_deploy_checks.description",
            defaultValue: """{"contract_compliance":true,"security_scan":true,"evidence_pack":false}""",
            uiEditorType: "json-editor",
            sortOrder: 7310),

        ConfigurationDefinition.Create(
            key: "change.deploy.post_deploy_verification.enabled",
            displayName: "config.change.deploy.post_deploy_verification.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.post_deploy_verification.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7320),

        ConfigurationDefinition.Create(
            key: "change.deploy.post_deploy_verification.window_minutes",
            displayName: "config.change.deploy.post_deploy_verification.window_minutes.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.post_deploy_verification.window_minutes.description",
            defaultValue: "30",
            uiEditorType: "text",
            sortOrder: 7330),

        ConfigurationDefinition.Create(
            key: "change.deploy.canary.enabled",
            displayName: "config.change.deploy.canary.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.canary.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7340),

        ConfigurationDefinition.Create(
            key: "change.deploy.rollback.auto_enabled",
            displayName: "config.change.deploy.rollback.auto_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.rollback.auto_enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7350),

        ConfigurationDefinition.Create(
            key: "change.deploy.rollback.auto_thresholds",
            displayName: "config.change.deploy.rollback.auto_thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.deploy.rollback.auto_thresholds.description",
            defaultValue: """{"error_rate_increase_pct":50,"latency_increase_pct":100,"availability_drop_pct":5}""",
            uiEditorType: "json-editor",
            sortOrder: 7360),

        ConfigurationDefinition.Create(
            key: "change.gitops.require_pr_approval",
            displayName: "config.change.gitops.require_pr_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.gitops.require_pr_approval.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7370),

        // ── Change Governance — GitOps ─────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "change.gitops.require_signed_commits",
            displayName: "config.change.gitops.require_signed_commits.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.change.gitops.require_signed_commits.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7380),

        // ── Promotion Governance ───────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "promotion.require_all_gates_passed",
            displayName: "config.promotion.require_all_gates_passed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.require_all_gates_passed.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7400),

        ConfigurationDefinition.Create(
            key: "promotion.require_non_prod_validation",
            displayName: "config.promotion.require_non_prod_validation.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.promotion.require_non_prod_validation.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7410),

        ConfigurationDefinition.Create(
            key: "promotion.min_time_in_staging_hours",
            displayName: "config.promotion.min_time_in_staging_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.min_time_in_staging_hours.description",
            defaultValue: "24",
            uiEditorType: "text",
            sortOrder: 7420),

        ConfigurationDefinition.Create(
            key: "promotion.require_evidence_pack",
            displayName: "config.promotion.require_evidence_pack.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.promotion.require_evidence_pack.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7430),

        // ── Workflow Engine ────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "workflow.dynamic_stages.enabled",
            displayName: "config.workflow.dynamic_stages.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.dynamic_stages.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7500),

        ConfigurationDefinition.Create(
            key: "workflow.parallel_approval.enabled",
            displayName: "config.workflow.parallel_approval.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.parallel_approval.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7510),

        ConfigurationDefinition.Create(
            key: "workflow.delegation.enabled",
            displayName: "config.workflow.delegation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.delegation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7520),

        ConfigurationDefinition.Create(
            key: "workflow.reminder.enabled",
            displayName: "config.workflow.reminder.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.reminder.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7530),

        ConfigurationDefinition.Create(
            key: "workflow.reminder.interval_hours",
            displayName: "config.workflow.reminder.interval_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.workflow.reminder.interval_hours.description",
            defaultValue: "4",
            uiEditorType: "text",
            sortOrder: 7540),

        // ── Incidents & Operations ─────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.from_monitoring.enabled",
            displayName: "config.incidents.auto_creation.from_monitoring.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.incidents.auto_creation.from_monitoring.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7600),

        ConfigurationDefinition.Create(
            key: "incidents.auto_assign.enabled",
            displayName: "config.incidents.auto_assign.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.auto_assign.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7610),

        ConfigurationDefinition.Create(
            key: "incidents.post_incident_review.required",
            displayName: "config.incidents.post_incident_review.required.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.incidents.post_incident_review.required.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7620),

        ConfigurationDefinition.Create(
            key: "incidents.post_incident_review.min_severity",
            displayName: "config.incidents.post_incident_review.min_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.post_incident_review.min_severity.description",
            defaultValue: "High",
            uiEditorType: "select",
            sortOrder: 7630),

        ConfigurationDefinition.Create(
            key: "incidents.correlation.auto_link_to_changes.enabled",
            displayName: "config.incidents.correlation.auto_link_to_changes.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.incidents.correlation.auto_link_to_changes.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7640),

        ConfigurationDefinition.Create(
            key: "operations.runbook.auto_suggest.enabled",
            displayName: "config.operations.runbook.auto_suggest.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.runbook.auto_suggest.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7650),

        ConfigurationDefinition.Create(
            key: "operations.automation.require_approval_in_production",
            displayName: "config.operations.automation.require_approval_in_production.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.automation.require_approval_in_production.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7660),

        // ── AI Hub ─────────────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "ai.external_models.require_approval",
            displayName: "config.ai.external_models.require_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.external_models.require_approval.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7700),

        ConfigurationDefinition.Create(
            key: "ai.data_classification.block_sensitive",
            displayName: "config.ai.data_classification.block_sensitive.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.data_classification.block_sensitive.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7710),

        ConfigurationDefinition.Create(
            key: "ai.knowledge_capture.auto_approve",
            displayName: "config.ai.knowledge_capture.auto_approve.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.knowledge_capture.auto_approve.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7720),

        ConfigurationDefinition.Create(
            key: "ai.agents.custom_creation.enabled",
            displayName: "config.ai.agents.custom_creation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.agents.custom_creation.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7730),

        ConfigurationDefinition.Create(
            key: "ai.agents.custom_creation.require_approval",
            displayName: "config.ai.agents.custom_creation.require_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.agents.custom_creation.require_approval.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7740),

        ConfigurationDefinition.Create(
            key: "ai.ide.extensions.enabled",
            displayName: "config.ai.ide.extensions.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.ide.extensions.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7750),

        ConfigurationDefinition.Create(
            key: "ai.ide.allowed_capabilities",
            displayName: "config.ai.ide.allowed_capabilities.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role],
            description: "config.ai.ide.allowed_capabilities.description",
            defaultValue: """["code_review","contract_generation","test_generation","documentation"]""",
            uiEditorType: "json-editor",
            sortOrder: 7760),

        // ── Security & Identity ────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "security.password.policy",
            displayName: "config.security.password.policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.password.policy.description",
            defaultValue: """{"min_length":12,"require_uppercase":true,"require_lowercase":true,"require_number":true,"require_special":true,"max_age_days":90,"history_count":5}""",
            uiEditorType: "json-editor",
            sortOrder: 7800),

        ConfigurationDefinition.Create(
            key: "security.session.concurrent_limit",
            displayName: "config.security.session.concurrent_limit.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.session.concurrent_limit.description",
            defaultValue: "5",
            uiEditorType: "text",
            sortOrder: 7810),

        ConfigurationDefinition.Create(
            key: "security.break_glass.require_dual_approval",
            displayName: "config.security.break_glass.require_dual_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.break_glass.require_dual_approval.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7820),

        ConfigurationDefinition.Create(
            key: "security.jit_access.max_duration_hours",
            displayName: "config.security.jit_access.max_duration_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.jit_access.max_duration_hours.description",
            defaultValue: "8",
            uiEditorType: "text",
            sortOrder: 7830),

        ConfigurationDefinition.Create(
            key: "security.access_review.frequency_days",
            displayName: "config.security.access_review.frequency_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.access_review.frequency_days.description",
            defaultValue: "90",
            uiEditorType: "text",
            sortOrder: 7840),

        ConfigurationDefinition.Create(
            key: "security.access_review.auto_revoke_on_no_response",
            displayName: "config.security.access_review.auto_revoke_on_no_response.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.access_review.auto_revoke_on_no_response.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7850),

        ConfigurationDefinition.Create(
            key: "identity.roles.custom_creation.enabled",
            displayName: "config.identity.roles.custom_creation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.identity.roles.custom_creation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7860),

        ConfigurationDefinition.Create(
            key: "identity.roles.max_custom_roles",
            displayName: "config.identity.roles.max_custom_roles.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.identity.roles.max_custom_roles.description",
            defaultValue: "20",
            uiEditorType: "text",
            sortOrder: 7870),

        ConfigurationDefinition.Create(
            key: "identity.roles.require_description",
            displayName: "config.identity.roles.require_description.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.identity.roles.require_description.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 7880),

        // ── Security — Scanning ────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "security.scan.require_before_deploy",
            displayName: "config.security.scan.require_before_deploy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.security.scan.require_before_deploy.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 7900),

        ConfigurationDefinition.Create(
            key: "security.scan.blocked_severities",
            displayName: "config.security.scan.blocked_severities.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.security.scan.blocked_severities.description",
            defaultValue: """["critical"]""",
            uiEditorType: "json-editor",
            sortOrder: 7910),

        // ── Audit & Compliance ─────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "audit.chain_integrity.verification_frequency_hours",
            displayName: "config.audit.chain_integrity.verification_frequency_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System],
            description: "config.audit.chain_integrity.verification_frequency_hours.description",
            defaultValue: "24",
            uiEditorType: "text",
            sortOrder: 8000),

        ConfigurationDefinition.Create(
            key: "audit.continuous_compliance.enabled",
            displayName: "config.audit.continuous_compliance.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.audit.continuous_compliance.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8010),

        ConfigurationDefinition.Create(
            key: "audit.compliance.require_evidence_for_exceptions",
            displayName: "config.audit.compliance.require_evidence_for_exceptions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.audit.compliance.require_evidence_for_exceptions.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8020),

        // ── Integrations ───────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "integrations.new_connector.require_approval",
            displayName: "config.integrations.new_connector.require_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.new_connector.require_approval.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8100),

        ConfigurationDefinition.Create(
            key: "integrations.webhook.require_signature_validation",
            displayName: "config.integrations.webhook.require_signature_validation.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.webhook.require_signature_validation.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8110),

        ConfigurationDefinition.Create(
            key: "integrations.data_sync.direction_policy",
            displayName: "config.integrations.data_sync.direction_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.integrations.data_sync.direction_policy.description",
            defaultValue: "bidirectional",
            uiEditorType: "select",
            sortOrder: 8120),

        // ── FinOps ─────────────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "finops.budget.auto_alert.enabled",
            displayName: "config.finops.budget.auto_alert.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.auto_alert.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8200),

        ConfigurationDefinition.Create(
            key: "finops.budget.enforcement.enabled",
            displayName: "config.finops.budget.enforcement.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.budget.enforcement.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8210),

        ConfigurationDefinition.Create(
            key: "finops.recommendations.auto_apply.enabled",
            displayName: "config.finops.recommendations.auto_apply.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.recommendations.auto_apply.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8220),

        ConfigurationDefinition.Create(
            key: "finops.showback.enabled",
            displayName: "config.finops.showback.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.showback.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8230),

        ConfigurationDefinition.Create(
            key: "finops.chargeback.enabled",
            displayName: "config.finops.chargeback.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.chargeback.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8240),

        // ── Operational Knowledge ──────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "knowledge.operational_documents.enabled",
            displayName: "config.knowledge.operational_documents.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.operational_documents.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8300),

        ConfigurationDefinition.Create(
            key: "knowledge.auto_capture.enabled",
            displayName: "config.knowledge.auto_capture.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.auto_capture.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8310),

        ConfigurationDefinition.Create(
            key: "knowledge.auto_capture.categories",
            displayName: "config.knowledge.auto_capture.categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.auto_capture.categories.description",
            defaultValue: """["PostMortem","ComplianceEvidence","DecisionRecord","ChangeLog"]""",
            uiEditorType: "json-editor",
            sortOrder: 8320),

        ConfigurationDefinition.Create(
            key: "knowledge.search.federated.enabled",
            displayName: "config.knowledge.search.federated.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.search.federated.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8330),

        ConfigurationDefinition.Create(
            key: "knowledge.relations.auto_link.enabled",
            displayName: "config.knowledge.relations.auto_link.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.relations.auto_link.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8340),

        ConfigurationDefinition.Create(
            key: "knowledge.graph.max_depth",
            displayName: "config.knowledge.graph.max_depth.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.graph.max_depth.description",
            defaultValue: "3",
            uiEditorType: "text",
            sortOrder: 8350),

        // ── Reliability ────────────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "reliability.slo.require_definition",
            displayName: "config.reliability.slo.require_definition.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.reliability.slo.require_definition.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8400),

        ConfigurationDefinition.Create(
            key: "reliability.error_budget.auto_block_deploys",
            displayName: "config.reliability.error_budget.auto_block_deploys.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.reliability.error_budget.auto_block_deploys.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8410),

        ConfigurationDefinition.Create(
            key: "reliability.error_budget.block_threshold_pct",
            displayName: "config.reliability.error_budget.block_threshold_pct.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.reliability.error_budget.block_threshold_pct.description",
            defaultValue: "0",
            uiEditorType: "text",
            sortOrder: 8420),

        // ── Governance Transversal ─────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "governance.four_eyes_principle.enabled",
            displayName: "config.governance.four_eyes_principle.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.four_eyes_principle.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8500),

        ConfigurationDefinition.Create(
            key: "governance.four_eyes_principle.actions",
            displayName: "config.governance.four_eyes_principle.actions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.four_eyes_principle.actions.description",
            defaultValue: """["production_deploy","security_config_change","privileged_access_grant","compliance_waiver","break_glass"]""",
            uiEditorType: "json-editor",
            sortOrder: 8510),

        ConfigurationDefinition.Create(
            key: "governance.change_advisory_board.enabled",
            displayName: "config.governance.change_advisory_board.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.change_advisory_board.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8520),

        ConfigurationDefinition.Create(
            key: "governance.change_advisory_board.members",
            displayName: "config.governance.change_advisory_board.members.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.change_advisory_board.members.description",
            defaultValue: """[]""",
            uiEditorType: "json-editor",
            sortOrder: 8530),

        ConfigurationDefinition.Create(
            key: "governance.change_advisory_board.trigger_conditions",
            displayName: "config.governance.change_advisory_board.trigger_conditions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.change_advisory_board.trigger_conditions.description",
            defaultValue: """{"min_criticality":"High","min_blast_radius":"Medium","environment":["production"]}""",
            uiEditorType: "json-editor",
            sortOrder: 8540),

        ConfigurationDefinition.Create(
            key: "governance.compliance.framework",
            displayName: "config.governance.compliance.framework.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.compliance.framework.description",
            defaultValue: """["internal"]""",
            uiEditorType: "json-editor",
            sortOrder: 8550),

        ConfigurationDefinition.Create(
            key: "governance.compliance.auto_remediation.enabled",
            displayName: "config.governance.compliance.auto_remediation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.compliance.auto_remediation.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8560),

        // ── Product Analytics ──────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "analytics.collection.enabled",
            displayName: "config.analytics.collection.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.collection.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8600),

        ConfigurationDefinition.Create(
            key: "analytics.persona_tracking.enabled",
            displayName: "config.analytics.persona_tracking.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.persona_tracking.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8610),

        ConfigurationDefinition.Create(
            key: "analytics.max_range_days",
            displayName: "config.analytics.max_range_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.max_range_days.description",
            defaultValue: "180",
            uiEditorType: "number",
            sortOrder: 8620),

        ConfigurationDefinition.Create(
            key: "analytics.top_modules_limit",
            displayName: "config.analytics.top_modules_limit.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.top_modules_limit.description",
            defaultValue: "6",
            uiEditorType: "number",
            sortOrder: 8625),

        ConfigurationDefinition.Create(
            key: "analytics.top_features_limit",
            displayName: "config.analytics.top_features_limit.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.top_features_limit.description",
            defaultValue: "5",
            uiEditorType: "number",
            sortOrder: 8630),

        ConfigurationDefinition.Create(
            key: "analytics.trend_threshold_percent",
            displayName: "config.analytics.trend_threshold_percent.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.trend_threshold_percent.description",
            defaultValue: "0.05",
            uiEditorType: "number",
            sortOrder: 8635),

        ConfigurationDefinition.Create(
            key: "analytics.default_range",
            displayName: "config.analytics.default_range.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.default_range.description",
            defaultValue: "last_30d",
            uiEditorType: "text",
            sortOrder: 8640),

        ConfigurationDefinition.Create(
            key: "analytics.retention_days",
            displayName: "config.analytics.retention_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.analytics.retention_days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 8645),

        // ── Best Practices — DORA ──────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "governance.dora.tracking.enabled",
            displayName: "config.governance.dora.tracking.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.governance.dora.tracking.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 8700),

        ConfigurationDefinition.Create(
            key: "governance.dora.performance_targets",
            displayName: "config.governance.dora.performance_targets.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Team],
            description: "config.governance.dora.performance_targets.description",
            defaultValue: """{"deployment_frequency":"weekly","lead_time_hours":168,"mttr_hours":24,"change_failure_rate_pct":15}""",
            uiEditorType: "json-editor",
            sortOrder: 8710),

        // ── Chaos Engineering ──────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "operations.chaos.enabled",
            displayName: "config.operations.chaos.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.operations.chaos.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 8800),

        ConfigurationDefinition.Create(
            key: "operations.chaos.allowed_environments",
            displayName: "config.operations.chaos.allowed_environments.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.operations.chaos.allowed_environments.description",
            defaultValue: """["development","staging"]""",
            uiEditorType: "json-editor",
            sortOrder: 8810),

        // ── API Rate Limiting ──────────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "catalog.api.rate_limiting.default_policy",
            displayName: "config.catalog.api.rate_limiting.default_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.catalog.api.rate_limiting.default_policy.description",
            defaultValue: """{"enabled":false,"requests_per_minute":1000,"burst":100}""",
            uiEditorType: "json-editor",
            sortOrder: 8900),

        // ── Platform Customization ─────────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "platform.sidebar.user_customization.enabled",
            displayName: "config.platform.sidebar.user_customization.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.sidebar.user_customization.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9000),

        ConfigurationDefinition.Create(
            key: "platform.sidebar.pinned_items.max",
            displayName: "config.platform.sidebar.pinned_items.max.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.sidebar.pinned_items.max.description",
            defaultValue: "10",
            uiEditorType: "text",
            sortOrder: 9010),

        ConfigurationDefinition.Create(
            key: "platform.home.user_customization.enabled",
            displayName: "config.platform.home.user_customization.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.user_customization.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9020),

        ConfigurationDefinition.Create(
            key: "platform.home.default_layout",
            displayName: "config.platform.home.default_layout.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role],
            description: "config.platform.home.default_layout.description",
            defaultValue: "two-column",
            uiEditorType: "select",
            sortOrder: 9030),

        ConfigurationDefinition.Create(
            key: "platform.home.available_widgets",
            displayName: "config.platform.home.available_widgets.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role],
            description: "config.platform.home.available_widgets.description",
            defaultValue: """["team-services","change-risk","incident-overview","slo-status","pending-approvals","recent-changes","contract-health","dora-metrics","finops-summary","ai-insights","compliance-status","reliability-trend"]""",
            uiEditorType: "json-editor",
            sortOrder: 9040),

        ConfigurationDefinition.Create(
            key: "platform.home.max_widgets",
            displayName: "config.platform.home.max_widgets.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.max_widgets.description",
            defaultValue: "12",
            uiEditorType: "text",
            sortOrder: 9050),

        ConfigurationDefinition.Create(
            key: "platform.quick_actions.user_customization.enabled",
            displayName: "config.platform.quick_actions.user_customization.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.quick_actions.user_customization.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9060),

        ConfigurationDefinition.Create(
            key: "platform.custom_dashboards.enabled",
            displayName: "config.platform.custom_dashboards.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.custom_dashboards.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9070),

        ConfigurationDefinition.Create(
            key: "platform.custom_dashboards.sharing.enabled",
            displayName: "config.platform.custom_dashboards.sharing.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.custom_dashboards.sharing.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9080),

        ConfigurationDefinition.Create(
            key: "platform.custom_dashboards.max_per_user",
            displayName: "config.platform.custom_dashboards.max_per_user.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.custom_dashboards.max_per_user.description",
            defaultValue: "10",
            uiEditorType: "text",
            sortOrder: 9090),

        // ── Phase 12: Data Retention & Export/Import ─────────────────────────

        ConfigurationDefinition.Create(
            key: "data.retention.audit_log_days",
            displayName: "config.data.retention.audit_log_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.data.retention.audit_log_days.description",
            defaultValue: "365",
            uiEditorType: "text",
            sortOrder: 9100),

        ConfigurationDefinition.Create(
            key: "data.retention.telemetry_days",
            displayName: "config.data.retention.telemetry_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "config.data.retention.telemetry_days.description",
            defaultValue: "90",
            uiEditorType: "text",
            sortOrder: 9110),

        ConfigurationDefinition.Create(
            key: "data.retention.incident_history_days",
            displayName: "config.data.retention.incident_history_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.data.retention.incident_history_days.description",
            defaultValue: "730",
            uiEditorType: "text",
            sortOrder: 9120),

        ConfigurationDefinition.Create(
            key: "data.retention.change_history_days",
            displayName: "config.data.retention.change_history_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.data.retention.change_history_days.description",
            defaultValue: "730",
            uiEditorType: "text",
            sortOrder: 9130),

        ConfigurationDefinition.Create(
            key: "data.retention.ai_conversation_days",
            displayName: "config.data.retention.ai_conversation_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.data.retention.ai_conversation_days.description",
            defaultValue: "90",
            uiEditorType: "text",
            sortOrder: 9140),

        ConfigurationDefinition.Create(
            key: "data.export.max_records",
            displayName: "config.data.export.max_records.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.data.export.max_records.description",
            defaultValue: "10000",
            uiEditorType: "text",
            sortOrder: 9150),

        ConfigurationDefinition.Create(
            key: "data.export.formats_enabled",
            displayName: "config.data.export.formats_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.data.export.formats_enabled.description",
            defaultValue: """["CSV","JSON","PDF"]""",
            uiEditorType: "json-editor",
            sortOrder: 9160),

        // ── Phase 13: Onboarding & Guided Experience ─────────────────────────

        ConfigurationDefinition.Create(
            key: "onboarding.enabled",
            displayName: "config.onboarding.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.onboarding.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9200),

        ConfigurationDefinition.Create(
            key: "onboarding.checklist.enabled",
            displayName: "config.onboarding.checklist.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.onboarding.checklist.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9210),

        ConfigurationDefinition.Create(
            key: "onboarding.tooltips.enabled",
            displayName: "config.onboarding.tooltips.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.onboarding.tooltips.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9220),

        ConfigurationDefinition.Create(
            key: "onboarding.sample_data.enabled",
            displayName: "config.onboarding.sample_data.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.onboarding.sample_data.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 9230),

        // ── Phase 14: Email Templates & Communication ────────────────────────

        ConfigurationDefinition.Create(
            key: "email.templates.incident_notification",
            displayName: "config.email.templates.incident_notification.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.email.templates.incident_notification.description",
            defaultValue: "default",
            uiEditorType: "select",
            sortOrder: 9300),

        ConfigurationDefinition.Create(
            key: "email.templates.change_approval",
            displayName: "config.email.templates.change_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.email.templates.change_approval.description",
            defaultValue: "default",
            uiEditorType: "select",
            sortOrder: 9310),

        ConfigurationDefinition.Create(
            key: "email.templates.welcome",
            displayName: "config.email.templates.welcome.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.email.templates.welcome.description",
            defaultValue: "default",
            uiEditorType: "select",
            sortOrder: 9320),

        ConfigurationDefinition.Create(
            key: "email.sender.from_address",
            displayName: "config.email.sender.from_address.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.email.sender.from_address.description",
            defaultValue: "noreply@nextrace.local",
            uiEditorType: "text",
            sortOrder: 9330),

        ConfigurationDefinition.Create(
            key: "email.sender.display_name",
            displayName: "config.email.sender.display_name.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.email.sender.display_name.description",
            defaultValue: "NexTraceOne",
            uiEditorType: "text",
            sortOrder: 9340),

        // ── Phase 15: UI Density & Accessibility ─────────────────────────────

        ConfigurationDefinition.Create(
            key: "platform.ui.density",
            displayName: "config.platform.ui.density.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.density.description",
            defaultValue: "comfortable",
            uiEditorType: "select",
            sortOrder: 9400),

        ConfigurationDefinition.Create(
            key: "platform.ui.animations.enabled",
            displayName: "config.platform.ui.animations.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.animations.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9410),

        ConfigurationDefinition.Create(
            key: "platform.ui.table_rows_per_page",
            displayName: "config.platform.ui.table_rows_per_page.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.table_rows_per_page.description",
            defaultValue: "25",
            uiEditorType: "select",
            sortOrder: 9420),

        ConfigurationDefinition.Create(
            key: "platform.ui.date_display_format",
            displayName: "config.platform.ui.date_display_format.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.date_display_format.description",
            defaultValue: "relative",
            uiEditorType: "select",
            sortOrder: 9430),

        ConfigurationDefinition.Create(
            key: "platform.ui.code_font_size",
            displayName: "config.platform.ui.code_font_size.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.code_font_size.description",
            defaultValue: "13",
            uiEditorType: "text",
            sortOrder: 9440),

        ConfigurationDefinition.Create(
            key: "platform.ui.high_contrast.enabled",
            displayName: "config.platform.ui.high_contrast.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.high_contrast.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 9450),

        ConfigurationDefinition.Create(
            key: "platform.ui.reduced_motion.enabled",
            displayName: "config.platform.ui.reduced_motion.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.reduced_motion.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 9460),

        ConfigurationDefinition.Create(
            key: "platform.ui.keyboard_shortcuts.enabled",
            displayName: "config.platform.ui.keyboard_shortcuts.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.platform.ui.keyboard_shortcuts.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9470),

        // ── Phase 16: Notification User Preferences ──────────────────────────

        ConfigurationDefinition.Create(
            key: "notifications.user.email_enabled",
            displayName: "config.notifications.user.email_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.user.email_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9500),

        ConfigurationDefinition.Create(
            key: "notifications.user.inapp_enabled",
            displayName: "config.notifications.user.inapp_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.user.inapp_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9510),

        ConfigurationDefinition.Create(
            key: "notifications.user.digest_enabled",
            displayName: "config.notifications.user.digest_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.user.digest_enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 9520),

        ConfigurationDefinition.Create(
            key: "notifications.user.digest_frequency_hours",
            displayName: "config.notifications.user.digest_frequency_hours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.user.digest_frequency_hours.description",
            defaultValue: "24",
            uiEditorType: "select",
            sortOrder: 9530),

        ConfigurationDefinition.Create(
            key: "notifications.user.categories_subscribed",
            displayName: "config.notifications.user.categories_subscribed.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "config.notifications.user.categories_subscribed.description",
            defaultValue: """["Incidents","Changes","Governance","Security"]""",
            uiEditorType: "json-editor",
            sortOrder: 9540),

        // ── User Profile Preferences (Phase 1) ──────────────────────────────────
        ConfigurationDefinition.Create(
            key: "user.timezone",
            displayName: "config.user.timezone.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.timezone.description",
            defaultValue: "UTC",
            uiEditorType: "select",
            sortOrder: 9000),

        ConfigurationDefinition.Create(
            key: "user.date_format",
            displayName: "config.user.date_format.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.date_format.description",
            defaultValue: "yyyy-MM-dd",
            validationRules: """{"enum":["yyyy-MM-dd","MM/dd/yyyy","dd/MM/yyyy","dd.MM.yyyy"]}""",
            uiEditorType: "select",
            sortOrder: 9010),

        ConfigurationDefinition.Create(
            key: "user.time_format",
            displayName: "config.user.time_format.label",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.time_format.description",
            defaultValue: "HH:mm:ss",
            validationRules: """{"enum":["HH:mm:ss","hh:mm:ss a","HH:mm"]}""",
            uiEditorType: "select",
            sortOrder: 9020),

        ConfigurationDefinition.Create(
            key: "user.items_per_page",
            displayName: "config.user.items_per_page.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.items_per_page.description",
            defaultValue: "25",
            validationRules: """{"enum":[10,25,50,100]}""",
            uiEditorType: "select",
            sortOrder: 9030),

        // ── Default Scope Preferences ────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "default.environment",
            displayName: "config.default.environment.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.default.environment.description",
            uiEditorType: "select",
            sortOrder: 9100),

        ConfigurationDefinition.Create(
            key: "default.team",
            displayName: "config.default.team.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.default.team.description",
            uiEditorType: "select",
            sortOrder: 9110),

        ConfigurationDefinition.Create(
            key: "default.service",
            displayName: "config.default.service.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.default.service.description",
            uiEditorType: "select",
            sortOrder: 9120),

        // ── Table Column Preferences ─────────────────────────────────────────────
        ConfigurationDefinition.Create(
            key: "table.columns.catalog.services",
            displayName: "config.table.columns.catalog.services.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.table.columns.catalog.services.description",
            defaultValue: """["name","team","type","status","criticality"]""",
            uiEditorType: "json",
            sortOrder: 9200),

        ConfigurationDefinition.Create(
            key: "table.columns.changes.list",
            displayName: "config.table.columns.changes.list.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.table.columns.changes.list.description",
            defaultValue: """["title","service","environment","status","risk","createdAt"]""",
            uiEditorType: "json",
            sortOrder: 9210),

        ConfigurationDefinition.Create(
            key: "table.columns.contracts.list",
            displayName: "config.table.columns.contracts.list.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.table.columns.contracts.list.description",
            defaultValue: """["name","service","type","version","status"]""",
            uiEditorType: "json",
            sortOrder: 9220),

        ConfigurationDefinition.Create(
            key: "table.columns.incidents.list",
            displayName: "config.table.columns.incidents.list.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.table.columns.incidents.list.description",
            defaultValue: """["title","service","severity","status","assignee","createdAt"]""",
            uiEditorType: "json",
            sortOrder: 9230),

        ConfigurationDefinition.Create(
            key: "platform.home.template.engineer",
            displayName: "config.platform.home.template.engineer.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.template.engineer.description",
            defaultValue: """["team-services","recent-changes","active-incidents","dora-metrics"]""",
            uiEditorType: "json",
            sortOrder: 9300),

        ConfigurationDefinition.Create(
            key: "platform.home.template.tech_lead",
            displayName: "config.platform.home.template.tech_lead.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.template.tech_lead.description",
            defaultValue: """["team-services","change-risk","pending-approvals","slo-status","dora-metrics"]""",
            uiEditorType: "json",
            sortOrder: 9310),

        ConfigurationDefinition.Create(
            key: "platform.home.template.architect",
            displayName: "config.platform.home.template.architect.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.template.architect.description",
            defaultValue: """["contract-health","dependency-map","compliance-status","reliability-trend"]""",
            uiEditorType: "json",
            sortOrder: 9320),

        ConfigurationDefinition.Create(
            key: "platform.home.template.executive",
            displayName: "config.platform.home.template.executive.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.template.executive.description",
            defaultValue: """["finops-summary","compliance-status","incident-overview","dora-metrics"]""",
            uiEditorType: "json",
            sortOrder: 9330),

        ConfigurationDefinition.Create(
            key: "platform.home.template.platform_admin",
            displayName: "config.platform.home.template.platform_admin.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.platform.home.template.platform_admin.description",
            defaultValue: """["ai-usage","security-findings","audit-activity","system-health"]""",
            uiEditorType: "json",
            sortOrder: 9340),

        ConfigurationDefinition.Create(
            key: "home.widget.notes.content",
            displayName: "config.home.widget.notes.content.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.home.widget.notes.content.description",
            defaultValue: "",
            uiEditorType: "markdown",
            sortOrder: 9350),

        // ── Phase 3: Quiet Hours ──────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.enabled",
            displayName: "config.notifications.quiet_hours.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.User],
            description: "config.notifications.quiet_hours.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 9400),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.start",
            displayName: "config.notifications.quiet_hours.start.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.notifications.quiet_hours.start.description",
            defaultValue: "22:00",
            uiEditorType: "time",
            sortOrder: 9410),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.end",
            displayName: "config.notifications.quiet_hours.end.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.notifications.quiet_hours.end.description",
            defaultValue: "08:00",
            uiEditorType: "time",
            sortOrder: 9420),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.timezone",
            displayName: "config.notifications.quiet_hours.timezone.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.notifications.quiet_hours.timezone.description",
            defaultValue: "UTC",
            uiEditorType: "timezone-select",
            sortOrder: 9430),

        // ── Phase 3: Digest ───────────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "notifications.digest.frequency",
            displayName: "config.notifications.digest.frequency.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.notifications.digest.frequency.description",
            defaultValue: "daily",
            uiEditorType: "select",
            sortOrder: 9440),

        ConfigurationDefinition.Create(
            key: "notifications.digest.sections",
            displayName: "config.notifications.digest.sections.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.notifications.digest.sections.description",
            defaultValue: """["changes","incidents","contracts","compliance"]""",
            uiEditorType: "json",
            sortOrder: 9450),

        // ── Phase 7 — AI Customization (User-level) ───────────────────────

        ConfigurationDefinition.Create(
            key: "user.ai.response_verbosity",
            displayName: "config.user.ai.response_verbosity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.ai.response_verbosity.description",
            defaultValue: "standard",
            uiEditorType: "select",
            sortOrder: 9500),

        ConfigurationDefinition.Create(
            key: "user.ai.preferred_language",
            displayName: "config.user.ai.preferred_language.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.ai.preferred_language.description",
            defaultValue: "en",
            uiEditorType: "text",
            sortOrder: 9510),

        ConfigurationDefinition.Create(
            key: "user.ai.auto_context_scope",
            displayName: "config.user.ai.auto_context_scope.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.ai.auto_context_scope.description",
            defaultValue: "team",
            uiEditorType: "select",
            sortOrder: 9520),

        ConfigurationDefinition.Create(
            key: "user.ai.knowledge_sources",
            displayName: "config.user.ai.knowledge_sources.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.user.ai.knowledge_sources.description",
            defaultValue: """["contracts","services","changes","incidents","runbooks"]""",
            uiEditorType: "json",
            sortOrder: 9530),

        // ── Phase 6 — Reports Seeds ──────────────────

        ConfigurationDefinition.Create(
            key: "reports.saved_templates",
            displayName: "config.reports.saved_templates.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.User],
            description: "config.reports.saved_templates.description",
            defaultValue: "[]",
            uiEditorType: "json",
            sortOrder: 9540),

        ConfigurationDefinition.Create(
            key: "reports.export.default_format",
            displayName: "config.reports.export.default_format.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.User, ConfigurationScope.Tenant],
            description: "config.reports.export.default_format.description",
            defaultValue: "csv",
            uiEditorType: "select",
            sortOrder: 9545),

        // ── Phase 8 — Integrations & API (Tenant-level) ──────────────────

        ConfigurationDefinition.Create(
            key: "integration.field_mapping.enabled",
            displayName: "config.integration.field_mapping.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integration.field_mapping.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 9600),

        // ═══════════════════════════════════════════════════════════════════
        // ENVIRONMENT BEHAVIOR PARAMETERS
        // Parâmetros que controlam o comportamento operacional da plataforma
        // por ambiente cadastrado pelo utilizador.
        // Regra: IA NÃO é parametrizada por ambiente (scope System/Tenant apenas).
        // O ambiente é passado como contexto de execução à IA, nunca como
        // configuração de modelo, budget ou política.
        // ═══════════════════════════════════════════════════════════════════

        // ── Notificações por Ambiente ──────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.notifications.external_channels.enabled",
            displayName: "config.env.behavior.notifications.external_channels.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.notifications.external_channels.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9700),

        ConfigurationDefinition.Create(
            key: "env.behavior.notifications.minimum_severity",
            displayName: "config.env.behavior.notifications.minimum_severity.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.notifications.minimum_severity.description",
            defaultValue: "Warning",
            validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""",
            isInheritable: true,
            uiEditorType: "select",
            sortOrder: 9710),

        // ── Métricas e DORA por Ambiente ───────────────────────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.metrics.dora.enabled",
            displayName: "config.env.behavior.metrics.dora.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.metrics.dora.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9720),

        ConfigurationDefinition.Create(
            key: "env.behavior.metrics.slo.enabled",
            displayName: "config.env.behavior.metrics.slo.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.metrics.slo.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9730),

        ConfigurationDefinition.Create(
            key: "env.behavior.metrics.change_confidence.enabled",
            displayName: "config.env.behavior.metrics.change_confidence.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.metrics.change_confidence.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9740),

        ConfigurationDefinition.Create(
            key: "env.behavior.metrics.blast_radius.enabled",
            displayName: "config.env.behavior.metrics.blast_radius.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.metrics.blast_radius.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9750),

        // ── Background Jobs por Ambiente ───────────────────────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.jobs.telemetry_retention.enabled",
            displayName: "config.env.behavior.jobs.telemetry_retention.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.jobs.telemetry_retention.enabled.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9760),

        ConfigurationDefinition.Create(
            key: "env.behavior.jobs.scheduled_reports.enabled",
            displayName: "config.env.behavior.jobs.scheduled_reports.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.jobs.scheduled_reports.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9770),

        ConfigurationDefinition.Create(
            key: "env.behavior.jobs.governance_waiver_expiry.enabled",
            displayName: "config.env.behavior.jobs.governance_waiver_expiry.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.jobs.governance_waiver_expiry.enabled.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9780),

        // ── Retenção de Dados por Ambiente ─────────────────────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.data.telemetry_retention_days",
            displayName: "config.env.behavior.data.telemetry_retention_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.data.telemetry_retention_days.description",
            defaultValue: "30",
            validationRules: """{"min":1,"max":730}""",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 9790),

        ConfigurationDefinition.Create(
            key: "env.behavior.data.change_history_days",
            displayName: "config.env.behavior.data.change_history_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.data.change_history_days.description",
            defaultValue: "90",
            validationRules: """{"min":1,"max":1825}""",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 9800),

        ConfigurationDefinition.Create(
            key: "env.behavior.data.incident_history_days",
            displayName: "config.env.behavior.data.incident_history_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.data.incident_history_days.description",
            defaultValue: "90",
            validationRules: """{"min":1,"max":1825}""",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 9810),

        // ── Alertas Operacionais por Ambiente ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.alerts.slo_breach.enabled",
            displayName: "config.env.behavior.alerts.slo_breach.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.alerts.slo_breach.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9820),

        ConfigurationDefinition.Create(
            key: "env.behavior.alerts.anomaly_detection.enabled",
            displayName: "config.env.behavior.alerts.anomaly_detection.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.alerts.anomaly_detection.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9830),

        // ── Webhooks e Integrações Externas por Ambiente ───────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.webhooks.outbound.enabled",
            displayName: "config.env.behavior.webhooks.outbound.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.webhooks.outbound.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9840),

        // ── Pipeline de Mudanças por Ambiente ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "env.behavior.change.promotion_gates.enabled",
            displayName: "config.env.behavior.change.promotion_gates.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.change.promotion_gates.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9850),

        ConfigurationDefinition.Create(
            key: "env.behavior.change.ingest.enabled",
            displayName: "config.env.behavior.change.ingest.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.change.ingest.enabled.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9860),

        ConfigurationDefinition.Create(
            key: "env.behavior.change.post_change_verification.enabled",
            displayName: "config.env.behavior.change.post_change_verification.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.change.post_change_verification.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9870),

        ConfigurationDefinition.Create(
            key: "env.behavior.jobs.non_prod_scheduler.enabled",
            displayName: "config.env.behavior.jobs.non_prod_scheduler.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.jobs.non_prod_scheduler.enabled.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9880),

        ConfigurationDefinition.Create(
            key: "env.behavior.notifications.escalation.enabled",
            displayName: "config.env.behavior.notifications.escalation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.notifications.escalation.enabled.description",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9890),

        ConfigurationDefinition.Create(
            key: "env.behavior.notifications.digest.enabled",
            displayName: "config.env.behavior.notifications.digest.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "config.env.behavior.notifications.digest.enabled.description",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 9900),

        // ── Block H — Change Confidence Score 2.0 — Pesos e Thresholds ─────

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.testCoverage",
            displayName: "config.change.confidence.weights.testCoverage.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.testCoverage.description",
            defaultValue: "0.15",
            uiEditorType: "number",
            sortOrder: 4700),

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.contractStability",
            displayName: "config.change.confidence.weights.contractStability.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.contractStability.description",
            defaultValue: "0.20",
            uiEditorType: "number",
            sortOrder: 4710),

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.historicalRegression",
            displayName: "config.change.confidence.weights.historicalRegression.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.historicalRegression.description",
            defaultValue: "0.15",
            uiEditorType: "number",
            sortOrder: 4720),

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.blastSurface",
            displayName: "config.change.confidence.weights.blastSurface.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.blastSurface.description",
            defaultValue: "0.15",
            uiEditorType: "number",
            sortOrder: 4730),

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.dependencyHealth",
            displayName: "config.change.confidence.weights.dependencyHealth.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.dependencyHealth.description",
            defaultValue: "0.10",
            uiEditorType: "number",
            sortOrder: 4740),

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.canarySignal",
            displayName: "config.change.confidence.weights.canarySignal.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.canarySignal.description",
            defaultValue: "0.10",
            uiEditorType: "number",
            sortOrder: 4750),

        ConfigurationDefinition.Create(
            key: "change.confidence.weights.preProdDelta",
            displayName: "config.change.confidence.weights.preProdDelta.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.weights.preProdDelta.description",
            defaultValue: "0.15",
            uiEditorType: "number",
            sortOrder: 4760),

        ConfigurationDefinition.Create(
            key: "change.confidence.minConfidenceForPromotion",
            displayName: "config.change.confidence.minConfidenceForPromotion.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.minConfidenceForPromotion.description",
            defaultValue: "70",
            uiEditorType: "number",
            sortOrder: 4770),

        ConfigurationDefinition.Create(
            key: "change.confidence.historicalWindow.days",
            displayName: "config.change.confidence.historicalWindow.days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.change.confidence.historicalWindow.days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 4780),

        ConfigurationDefinition.Create(
            key: "ai.evaluation.defaults.latencyBudgetMs",
            displayName: "config.ai.evaluation.defaults.latencyBudgetMs.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.evaluation.defaults.latencyBudgetMs.description",
            defaultValue: "5000",
            uiEditorType: "number",
            sortOrder: 4900),

        ConfigurationDefinition.Create(
            key: "ai.evaluation.defaults.llmJudge.model",
            displayName: "config.ai.evaluation.defaults.llmJudge.model.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.evaluation.defaults.llmJudge.model.description",
            defaultValue: "gpt-4o-mini",
            uiEditorType: "text",
            sortOrder: 4910),

        ConfigurationDefinition.Create(
            key: "ai.evaluation.runs.retentionDays",
            displayName: "config.ai.evaluation.runs.retentionDays.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.evaluation.runs.retentionDays.description",
            defaultValue: "180",
            uiEditorType: "number",
            sortOrder: 4920),

        ConfigurationDefinition.Create(
            key: "ai.evaluation.runs.maxConcurrency",
            displayName: "config.ai.evaluation.runs.maxConcurrency.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.evaluation.runs.maxConcurrency.description",
            defaultValue: "5",
            uiEditorType: "number",
            sortOrder: 4930),

        ConfigurationDefinition.Create(
            key: "ai.evaluation.humanReview.slaHours",
            displayName: "config.ai.evaluation.humanReview.slaHours.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.evaluation.humanReview.slaHours.description",
            defaultValue: "48",
            uiEditorType: "number",
            sortOrder: 4940),

        // ── Service Catalog — Tier & Ownership Drift (Wave A.3) ───────────

        ConfigurationDefinition.Create(
            key: "catalog.ownershipDrift.threshold.days",
            displayName: "config.catalog.ownershipDrift.thresholdDays.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.ownershipDrift.thresholdDays.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 5100),

        ConfigurationDefinition.Create(
            key: "catalog.tier.critical.sloMinPercent",
            displayName: "config.catalog.tier.critical.sloMinPercent.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.tier.critical.sloMinPercent.description",
            defaultValue: "99.9",
            uiEditorType: "number",
            sortOrder: 5110),

        ConfigurationDefinition.Create(
            key: "catalog.tier.standard.sloMinPercent",
            displayName: "config.catalog.tier.standard.sloMinPercent.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.tier.standard.sloMinPercent.description",
            defaultValue: "99.5",
            uiEditorType: "number",
            sortOrder: 5120),

        ConfigurationDefinition.Create(
            key: "catalog.tier.experimental.sloMinPercent",
            displayName: "config.catalog.tier.experimental.sloMinPercent.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.tier.experimental.sloMinPercent.description",
            defaultValue: "99.0",
            uiEditorType: "number",
            sortOrder: 5130),

        ConfigurationDefinition.Create(
            key: "catalog.tier.critical.maturityMinScore",
            displayName: "config.catalog.tier.critical.maturityMinScore.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.tier.critical.maturityMinScore.description",
            defaultValue: "0.8",
            uiEditorType: "number",
            sortOrder: 5140),

        ConfigurationDefinition.Create(
            key: "catalog.tier.standard.maturityMinScore",
            displayName: "config.catalog.tier.standard.maturityMinScore.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.catalog.tier.standard.maturityMinScore.description",
            defaultValue: "0.6",
            uiEditorType: "number",
            sortOrder: 5150),

        // ── Operational Intelligence — Correlation Feature Scoring (Wave A.5) ───────────

        ConfigurationDefinition.Create(
            key: "oi.correlation.feature.temporalWeight",
            displayName: "config.oi.correlation.feature.temporalWeight.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.oi.correlation.feature.temporalWeight.description",
            defaultValue: "0.4",
            uiEditorType: "number",
            sortOrder: 5200),

        ConfigurationDefinition.Create(
            key: "oi.correlation.feature.serviceWeight",
            displayName: "config.oi.correlation.feature.serviceWeight.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.oi.correlation.feature.serviceWeight.description",
            defaultValue: "0.4",
            uiEditorType: "number",
            sortOrder: 5210),

        ConfigurationDefinition.Create(
            key: "oi.correlation.feature.ownershipWeight",
            displayName: "config.oi.correlation.feature.ownershipWeight.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.oi.correlation.feature.ownershipWeight.description",
            defaultValue: "0.2",
            uiEditorType: "number",
            sortOrder: 5220),

        ConfigurationDefinition.Create(
            key: "oi.runbook.maxConcurrentExecutions",
            displayName: "config.oi.runbook.maxConcurrentExecutions.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.oi.runbook.maxConcurrentExecutions.description",
            defaultValue: "5",
            uiEditorType: "number",
            sortOrder: 5230),

        ConfigurationDefinition.Create(
            key: "oi.similarity.lookbackDays",
            displayName: "config.oi.similarity.lookbackDays.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.oi.similarity.lookbackDays.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 5240),

        ConfigurationDefinition.Create(
            key: "oi.similarity.minScore",
            displayName: "config.oi.similarity.minScore.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.oi.similarity.minScore.description",
            defaultValue: "1",
            uiEditorType: "number",
            sortOrder: 5250),

        // ── Block: Ecosystem Integrations — Backstage Bridge & External Change ─

        ConfigurationDefinition.Create(
            key: "integrations.backstage.instanceUrl",
            displayName: "config.integrations.backstage.instanceUrl.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.backstage.instanceUrl.description",
            defaultValue: "",
            uiEditorType: "text",
            sortOrder: 5450),

        ConfigurationDefinition.Create(
            key: "integrations.backstage.exportEnabled",
            displayName: "config.integrations.backstage.exportEnabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.backstage.exportEnabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5460),

        ConfigurationDefinition.Create(
            key: "integrations.externalChange.autoLinkEnabled",
            displayName: "config.integrations.externalChange.autoLinkEnabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.externalChange.autoLinkEnabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 5470),

        ConfigurationDefinition.Create(
            key: "integrations.externalChange.allowedSystems",
            displayName: "config.integrations.externalChange.allowedSystems.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.integrations.externalChange.allowedSystems.description",
            defaultValue: """["ServiceNow","Jira","AzureDevOps","Generic"]""",
            uiEditorType: "json-editor",
            sortOrder: 5480),

        // ── Block: FinOps Contextual & Knowledge Hub ─────────────────────────

        ConfigurationDefinition.Create(
            key: "finops.waste.detection_enabled",
            displayName: "config.finops.waste.detection_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.waste.detection_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5700),

        ConfigurationDefinition.Create(
            key: "finops.waste.thresholds",
            displayName: "config.finops.waste.thresholds.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.waste.thresholds.description",
            defaultValue: """{"overbudgetPct":20,"idleZeroCostPeriods":2}""",
            uiEditorType: "json-editor",
            sortOrder: 5710),

        ConfigurationDefinition.Create(
            key: "finops.waste.categories",
            displayName: "config.finops.waste.categories.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.waste.categories.description",
            defaultValue: """["IdleResources","Overprovisioned","UnattachedStorage","UnusedLicenses","OrphanedResources","OverlappingServices"]""",
            uiEditorType: "json-editor",
            sortOrder: 5720),

        ConfigurationDefinition.Create(
            key: "finops.focus.export_enabled",
            displayName: "config.finops.focus.export_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.focus.export_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5730),

        ConfigurationDefinition.Create(
            key: "finops.focus.schema_version",
            displayName: "config.finops.focus.schema_version.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.finops.focus.schema_version.description",
            defaultValue: "FOCUS_1.0",
            uiEditorType: "text",
            sortOrder: 5740),

        ConfigurationDefinition.Create(
            key: "finops.change_gate.enabled",
            displayName: "config.finops.change_gate.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.finops.change_gate.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5750),

        ConfigurationDefinition.Create(
            key: "ai.cost_attribution.enabled",
            displayName: "config.ai.cost_attribution.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.cost_attribution.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5760),

        ConfigurationDefinition.Create(
            key: "ai.cost_attribution.period_days",
            displayName: "config.ai.cost_attribution.period_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.ai.cost_attribution.period_days.description",
            defaultValue: "30",
            uiEditorType: "number",
            sortOrder: 5770),

        ConfigurationDefinition.Create(
            key: "knowledge.freshness.score_enabled",
            displayName: "config.knowledge.freshness.score_enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.freshness.score_enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5780),

        ConfigurationDefinition.Create(
            key: "knowledge.freshness.stale_threshold_days",
            displayName: "config.knowledge.freshness.stale_threshold_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.freshness.stale_threshold_days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 5790),

        ConfigurationDefinition.Create(
            key: "knowledge.runbook_proposal.enabled",
            displayName: "config.knowledge.runbook_proposal.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.runbook_proposal.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5800),

        ConfigurationDefinition.Create(
            key: "knowledge.runbook_proposal.auto_approve_threshold",
            displayName: "config.knowledge.runbook_proposal.auto_approve_threshold.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.runbook_proposal.auto_approve_threshold.description",
            defaultValue: "0",
            uiEditorType: "number",
            sortOrder: 5810),

        ConfigurationDefinition.Create(
            key: "knowledge.search.max_results",
            displayName: "config.knowledge.search.max_results.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.knowledge.search.max_results.description",
            defaultValue: "20",
            uiEditorType: "number",
            sortOrder: 5820),

        // ── Wave C.1 — Vulnerability Gate ─────────────────────────────────

        ConfigurationDefinition.Create(
            key: "security.vulnerability.gate.enabled",
            displayName: "config.security.vulnerability.gate.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.vulnerability.gate.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5900),

        ConfigurationDefinition.Create(
            key: "security.vulnerability.gate.max_critical",
            displayName: "config.security.vulnerability.gate.max_critical.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.vulnerability.gate.max_critical.description",
            defaultValue: "0",
            uiEditorType: "number",
            sortOrder: 5910),

        ConfigurationDefinition.Create(
            key: "security.vulnerability.gate.max_high",
            displayName: "config.security.vulnerability.gate.max_high.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.vulnerability.gate.max_high.description",
            defaultValue: "5",
            uiEditorType: "number",
            sortOrder: 5920),

        ConfigurationDefinition.Create(
            key: "security.vulnerability.ingest.enabled",
            displayName: "config.security.vulnerability.ingest.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.vulnerability.ingest.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5930),

        ConfigurationDefinition.Create(
            key: "security.vulnerability.ingest.max_batch_size",
            displayName: "config.security.vulnerability.ingest.max_batch_size.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.vulnerability.ingest.max_batch_size.description",
            defaultValue: "100",
            uiEditorType: "number",
            sortOrder: 5940),

        // ── Wave C.2 — Evidence Pack Signing ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "security.evidence_pack.signing_key",
            displayName: "config.security.evidence_pack.signing_key.label",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "config.security.evidence_pack.signing_key.description",
            defaultValue: "change-me-in-production",
            uiEditorType: "password",
            sortOrder: 5950),

        ConfigurationDefinition.Create(
            key: "security.evidence_pack.require_signature_for_audit",
            displayName: "config.security.evidence_pack.require_signature_for_audit.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.evidence_pack.require_signature_for_audit.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 5960),

        // ── Wave C.2 — Access Review Escalation ───────────────────────────

        ConfigurationDefinition.Create(
            key: "security.access_review.escalation.days_before_deadline",
            displayName: "config.security.access_review.escalation.days_before_deadline.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.access_review.escalation.days_before_deadline.description",
            defaultValue: "3",
            uiEditorType: "number",
            sortOrder: 5970),

        ConfigurationDefinition.Create(
            key: "security.access_review.escalation.enabled",
            displayName: "config.security.access_review.escalation.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.security.access_review.escalation.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5980),

        // ── Wave D.1 — Digital Twin ────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "digital_twin.what_if.enabled",
            displayName: "config.digital_twin.what_if.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.digital_twin.what_if.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9910),

        ConfigurationDefinition.Create(
            key: "digital_twin.what_if.max_consumers_analysis",
            displayName: "config.digital_twin.what_if.max_consumers_analysis.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.digital_twin.what_if.max_consumers_analysis.description",
            defaultValue: "50",
            uiEditorType: "number",
            sortOrder: 9920),

        ConfigurationDefinition.Create(
            key: "digital_twin.topology_snapshot.enabled",
            displayName: "config.digital_twin.topology_snapshot.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.digital_twin.topology_snapshot.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9930),

        // ── Wave D.4 — Agent API ───────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "agent.api.token.max_per_tenant",
            displayName: "config.agent.api.token.max_per_tenant.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.agent.api.token.max_per_tenant.description",
            defaultValue: "20",
            uiEditorType: "number",
            sortOrder: 9940),

        ConfigurationDefinition.Create(
            key: "agent.api.token.default_expiry_days",
            displayName: "config.agent.api.token.default_expiry_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.agent.api.token.default_expiry_days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 9950),

        ConfigurationDefinition.Create(
            key: "agent.api.query_audit.enabled",
            displayName: "config.agent.api.query_audit.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.agent.api.query_audit.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9960),

        ConfigurationDefinition.Create(
            key: "agent.api.query_audit.retention_days",
            displayName: "config.agent.api.query_audit.retention_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.agent.api.query_audit.retention_days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 9970),

        // ── Wave D.1.b — Digital Twin: Failure Simulation ────────────────

        ConfigurationDefinition.Create(
            key: "digital_twin.failure_sim.max_depth",
            displayName: "config.digital_twin.failure_sim.max_depth.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.digital_twin.failure_sim.max_depth.description",
            defaultValue: "3",
            uiEditorType: "number",
            sortOrder: 9980),

        // ── NIS2 Compliance Report ────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "compliance.nis2.enabled",
            displayName: "config.compliance.nis2.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.compliance.nis2.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 9990),

        ConfigurationDefinition.Create(
            key: "compliance.nis2.report.period_days",
            displayName: "config.compliance.nis2.report.period_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.compliance.nis2.report.period_days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 10000),

        ConfigurationDefinition.Create(
            key: "slsa.provenance.required_for_production",
            displayName: "config.slsa.provenance.required_for_production.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.slsa.provenance.required_for_production.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 10010),

        ConfigurationDefinition.Create(
            key: "slsa.artifact_gate.enabled",
            displayName: "config.slsa.artifact_gate.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.slsa.artifact_gate.enabled.description",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 10020),

        ConfigurationDefinition.Create(
            key: "profiling.enabled",
            displayName: "config.profiling.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.profiling.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10030),

        ConfigurationDefinition.Create(
            key: "profiling.retention.days",
            displayName: "config.profiling.retention.days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.profiling.retention.days.description",
            defaultValue: "90",
            uiEditorType: "number",
            sortOrder: 10040),

        ConfigurationDefinition.Create(
            key: "profiling.max_sessions_per_service",
            displayName: "config.profiling.max_sessions_per_service.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.profiling.max_sessions_per_service.description",
            defaultValue: "500",
            uiEditorType: "number",
            sortOrder: 10050),

        // ── Wave D.2 — Cross-tenant Benchmarks anonimizados ──────────────────

        ConfigurationDefinition.Create(
            key: "benchmark.consent.enabled",
            displayName: "config.benchmark.consent.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.benchmark.consent.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10060),

        ConfigurationDefinition.Create(
            key: "benchmark.min_peer_set_size",
            displayName: "config.benchmark.min_peer_set_size.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System],
            description: "config.benchmark.min_peer_set_size.description",
            defaultValue: "5",
            uiEditorType: "number",
            sortOrder: 10070),

        ConfigurationDefinition.Create(
            key: "benchmark.snapshot.retention_days",
            displayName: "config.benchmark.snapshot.retention_days.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System],
            description: "config.benchmark.snapshot.retention_days.description",
            defaultValue: "365",
            uiEditorType: "number",
            sortOrder: 10075),

        // ── Wave D.3 — No-code Policy Studio ─────────────────────────────────

        ConfigurationDefinition.Create(
            key: "policy_studio.enabled",
            displayName: "config.policy_studio.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.policy_studio.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10080),

        ConfigurationDefinition.Create(
            key: "policy_studio.max_rules_per_policy",
            displayName: "config.policy_studio.max_rules_per_policy.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.policy_studio.max_rules_per_policy.description",
            defaultValue: "20",
            uiEditorType: "number",
            sortOrder: 10090),

        ConfigurationDefinition.Create(
            key: "policy_studio.evaluation.fail_open",
            displayName: "config.policy_studio.evaluation.fail_open.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.policy_studio.evaluation.fail_open.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10100),

        // ── Wave F.1 — Release Calendar ──────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "release_calendar.enabled",
            displayName: "config.release_calendar.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.release_calendar.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10110),

        ConfigurationDefinition.Create(
            key: "release_calendar.freeze.default_hours_before_release",
            displayName: "config.release_calendar.freeze.default_hours_before_release.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.release_calendar.freeze.default_hours_before_release.description",
            defaultValue: "48",
            uiEditorType: "number",
            sortOrder: 10120),

        ConfigurationDefinition.Create(
            key: "release_calendar.hotfix.requires_approval",
            displayName: "config.release_calendar.hotfix.requires_approval.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.release_calendar.hotfix.requires_approval.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10130),

        // ── Wave F.2 — Risk Center ────────────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "risk_center.enabled",
            displayName: "config.risk_center.enabled.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.risk_center.enabled.description",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 10140),

        ConfigurationDefinition.Create(
            key: "risk_center.critical_score_threshold",
            displayName: "config.risk_center.critical_score_threshold.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.risk_center.critical_score_threshold.description",
            defaultValue: "80",
            uiEditorType: "number",
            sortOrder: 10150),

        ConfigurationDefinition.Create(
            key: "risk_center.report.max_services",
            displayName: "config.risk_center.report.max_services.label",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "config.risk_center.report.max_services.description",
            defaultValue: "50",
            uiEditorType: "number",
            sortOrder: 10160),
    ];
}
