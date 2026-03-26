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

        // ── PHASE 2: Notification & Communication Parameterization ─────────────

        // --- Types, Categories & Severities ---

        ConfigurationDefinition.Create(
            key: "notifications.types.enabled",
            displayName: "Enabled Notification Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "JSON array of enabled notification type identifiers. Only listed types will be processed.",
            defaultValue: """["IncidentCreated","IncidentEscalated","IncidentResolved","AnomalyDetected","HealthDegradation","ApprovalPending","ApprovalApproved","ApprovalRejected","ApprovalExpiring","ContractPublished","BreakingChangeDetected","ContractValidationFailed","BreakGlassActivated","JitAccessPending","JitAccessGranted","UserRoleChanged","AccessReviewPending","ComplianceCheckFailed","PolicyViolated","EvidenceExpiring","BudgetExceeded","BudgetThresholdReached","IntegrationFailed","SyncFailed","ConnectorAuthFailed","AiProviderUnavailable","TokenBudgetExceeded","AiGenerationFailed","AiActionBlockedByPolicy"]""",
            uiEditorType: "json-editor",
            sortOrder: 150),

        ConfigurationDefinition.Create(
            key: "notifications.categories.enabled",
            displayName: "Enabled Notification Categories",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON array of enabled notification categories. Only listed categories will be active.",
            defaultValue: """["Incident","Approval","Change","Contract","Security","Compliance","FinOps","AI","Integration","Platform","Informational"]""",
            uiEditorType: "json-editor",
            sortOrder: 151),

        ConfigurationDefinition.Create(
            key: "notifications.severity.default",
            displayName: "Default Notification Severity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default severity assigned to notifications when not explicitly specified by the event.",
            defaultValue: "Info",
            validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""",
            uiEditorType: "select",
            sortOrder: 152),

        ConfigurationDefinition.Create(
            key: "notifications.severity.minimum_for_external",
            displayName: "Minimum Severity for External Channels",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Minimum severity level required for notifications to be delivered via external channels (Email, Teams).",
            defaultValue: "Warning",
            validationRules: """{"enum":["Info","ActionRequired","Warning","Critical"]}""",
            uiEditorType: "select",
            sortOrder: 153),

        ConfigurationDefinition.Create(
            key: "notifications.mandatory.types",
            displayName: "Mandatory Notification Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "JSON array of notification types that are mandatory and cannot be disabled by user preferences.",
            defaultValue: """["BreakGlassActivated","IncidentCreated","IncidentEscalated","ApprovalPending","ComplianceCheckFailed"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 154),

        ConfigurationDefinition.Create(
            key: "notifications.mandatory.severities",
            displayName: "Mandatory Notification Severities",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "JSON array of severities that are always mandatory regardless of user preferences.",
            defaultValue: """["Critical"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 155),

        // --- Channels Allowed & Mandatory ---

        ConfigurationDefinition.Create(
            key: "notifications.channels.inapp.enabled",
            displayName: "In-App Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether in-app notifications are active. In-App is always the baseline channel.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 160),

        ConfigurationDefinition.Create(
            key: "notifications.channels.allowed_by_type",
            displayName: "Allowed Channels per Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object mapping notification types to arrays of allowed delivery channels.",
            defaultValue: """{}""",
            uiEditorType: "json-editor",
            sortOrder: 161),

        ConfigurationDefinition.Create(
            key: "notifications.channels.mandatory_by_severity",
            displayName: "Mandatory Channels per Severity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "JSON object mapping severities to arrays of mandatory channels that override user preferences.",
            defaultValue: """{"Critical":["InApp","Email","MicrosoftTeams"],"Warning":["InApp","Email"]}""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 162),

        ConfigurationDefinition.Create(
            key: "notifications.channels.mandatory_by_type",
            displayName: "Mandatory Channels per Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "JSON object mapping specific notification types to their mandatory channels.",
            defaultValue: """{"BreakGlassActivated":["InApp","Email","MicrosoftTeams"],"ApprovalPending":["InApp","Email"],"ComplianceCheckFailed":["InApp","Email"]}""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 163),

        ConfigurationDefinition.Create(
            key: "notifications.channels.disabled_in_environment",
            displayName: "Channels Disabled by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.Environment],
            description: "JSON array of channels disabled in this environment. Example: [\"Email\",\"MicrosoftTeams\"] for dev environments.",
            defaultValue: "[]",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 164),

        // --- Templates ---

        ConfigurationDefinition.Create(
            key: "notifications.templates.internal",
            displayName: "Internal Notification Templates",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object with internal notification templates. Each key is a notification type, value is {title, message, placeholders}.",
            defaultValue: """{"IncidentCreated":{"title":"Incident created — {ServiceName}","message":"A new incident with severity {IncidentSeverity} has been created for service {ServiceName}.","placeholders":["ServiceName","IncidentSeverity"]},"ApprovalPending":{"title":"Approval required — {EntityName}","message":"A new approval has been requested by {RequestedBy} for {EntityName}.","placeholders":["EntityName","RequestedBy"]},"BreakGlassActivated":{"title":"Break-glass access activated","message":"Emergency break-glass access was activated by {ActivatedBy}.","placeholders":["ActivatedBy"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 170),

        ConfigurationDefinition.Create(
            key: "notifications.templates.email",
            displayName: "Email Notification Templates",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object with email templates per notification type. Each contains subject, bodyHtml, and placeholders.",
            defaultValue: """{"default":{"subject":"[NexTraceOne] {Title}","bodyHtml":"<h2>{Title}</h2><p>{Message}</p><p><a href='{ActionUrl}'>View details</a></p>","placeholders":["Title","Message","ActionUrl"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 171),

        ConfigurationDefinition.Create(
            key: "notifications.templates.teams",
            displayName: "Teams Notification Templates",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object with Microsoft Teams adaptive card templates per notification type.",
            defaultValue: """{"default":{"cardTitle":"NexTraceOne — {Title}","cardBody":"{Message}","actionUrl":"{ActionUrl}","placeholders":["Title","Message","ActionUrl"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 172),

        // --- Routing & Fallback ---

        ConfigurationDefinition.Create(
            key: "notifications.routing.default_policy",
            displayName: "Default Routing Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object defining default recipient routing policy. Keys: ownerFirst, adminFallback, approverRouting.",
            defaultValue: """{"ownerFirst":true,"adminFallback":true,"approverRouting":false}""",
            uiEditorType: "json-editor",
            sortOrder: 175),

        ConfigurationDefinition.Create(
            key: "notifications.routing.fallback_recipients",
            displayName: "Fallback Recipients",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON array of fallback recipient user IDs or role identifiers when primary routing fails.",
            defaultValue: "[]",
            uiEditorType: "json-editor",
            sortOrder: 176),

        ConfigurationDefinition.Create(
            key: "notifications.routing.by_category",
            displayName: "Routing Rules per Category",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object mapping categories to routing rules.",
            defaultValue: """{"Incident":{"recipientType":"owner","fallbackToAdmin":true},"Approval":{"recipientType":"approver","fallbackToAdmin":true},"Security":{"recipientType":"admin","fallbackToAdmin":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 177),

        ConfigurationDefinition.Create(
            key: "notifications.routing.by_severity",
            displayName: "Routing Rules per Severity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object mapping severities to additional routing behavior.",
            defaultValue: """{"Critical":{"notifyAdmins":true,"broadcastToTeam":true},"Warning":{"notifyAdmins":false,"broadcastToTeam":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 178),

        // --- Preferences, Quiet Hours, Digest & Suppression ---

        ConfigurationDefinition.Create(
            key: "notifications.preferences.default_by_tenant",
            displayName: "Default Notification Preferences by Tenant",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object with default notification preferences applied to all users in a tenant.",
            defaultValue: """{"emailEnabled":true,"teamsEnabled":true,"digestEnabled":false}""",
            uiEditorType: "json-editor",
            sortOrder: 180),

        ConfigurationDefinition.Create(
            key: "notifications.preferences.default_by_role",
            displayName: "Default Notification Preferences by Role",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Role],
            description: "JSON object with default notification preferences per role.",
            defaultValue: "{}",
            uiEditorType: "json-editor",
            sortOrder: 181),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.enabled",
            displayName: "Quiet Hours Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "Controls whether quiet hours are active for deferring non-critical notifications.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 182),

        ConfigurationDefinition.Create(
            key: "notifications.quiet_hours.bypass_categories",
            displayName: "Categories that Bypass Quiet Hours",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "JSON array of notification categories that are never deferred by quiet hours.",
            defaultValue: """["Incident","Security"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 183),

        ConfigurationDefinition.Create(
            key: "notifications.digest.enabled",
            displayName: "Digest Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "Controls whether digest summaries are generated for accumulated notifications.",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 184),

        ConfigurationDefinition.Create(
            key: "notifications.digest.period_hours",
            displayName: "Digest Period (Hours)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.User],
            description: "Period in hours between digest summary generations.",
            defaultValue: "24",
            validationRules: """{"min":1,"max":168}""",
            uiEditorType: "text",
            sortOrder: 185),

        ConfigurationDefinition.Create(
            key: "notifications.digest.eligible_categories",
            displayName: "Digest Eligible Categories",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON array of categories eligible for digest aggregation. Critical categories should NOT be included.",
            defaultValue: """["Informational","Change","Integration","Platform"]""",
            uiEditorType: "json-editor",
            sortOrder: 186),

        ConfigurationDefinition.Create(
            key: "notifications.suppress.enabled",
            displayName: "Suppression Rules Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether automatic suppression of duplicate/acknowledged notifications is active.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 187),

        ConfigurationDefinition.Create(
            key: "notifications.suppress.acknowledged_window_minutes",
            displayName: "Acknowledged Suppression Window (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Time window in minutes during which a notification for the same entity is suppressed after acknowledgment.",
            defaultValue: "30",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 188),

        ConfigurationDefinition.Create(
            key: "notifications.acknowledge.required_categories",
            displayName: "Categories Requiring Acknowledgment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON array of categories where explicit acknowledgment is required before dismissal.",
            defaultValue: """["Incident","Security","Compliance"]""",
            uiEditorType: "json-editor",
            sortOrder: 189),

        // --- Escalation, Dedup & Incident Linkage ---

        ConfigurationDefinition.Create(
            key: "notifications.dedup.enabled",
            displayName: "Deduplication Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether notification deduplication is active.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 190),

        ConfigurationDefinition.Create(
            key: "notifications.dedup.window_minutes",
            displayName: "Deduplication Window (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default time window in minutes for notification deduplication.",
            defaultValue: "5",
            validationRules: """{"min":1,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 191),

        ConfigurationDefinition.Create(
            key: "notifications.dedup.window_by_category",
            displayName: "Deduplication Window per Category",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON object mapping categories to specific deduplication windows in minutes.",
            defaultValue: """{"Incident":10,"Security":10,"Integration":15}""",
            uiEditorType: "json-editor",
            sortOrder: 192),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.enabled",
            displayName: "Escalation Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether unacknowledged notifications are automatically escalated.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 193),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.critical_threshold_minutes",
            displayName: "Critical Escalation Threshold (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minutes before an unacknowledged Critical notification is escalated.",
            defaultValue: "30",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 194),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.action_required_threshold_minutes",
            displayName: "ActionRequired Escalation Threshold (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minutes before an unacknowledged ActionRequired notification is escalated.",
            defaultValue: "120",
            validationRules: """{"min":15,"max":2880}""",
            uiEditorType: "text",
            sortOrder: 195),

        ConfigurationDefinition.Create(
            key: "notifications.escalation.channels",
            displayName: "Escalation Delivery Channels",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON array of channels used when escalating notifications.",
            defaultValue: """["InApp","Email","MicrosoftTeams"]""",
            uiEditorType: "json-editor",
            sortOrder: 196),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.enabled",
            displayName: "Incident Linkage Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether critical notifications can be automatically linked to or create incidents.",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 197),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.auto_create_enabled",
            displayName: "Auto-Create Incident from Notification",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether incidents are automatically created from critical notifications when no matching incident exists.",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 198),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.eligible_types",
            displayName: "Incident Linkage Eligible Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "JSON array of notification types eligible for automatic incident linkage/creation.",
            defaultValue: """["IncidentCreated","IncidentEscalated","HealthDegradation","AnomalyDetected"]""",
            uiEditorType: "json-editor",
            sortOrder: 199),

        ConfigurationDefinition.Create(
            key: "notifications.incident_linkage.correlation_window_minutes",
            displayName: "Incident Correlation Window (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Time window in minutes to correlate notifications with existing incidents.",
            defaultValue: "60",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 200),

        ConfigurationDefinition.Create(
            key: "notifications.grouping.window_minutes",
            displayName: "Notification Grouping Window (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Time window in minutes for grouping related notifications under the same correlation key.",
            defaultValue: "60",
            validationRules: """{"min":5,"max":1440}""",
            uiEditorType: "text",
            sortOrder: 201),

        // ── END PHASE 2 ───────────────────────────────────────────────────

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

        // ── BLOCK B — Instance Configuration ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "instance.name",
            displayName: "Instance Name",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Display name of the platform instance",
            defaultValue: "NexTraceOne",
            uiEditorType: "text",
            sortOrder: 1000),

        ConfigurationDefinition.Create(
            key: "instance.commercial_name",
            displayName: "Instance Commercial Name",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Commercial/marketing name shown to users",
            defaultValue: "NexTraceOne Platform",
            uiEditorType: "text",
            sortOrder: 1010),

        ConfigurationDefinition.Create(
            key: "instance.default_language",
            displayName: "Instance Default Language",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Default language for the platform instance",
            defaultValue: "en",
            validationRules: """{"enum":["en","pt-BR","pt-PT","es"]}""",
            uiEditorType: "select",
            sortOrder: 1020),

        ConfigurationDefinition.Create(
            key: "instance.default_timezone",
            displayName: "Instance Default Timezone",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Default timezone for the instance",
            defaultValue: "UTC",
            uiEditorType: "text",
            sortOrder: 1030),

        ConfigurationDefinition.Create(
            key: "instance.date_format",
            displayName: "Instance Date Format",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Default date format pattern",
            defaultValue: "yyyy-MM-dd",
            uiEditorType: "text",
            sortOrder: 1040),

        ConfigurationDefinition.Create(
            key: "instance.support_url",
            displayName: "Instance Support URL",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "URL for platform support documentation",
            uiEditorType: "text",
            sortOrder: 1050),

        ConfigurationDefinition.Create(
            key: "instance.terms_url",
            displayName: "Instance Terms URL",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "URL for terms of service",
            uiEditorType: "text",
            sortOrder: 1060),

        ConfigurationDefinition.Create(
            key: "instance.privacy_url",
            displayName: "Instance Privacy URL",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "URL for privacy policy",
            uiEditorType: "text",
            sortOrder: 1070),

        // ── BLOCK C — Tenant Configuration ────────────────────────────────

        ConfigurationDefinition.Create(
            key: "tenant.display_name",
            displayName: "Tenant Display Name",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Tenant],
            description: "Custom display name for the tenant",
            uiEditorType: "text",
            sortOrder: 1100),

        ConfigurationDefinition.Create(
            key: "tenant.default_language",
            displayName: "Tenant Default Language",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default language for the tenant",
            defaultValue: "en",
            isInheritable: true,
            validationRules: """{"enum":["en","pt-BR","pt-PT","es"]}""",
            uiEditorType: "select",
            sortOrder: 1110),

        ConfigurationDefinition.Create(
            key: "tenant.default_timezone",
            displayName: "Tenant Default Timezone",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default timezone for the tenant",
            defaultValue: "UTC",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 1120),

        ConfigurationDefinition.Create(
            key: "tenant.contact_email",
            displayName: "Tenant Contact Email",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Tenant],
            description: "Primary contact email for the tenant",
            uiEditorType: "text",
            sortOrder: 1130),

        ConfigurationDefinition.Create(
            key: "tenant.max_users",
            displayName: "Tenant Maximum Users",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of users allowed in the tenant",
            defaultValue: "100",
            validationRules: """{"min":1,"max":10000}""",
            uiEditorType: "text",
            sortOrder: 1140),

        ConfigurationDefinition.Create(
            key: "tenant.max_environments",
            displayName: "Tenant Maximum Environments",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of environments per tenant",
            defaultValue: "10",
            validationRules: """{"min":1,"max":50}""",
            uiEditorType: "text",
            sortOrder: 1150),

        // ── BLOCK D — Environment Configuration ───────────────────────────

        ConfigurationDefinition.Create(
            key: "environment.classification",
            displayName: "Environment Classification",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment],
            description: "Classification/profile of the environment",
            validationRules: """{"enum":["Development","Test","QA","PreProduction","Production","Lab"]}""",
            uiEditorType: "select",
            sortOrder: 1200),

        ConfigurationDefinition.Create(
            key: "environment.is_production",
            displayName: "Environment Is Production",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment],
            description: "Whether this environment is the production environment. Only one environment per tenant should be marked as production.",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 1210),

        ConfigurationDefinition.Create(
            key: "environment.criticality",
            displayName: "Environment Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Criticality level of the environment",
            defaultValue: "medium",
            validationRules: """{"enum":["low","medium","high","critical"]}""",
            uiEditorType: "select",
            sortOrder: 1220),

        ConfigurationDefinition.Create(
            key: "environment.lifecycle_order",
            displayName: "Environment Lifecycle Order",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.Environment],
            description: "Order in the deployment lifecycle pipeline",
            defaultValue: "0",
            validationRules: """{"min":0,"max":100}""",
            uiEditorType: "text",
            sortOrder: 1230),

        ConfigurationDefinition.Create(
            key: "environment.description",
            displayName: "Environment Description",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment],
            description: "Description of the environment purpose",
            uiEditorType: "text",
            sortOrder: 1240),

        ConfigurationDefinition.Create(
            key: "environment.active",
            displayName: "Environment Active",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment],
            description: "Whether the environment is currently active",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1250),

        // ── BLOCK F — Branding & Experience Defaults ──────────────────────

        ConfigurationDefinition.Create(
            key: "branding.logo_url",
            displayName: "Branding Logo URL",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "URL for the organization logo (light variant)",
            uiEditorType: "text",
            sortOrder: 1300),

        ConfigurationDefinition.Create(
            key: "branding.logo_dark_url",
            displayName: "Branding Logo Dark URL",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "URL for the organization logo (dark variant)",
            uiEditorType: "text",
            sortOrder: 1310),

        ConfigurationDefinition.Create(
            key: "branding.accent_color",
            displayName: "Branding Accent Color",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Primary accent color in hex format",
            defaultValue: "#3B82F6",
            validationRules: """{"pattern":"^#[0-9a-fA-F]{6}$"}""",
            uiEditorType: "text",
            sortOrder: 1320),

        ConfigurationDefinition.Create(
            key: "branding.favicon_url",
            displayName: "Branding Favicon URL",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "URL for the custom favicon",
            uiEditorType: "text",
            sortOrder: 1330),

        ConfigurationDefinition.Create(
            key: "branding.welcome_message",
            displayName: "Branding Welcome Message",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Custom welcome message displayed on the dashboard",
            uiEditorType: "text",
            sortOrder: 1340),

        ConfigurationDefinition.Create(
            key: "branding.footer_text",
            displayName: "Branding Footer Text",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Custom footer text displayed on all pages",
            uiEditorType: "text",
            sortOrder: 1350),

        // ── BLOCK G — Feature Flags ───────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "feature.module.catalog.enabled",
            displayName: "Feature: Service Catalog",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Service Catalog module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1400),

        ConfigurationDefinition.Create(
            key: "feature.module.contracts.enabled",
            displayName: "Feature: Contract Governance",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Contract Governance module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1410),

        ConfigurationDefinition.Create(
            key: "feature.module.changes.enabled",
            displayName: "Feature: Change Intelligence",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Change Intelligence module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1420),

        ConfigurationDefinition.Create(
            key: "feature.module.operations.enabled",
            displayName: "Feature: Operations",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Operations module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1430),

        ConfigurationDefinition.Create(
            key: "feature.module.ai.enabled",
            displayName: "Feature: AI Hub",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the AI Hub module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1440),

        ConfigurationDefinition.Create(
            key: "feature.module.governance.enabled",
            displayName: "Feature: Governance",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Governance module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1450),

        ConfigurationDefinition.Create(
            key: "feature.module.finops.enabled",
            displayName: "Feature: FinOps",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the FinOps module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1460),

        ConfigurationDefinition.Create(
            key: "feature.module.integrations.enabled",
            displayName: "Feature: Integration Hub",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Integration Hub module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1470),

        ConfigurationDefinition.Create(
            key: "feature.module.analytics.enabled",
            displayName: "Feature: Product Analytics",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable/disable the Product Analytics module",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1480),

        ConfigurationDefinition.Create(
            key: "feature.preview.ai_agents.enabled",
            displayName: "Preview: AI Agents",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable AI Agents preview feature (beta)",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 1490),

        ConfigurationDefinition.Create(
            key: "feature.preview.environment_comparison.enabled",
            displayName: "Preview: Environment Comparison",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Enable Environment Comparison preview feature",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 1495),

        // ── BLOCK H — Environment Policies ────────────────────────────────

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_automation",
            displayName: "Policy: Allow Automation",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether automated operations are allowed in this environment",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1500),

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_promotion_target",
            displayName: "Policy: Allow Promotion Target",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether this environment can be a promotion target",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1510),

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_promotion_source",
            displayName: "Policy: Allow Promotion Source",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether this environment can be a promotion source",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1520),

        ConfigurationDefinition.Create(
            key: "policy.environment.require_approval_for_changes",
            displayName: "Policy: Require Approval for Changes",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether changes in this environment require approval",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1530),

        ConfigurationDefinition.Create(
            key: "policy.environment.allow_drift_analysis",
            displayName: "Policy: Allow Drift Analysis",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether drift analysis is active for this environment",
            defaultValue: "true",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1540),

        ConfigurationDefinition.Create(
            key: "policy.environment.restrict_sensitive_features",
            displayName: "Policy: Restrict Sensitive Features",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether sensitive features are restricted in this environment",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1550),

        ConfigurationDefinition.Create(
            key: "policy.environment.change_freeze.enabled",
            displayName: "Policy: Change Freeze Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Whether a change freeze is currently active for this environment",
            defaultValue: "false",
            isInheritable: true,
            uiEditorType: "toggle",
            sortOrder: 1560),

        ConfigurationDefinition.Create(
            key: "policy.environment.change_freeze.reason",
            displayName: "Policy: Change Freeze Reason",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Environment, ConfigurationScope.System],
            description: "Reason for the current change freeze",
            isInheritable: true,
            uiEditorType: "text",
            sortOrder: 1570),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 3 — WORKFLOW, APPROVALS & PROMOTION GOVERNANCE
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Workflow Types & Templates ────────────────────────

        ConfigurationDefinition.Create(
            key: "workflow.types.enabled",
            displayName: "Enabled Workflow Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of enabled workflow types (e.g. ReleaseApproval, PromotionApproval, WaiverApproval, GovernanceReview, AccessRequest)",
            defaultValue: """["ReleaseApproval","PromotionApproval","WaiverApproval","GovernanceReview"]""",
            uiEditorType: "json-editor",
            sortOrder: 2000),

        ConfigurationDefinition.Create(
            key: "workflow.templates.default",
            displayName: "Default Workflow Template",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Default workflow template definition with stages, quorum and approver rules",
            defaultValue: """{"name":"Standard Approval","stages":[{"name":"Review","order":1,"requiredApprovals":1,"approvalRule":"SingleApprover"}]}""",
            uiEditorType: "json-editor",
            sortOrder: 2010),

        ConfigurationDefinition.Create(
            key: "workflow.templates.by_change_level",
            displayName: "Workflow Templates by Change Level",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Map of change level to workflow template name (e.g. {\"1\":\"Standard\",\"2\":\"Enhanced\",\"3\":\"FullGovernance\"})",
            defaultValue: """{"1":"Standard","2":"Enhanced","3":"FullGovernance"}""",
            uiEditorType: "json-editor",
            sortOrder: 2020),

        ConfigurationDefinition.Create(
            key: "workflow.templates.active_version",
            displayName: "Active Workflow Template Version",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Currently active version number for the workflow template set",
            defaultValue: "1",
            validationRules: """{"min":1,"max":9999}""",
            uiEditorType: "text",
            sortOrder: 2030),

        // ── Block B — Stages, Sequencing & Quorum ──────────────────────

        ConfigurationDefinition.Create(
            key: "workflow.stages.max_count",
            displayName: "Maximum Workflow Stages",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of stages allowed in a single workflow",
            defaultValue: "10",
            validationRules: """{"min":1,"max":50}""",
            uiEditorType: "text",
            sortOrder: 2100),

        ConfigurationDefinition.Create(
            key: "workflow.stages.allow_parallel",
            displayName: "Allow Parallel Stages",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether parallel stage execution is permitted in workflows",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2110),

        ConfigurationDefinition.Create(
            key: "workflow.quorum.default_rule",
            displayName: "Default Quorum Rule",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Default quorum rule for approval stages (SingleApprover, Majority, Unanimous)",
            defaultValue: "SingleApprover",
            validationRules: """{"enum":["SingleApprover","Majority","Unanimous"]}""",
            uiEditorType: "select",
            sortOrder: 2120),

        ConfigurationDefinition.Create(
            key: "workflow.quorum.minimum_approvers",
            displayName: "Minimum Approvers per Stage",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Minimum number of approvers required per workflow stage",
            defaultValue: "1",
            validationRules: """{"min":1,"max":20}""",
            uiEditorType: "text",
            sortOrder: 2130),

        ConfigurationDefinition.Create(
            key: "workflow.stages.allow_optional",
            displayName: "Allow Optional Stages",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether optional (skippable) stages are permitted in workflows",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2140),

        // ── Block C — Approvers, Fallback & Escalation ─────────────────

        ConfigurationDefinition.Create(
            key: "workflow.approvers.policy",
            displayName: "Approver Assignment Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Policy for resolving approvers (by role, ownership, team, or explicit list)",
            defaultValue: """{"strategy":"ByOwnership","roles":["TechLead","Architect"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2200),

        ConfigurationDefinition.Create(
            key: "workflow.approvers.fallback",
            displayName: "Fallback Approver Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Fallback approvers when primary approver is unavailable",
            defaultValue: """{"enabled":true,"fallbackRoles":["PlatformAdmin"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2210),

        ConfigurationDefinition.Create(
            key: "workflow.approvers.self_approval_allowed",
            displayName: "Self-Approval Allowed",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether the requester can approve their own workflow (separation of duties control)",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2220),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.enabled",
            displayName: "Workflow Escalation Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether automatic escalation is enabled for workflow approvals",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2230),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.delay_minutes",
            displayName: "Escalation Delay (Minutes)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Time in minutes before an unanswered approval is escalated",
            defaultValue: "240",
            validationRules: """{"min":15,"max":10080}""",
            uiEditorType: "text",
            sortOrder: 2240),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.target_roles",
            displayName: "Escalation Target Roles",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Roles to receive escalated approval requests",
            defaultValue: """["PlatformAdmin","Architect"]""",
            uiEditorType: "json-editor",
            sortOrder: 2250),

        ConfigurationDefinition.Create(
            key: "workflow.escalation.by_criticality",
            displayName: "Escalation Policy by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Escalation delay and targets per criticality level (e.g. critical=60min, high=120min)",
            defaultValue: """{"critical":{"delayMinutes":60,"targets":["PlatformAdmin"]},"high":{"delayMinutes":120,"targets":["TechLead"]},"medium":{"delayMinutes":240,"targets":["TechLead"]}}""",
            uiEditorType: "json-editor",
            sortOrder: 2260),

        // ── Block D — SLAs, Deadlines, Timeout & Expiration ────────────

        ConfigurationDefinition.Create(
            key: "workflow.sla.default_hours",
            displayName: "Default Workflow SLA (Hours)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Default SLA in hours for workflow completion",
            defaultValue: "48",
            validationRules: """{"min":1,"max":720}""",
            uiEditorType: "text",
            sortOrder: 2300),

        ConfigurationDefinition.Create(
            key: "workflow.sla.by_type",
            displayName: "Workflow SLA by Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "SLA hours per workflow type (e.g. {\"ReleaseApproval\":24,\"PromotionApproval\":8})",
            defaultValue: """{"ReleaseApproval":24,"PromotionApproval":8,"WaiverApproval":48,"GovernanceReview":72}""",
            uiEditorType: "json-editor",
            sortOrder: 2310),

        ConfigurationDefinition.Create(
            key: "workflow.sla.by_environment",
            displayName: "Workflow SLA by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "SLA overrides by environment classification (e.g. {\"Production\":4,\"PreProduction\":8})",
            defaultValue: """{"Production":4,"PreProduction":8}""",
            uiEditorType: "json-editor",
            sortOrder: 2320),

        ConfigurationDefinition.Create(
            key: "workflow.timeout.approval_hours",
            displayName: "Approval Timeout (Hours)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Hours before an individual approval step times out",
            defaultValue: "72",
            validationRules: """{"min":1,"max":720}""",
            uiEditorType: "text",
            sortOrder: 2330),

        ConfigurationDefinition.Create(
            key: "workflow.expiry.hours",
            displayName: "Workflow Expiry (Hours)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Hours before an entire workflow expires if not completed",
            defaultValue: "168",
            validationRules: """{"min":1,"max":2160}""",
            uiEditorType: "text",
            sortOrder: 2340),

        ConfigurationDefinition.Create(
            key: "workflow.expiry.action",
            displayName: "Expiry Action",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Action when a workflow expires (Cancel, Escalate, Notify)",
            defaultValue: "Cancel",
            validationRules: """{"enum":["Cancel","Escalate","Notify"]}""",
            uiEditorType: "select",
            sortOrder: 2350),

        ConfigurationDefinition.Create(
            key: "workflow.resubmission.allowed",
            displayName: "Re-submission Allowed",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether rejected workflows can be resubmitted after corrections",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2360),

        ConfigurationDefinition.Create(
            key: "workflow.resubmission.max_attempts",
            displayName: "Maximum Re-submission Attempts",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of times a workflow can be resubmitted",
            defaultValue: "3",
            validationRules: """{"min":1,"max":10}""",
            uiEditorType: "text",
            sortOrder: 2370),

        // ── Block E — Gates, Checklists & Auto-Approval ────────────────

        ConfigurationDefinition.Create(
            key: "workflow.gates.enabled",
            displayName: "Gates Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether gate evaluations are enforced in workflows",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2400),

        ConfigurationDefinition.Create(
            key: "workflow.gates.by_environment",
            displayName: "Gates by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Gate requirements per environment (e.g. Production requires all gates, Dev requires none)",
            defaultValue: """{"Production":["SecurityScan","TestCoverage","ApprovalComplete","EvidencePack"],"PreProduction":["TestCoverage","ApprovalComplete"],"Development":[]}""",
            uiEditorType: "json-editor",
            sortOrder: 2410),

        ConfigurationDefinition.Create(
            key: "workflow.gates.by_criticality",
            displayName: "Gates by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Gate requirements per criticality level",
            defaultValue: """{"critical":["SecurityScan","TestCoverage","ApprovalComplete","EvidencePack"],"high":["TestCoverage","ApprovalComplete"],"medium":["ApprovalComplete"],"low":[]}""",
            uiEditorType: "json-editor",
            sortOrder: 2420),

        ConfigurationDefinition.Create(
            key: "workflow.checklist.by_type",
            displayName: "Checklist by Workflow Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Required checklist items per workflow type",
            defaultValue: """{"ReleaseApproval":["ChangeDescriptionReviewed","RiskAssessed","RollbackPlanDefined"],"PromotionApproval":["TargetEnvironmentVerified","DependenciesChecked"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2430),

        ConfigurationDefinition.Create(
            key: "workflow.checklist.by_environment",
            displayName: "Checklist by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional checklist items required per environment",
            defaultValue: """{"Production":["ProductionReadinessConfirmed","MonitoringVerified","RollbackTested"]}""",
            uiEditorType: "json-editor",
            sortOrder: 2440),

        ConfigurationDefinition.Create(
            key: "workflow.auto_approval.enabled",
            displayName: "Auto-Approval Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether automatic approval is permitted for qualifying workflows",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2450),

        ConfigurationDefinition.Create(
            key: "workflow.auto_approval.conditions",
            displayName: "Auto-Approval Conditions",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Conditions that must be met for auto-approval (e.g. changeLevel, environment, allGatesPassed)",
            defaultValue: """{"maxChangeLevel":1,"excludeEnvironments":["Production","PreProduction"],"requireAllGatesPassed":true}""",
            uiEditorType: "json-editor",
            sortOrder: 2460),

        ConfigurationDefinition.Create(
            key: "workflow.auto_approval.blocked_environments",
            displayName: "Auto-Approval Blocked Environments",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Environments where auto-approval is never allowed",
            defaultValue: """["Production"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 2470),

        ConfigurationDefinition.Create(
            key: "workflow.evidence_pack.required",
            displayName: "Evidence Pack Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether an evidence pack is required before workflow completion",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2480),

        ConfigurationDefinition.Create(
            key: "workflow.rejection.require_reason",
            displayName: "Require Rejection Reason",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether a reason must be provided when rejecting a workflow",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2490),

        // ── Block F — Promotion Governance ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "promotion.paths.allowed",
            displayName: "Allowed Promotion Paths",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Allowed source→target environment promotion paths",
            defaultValue: """[{"source":"Development","targets":["Test"]},{"source":"Test","targets":["QA"]},{"source":"QA","targets":["PreProduction"]},{"source":"PreProduction","targets":["Production"]}]""",
            uiEditorType: "json-editor",
            sortOrder: 2500),

        ConfigurationDefinition.Create(
            key: "promotion.production.extra_approvers_required",
            displayName: "Extra Approvers for Production",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional approvers required when promoting to production",
            defaultValue: "1",
            validationRules: """{"min":0,"max":10}""",
            uiEditorType: "text",
            sortOrder: 2510),

        ConfigurationDefinition.Create(
            key: "promotion.production.extra_gates",
            displayName: "Extra Gates for Production Promotion",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional gates required when promoting to production environment",
            defaultValue: """["SecurityScan","ComplianceCheck","PerformanceBaseline"]""",
            uiEditorType: "json-editor",
            sortOrder: 2520),

        ConfigurationDefinition.Create(
            key: "promotion.restrictions.by_criticality",
            displayName: "Promotion Restrictions by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Promotion restrictions based on service/change criticality",
            defaultValue: """{"critical":{"requireAdditionalApprovers":2,"requireEvidencePack":true},"high":{"requireAdditionalApprovers":1,"requireEvidencePack":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 2530),

        ConfigurationDefinition.Create(
            key: "promotion.rollback.recommendation_enabled",
            displayName: "Rollback Recommendation Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether the system recommends rollback procedures during promotion",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 2540),

        // ── Block G — Release Windows & Freeze Policies ────────────────

        ConfigurationDefinition.Create(
            key: "promotion.release_window.enabled",
            displayName: "Release Windows Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether release window restrictions are enforced",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2600),

        ConfigurationDefinition.Create(
            key: "promotion.release_window.schedule",
            displayName: "Release Window Schedule",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Allowed release windows (e.g. weekdays 06:00-18:00 UTC)",
            defaultValue: """{"days":["Monday","Tuesday","Wednesday","Thursday","Friday"],"startTimeUtc":"06:00","endTimeUtc":"18:00"}""",
            uiEditorType: "json-editor",
            sortOrder: 2610),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.enabled",
            displayName: "Freeze Policy Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether freeze windows are enforced for promotions",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 2620),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.windows",
            displayName: "Freeze Windows",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Defined freeze periods where promotions are blocked",
            defaultValue: "[]",
            uiEditorType: "json-editor",
            sortOrder: 2630),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.override_allowed",
            displayName: "Freeze Override Allowed",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "Whether freeze windows can be overridden with justification (system-level control)",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 2640),

        ConfigurationDefinition.Create(
            key: "promotion.freeze.override_roles",
            displayName: "Freeze Override Authorized Roles",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Roles authorized to override freeze windows",
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
            displayName: "Enabled Governance Policies",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of governance policy IDs enabled for evaluation",
            defaultValue: """["SecurityBaseline","ApiVersioning","DocumentationCoverage","TestCoverage","OwnershipRequired"]""",
            uiEditorType: "json-editor",
            sortOrder: 3000),

        ConfigurationDefinition.Create(
            key: "governance.policies.severity",
            displayName: "Policy Severity Map",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Severity level per policy (Critical, High, Medium, Low)",
            defaultValue: """{"SecurityBaseline":"Critical","ApiVersioning":"High","DocumentationCoverage":"Medium","TestCoverage":"High","OwnershipRequired":"Critical"}""",
            uiEditorType: "json-editor",
            sortOrder: 3010),

        ConfigurationDefinition.Create(
            key: "governance.policies.criticality",
            displayName: "Policy Criticality Map",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Criticality classification per policy (Blocking, NonBlocking, Advisory)",
            defaultValue: """{"SecurityBaseline":"Blocking","ApiVersioning":"NonBlocking","DocumentationCoverage":"Advisory","TestCoverage":"NonBlocking","OwnershipRequired":"Blocking"}""",
            uiEditorType: "json-editor",
            sortOrder: 3020),

        ConfigurationDefinition.Create(
            key: "governance.policies.category_map",
            displayName: "Policy Category Map",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Category classification per policy (Security, Quality, Operational, Documentation)",
            defaultValue: """{"SecurityBaseline":"Security","ApiVersioning":"Quality","DocumentationCoverage":"Documentation","TestCoverage":"Quality","OwnershipRequired":"Operational"}""",
            uiEditorType: "json-editor",
            sortOrder: 3030),

        ConfigurationDefinition.Create(
            key: "governance.policies.applicability",
            displayName: "Policy Applicability Rules",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Applicability rules per policy by system type, API type, or service classification",
            defaultValue: """{"SecurityBaseline":{"applies_to":"all"},"ApiVersioning":{"applies_to":["REST","SOAP"]},"TestCoverage":{"applies_to":"all"}}""",
            uiEditorType: "json-editor",
            sortOrder: 3040),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.enabled",
            displayName: "Enabled Compliance Profiles",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Active compliance profiles (e.g. Standard, Enhanced, Strict)",
            defaultValue: """["Standard","Enhanced","Strict"]""",
            uiEditorType: "json-editor",
            sortOrder: 3050),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.default",
            displayName: "Default Compliance Profile",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Default compliance profile applied when no specific binding exists",
            defaultValue: "Standard",
            validationRules: """{"enum":["Standard","Enhanced","Strict"]}""",
            uiEditorType: "select",
            sortOrder: 3060),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.policies_map",
            displayName: "Compliance Profile Policy Map",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Policies included in each compliance profile",
            defaultValue: """{"Standard":["SecurityBaseline","OwnershipRequired"],"Enhanced":["SecurityBaseline","OwnershipRequired","ApiVersioning","TestCoverage"],"Strict":["SecurityBaseline","OwnershipRequired","ApiVersioning","TestCoverage","DocumentationCoverage"]}""",
            uiEditorType: "json-editor",
            sortOrder: 3070),

        ConfigurationDefinition.Create(
            key: "governance.compliance.profiles.by_environment",
            displayName: "Compliance Profile by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Override compliance profile per environment classification",
            defaultValue: """{"Production":"Strict","PreProduction":"Enhanced","Development":"Standard"}""",
            uiEditorType: "json-editor",
            sortOrder: 3080),

        // ── Block B — Evidence Requirements ────────────────────────────

        ConfigurationDefinition.Create(
            key: "governance.evidence.types_accepted",
            displayName: "Accepted Evidence Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of accepted evidence types (e.g. Document, Screenshot, TestReport, ScanResult, AuditLog)",
            defaultValue: """["Document","Screenshot","TestReport","ScanResult","AuditLog","Attestation"]""",
            uiEditorType: "json-editor",
            sortOrder: 3100),

        ConfigurationDefinition.Create(
            key: "governance.evidence.required_by_policy",
            displayName: "Evidence Required by Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Evidence requirements per policy (type, mandatory flag, minimum count)",
            defaultValue: """{"SecurityBaseline":{"mandatory":true,"types":["ScanResult"],"minCount":1},"TestCoverage":{"mandatory":true,"types":["TestReport"],"minCount":1}}""",
            uiEditorType: "json-editor",
            sortOrder: 3110),

        ConfigurationDefinition.Create(
            key: "governance.evidence.expiry_days",
            displayName: "Evidence Default Expiry (Days)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Default number of days before evidence expires and must be renewed",
            defaultValue: "90",
            validationRules: """{"min":1,"max":730}""",
            uiEditorType: "text",
            sortOrder: 3120),

        ConfigurationDefinition.Create(
            key: "governance.evidence.expiry_by_criticality",
            displayName: "Evidence Expiry by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Evidence expiry days per policy criticality level",
            defaultValue: """{"critical":30,"high":60,"medium":90,"low":180}""",
            uiEditorType: "json-editor",
            sortOrder: 3130),

        ConfigurationDefinition.Create(
            key: "governance.evidence.expired_action",
            displayName: "Expired Evidence Action",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Action when evidence expires (Notify, Block, Degrade)",
            defaultValue: "Notify",
            validationRules: """{"enum":["Notify","Block","Degrade"]}""",
            uiEditorType: "select",
            sortOrder: 3140),

        ConfigurationDefinition.Create(
            key: "governance.evidence.required_by_environment",
            displayName: "Evidence Required by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional evidence requirements per environment classification",
            defaultValue: """{"Production":{"mandatory":true,"minCount":1},"PreProduction":{"mandatory":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 3150),

        // ── Block C — Waiver Rules ─────────────────────────────────────

        ConfigurationDefinition.Create(
            key: "governance.waiver.eligible_policies",
            displayName: "Policies Eligible for Waiver",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Policies that can be waived (empty = all non-critical)",
            defaultValue: """["ApiVersioning","DocumentationCoverage","TestCoverage"]""",
            uiEditorType: "json-editor",
            sortOrder: 3200),

        ConfigurationDefinition.Create(
            key: "governance.waiver.blocked_severities",
            displayName: "Waiver Blocked Severities",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Severity levels that cannot be waived (system-level only)",
            defaultValue: """["Critical"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 3210),

        ConfigurationDefinition.Create(
            key: "governance.waiver.validity_days_default",
            displayName: "Default Waiver Validity (Days)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default validity period for waivers in days",
            defaultValue: "30",
            validationRules: """{"min":1,"max":365}""",
            uiEditorType: "text",
            sortOrder: 3220),

        ConfigurationDefinition.Create(
            key: "governance.waiver.validity_days_max",
            displayName: "Maximum Waiver Validity (Days)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum validity period for waivers in days",
            defaultValue: "90",
            validationRules: """{"min":1,"max":365}""",
            uiEditorType: "text",
            sortOrder: 3230),

        ConfigurationDefinition.Create(
            key: "governance.waiver.require_approval",
            displayName: "Waiver Requires Approval",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether waiver requests require explicit approval",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3240),

        ConfigurationDefinition.Create(
            key: "governance.waiver.require_evidence",
            displayName: "Waiver Requires Evidence",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether waiver requests must include supporting evidence/justification",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3250),

        ConfigurationDefinition.Create(
            key: "governance.waiver.allowed_environments",
            displayName: "Waiver Allowed Environments",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Environments where waivers are permitted (empty = all)",
            defaultValue: """["Development","Test","QA"]""",
            uiEditorType: "json-editor",
            sortOrder: 3260),

        ConfigurationDefinition.Create(
            key: "governance.waiver.blocked_environments",
            displayName: "Waiver Blocked Environments",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Environments where waivers are never allowed",
            defaultValue: """["Production"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 3270),

        ConfigurationDefinition.Create(
            key: "governance.waiver.renewal.allowed",
            displayName: "Waiver Renewal Allowed",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether expired waivers can be renewed",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3280),

        ConfigurationDefinition.Create(
            key: "governance.waiver.renewal.max_count",
            displayName: "Maximum Waiver Renewals",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of times a waiver can be renewed",
            defaultValue: "2",
            validationRules: """{"min":0,"max":10}""",
            uiEditorType: "text",
            sortOrder: 3290),

        // ── Block D — Governance Packs & Bindings ──────────────────────

        ConfigurationDefinition.Create(
            key: "governance.packs.enabled",
            displayName: "Enabled Governance Packs",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of enabled governance pack identifiers",
            defaultValue: """["CoreGovernance","ApiGovernance","SecurityHardening"]""",
            uiEditorType: "json-editor",
            sortOrder: 3300),

        ConfigurationDefinition.Create(
            key: "governance.packs.active_version",
            displayName: "Active Governance Pack Version",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Currently active version number for the governance pack set",
            defaultValue: "1",
            validationRules: """{"min":1,"max":9999}""",
            uiEditorType: "text",
            sortOrder: 3310),

        ConfigurationDefinition.Create(
            key: "governance.packs.binding_policy",
            displayName: "Pack Binding Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default binding rules for governance packs (by tenant, environment, domain, team, system type)",
            defaultValue: """{"bindBy":["tenant","environment","systemType"],"precedence":"most_specific_wins"}""",
            uiEditorType: "json-editor",
            sortOrder: 3320),

        ConfigurationDefinition.Create(
            key: "governance.packs.by_environment",
            displayName: "Governance Packs by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Governance pack bindings per environment classification",
            defaultValue: """{"Production":["CoreGovernance","SecurityHardening"],"PreProduction":["CoreGovernance"],"Development":["CoreGovernance"]}""",
            uiEditorType: "json-editor",
            sortOrder: 3330),

        ConfigurationDefinition.Create(
            key: "governance.packs.by_system_type",
            displayName: "Governance Packs by System Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Governance pack bindings per system type (REST API, SOAP, Event, Background)",
            defaultValue: """{"REST":["ApiGovernance","CoreGovernance"],"SOAP":["ApiGovernance","CoreGovernance"],"Event":["CoreGovernance"],"Background":["CoreGovernance"]}""",
            uiEditorType: "json-editor",
            sortOrder: 3340),

        ConfigurationDefinition.Create(
            key: "governance.packs.overlap_resolution",
            displayName: "Pack Overlap Resolution Strategy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "How to resolve policy conflicts between overlapping packs (MostRestrictive, MostSpecific, Merge)",
            defaultValue: "MostRestrictive",
            validationRules: """{"enum":["MostRestrictive","MostSpecific","Merge"]}""",
            uiEditorType: "select",
            sortOrder: 3350),

        // ── Block E — Scorecards, Thresholds & Risk Matrix ─────────────

        ConfigurationDefinition.Create(
            key: "governance.scorecard.enabled",
            displayName: "Governance Scorecard Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether governance scorecards are active and calculated",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 3400),

        ConfigurationDefinition.Create(
            key: "governance.scorecard.thresholds",
            displayName: "Scorecard Score Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Score thresholds for governance scorecard classification (e.g. Excellent ≥90, Good ≥70, etc.)",
            defaultValue: """{"Excellent":90,"Good":70,"Fair":50,"Poor":0}""",
            uiEditorType: "json-editor",
            sortOrder: 3410),

        ConfigurationDefinition.Create(
            key: "governance.scorecard.weights",
            displayName: "Scorecard Category Weights",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Weight of each governance category in the overall score (must sum to 100)",
            defaultValue: """{"Security":30,"Quality":25,"Operational":25,"Documentation":20}""",
            uiEditorType: "json-editor",
            sortOrder: 3420),

        ConfigurationDefinition.Create(
            key: "governance.scorecard.thresholds_by_environment",
            displayName: "Scorecard Thresholds by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Override score thresholds per environment (e.g. Production requires higher scores)",
            defaultValue: """{"Production":{"Excellent":95,"Good":80,"Fair":60,"Poor":0},"Development":{"Excellent":80,"Good":60,"Fair":40,"Poor":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 3430),

        ConfigurationDefinition.Create(
            key: "governance.risk.matrix",
            displayName: "Risk Matrix Definition",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Risk matrix mapping likelihood x impact to risk level (Critical, High, Medium, Low)",
            defaultValue: """{"High_High":"Critical","High_Medium":"High","High_Low":"Medium","Medium_High":"High","Medium_Medium":"Medium","Medium_Low":"Low","Low_High":"Medium","Low_Medium":"Low","Low_Low":"Low"}""",
            uiEditorType: "json-editor",
            sortOrder: 3440),

        ConfigurationDefinition.Create(
            key: "governance.risk.thresholds",
            displayName: "Risk Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Numeric risk score thresholds for classification",
            defaultValue: """{"Critical":90,"High":70,"Medium":40,"Low":0}""",
            uiEditorType: "json-editor",
            sortOrder: 3450),

        ConfigurationDefinition.Create(
            key: "governance.risk.labels",
            displayName: "Risk Classification Labels",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Display labels and colors for risk classification levels",
            defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626"},"High":{"label":"High","color":"#F59E0B"},"Medium":{"label":"Medium","color":"#3B82F6"},"Low":{"label":"Low","color":"#10B981"}}""",
            uiEditorType: "json-editor",
            sortOrder: 3460),

        ConfigurationDefinition.Create(
            key: "governance.risk.thresholds_by_criticality",
            displayName: "Risk Thresholds by Service Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Override risk thresholds per service criticality level",
            defaultValue: """{"critical":{"Critical":80,"High":60,"Medium":30,"Low":0},"standard":{"Critical":90,"High":70,"Medium":40,"Low":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 3470),

        // ── Block F — Minimum Requirements by System/API Type ──────────

        ConfigurationDefinition.Create(
            key: "governance.requirements.by_system_type",
            displayName: "Minimum Requirements by System Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum governance requirements per system type (mandatory policies, evidence, packs)",
            defaultValue: """{"REST":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"mandatoryPack":"ApiGovernance","minScore":70},"SOAP":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"mandatoryPack":"ApiGovernance","minScore":70},"Event":{"mandatoryPolicies":["SecurityBaseline"],"mandatoryPack":"CoreGovernance","minScore":60},"Background":{"mandatoryPolicies":["SecurityBaseline"],"mandatoryPack":"CoreGovernance","minScore":50}}""",
            uiEditorType: "json-editor",
            sortOrder: 3500),

        ConfigurationDefinition.Create(
            key: "governance.requirements.by_api_type",
            displayName: "Minimum Requirements by API Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum governance requirements per API classification (Public, Internal, Partner)",
            defaultValue: """{"Public":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning","DocumentationCoverage"],"minScore":80},"Internal":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning"],"minScore":60},"Partner":{"mandatoryPolicies":["SecurityBaseline","ApiVersioning","DocumentationCoverage"],"minScore":75}}""",
            uiEditorType: "json-editor",
            sortOrder: 3510),

        ConfigurationDefinition.Create(
            key: "governance.requirements.mandatory_evidence_by_classification",
            displayName: "Mandatory Evidence by Classification",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Evidence requirements per system classification (critical, standard)",
            defaultValue: """{"critical":{"types":["ScanResult","TestReport","Attestation"],"minCount":2},"standard":{"types":["ScanResult"],"minCount":1}}""",
            uiEditorType: "json-editor",
            sortOrder: 3520),

        ConfigurationDefinition.Create(
            key: "governance.requirements.min_compliance_profile",
            displayName: "Minimum Compliance Profile by Classification",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum compliance profile required per service classification",
            defaultValue: """{"critical":"Strict","standard":"Standard"}""",
            uiEditorType: "json-editor",
            sortOrder: 3530),

        ConfigurationDefinition.Create(
            key: "governance.requirements.promotion_gates",
            displayName: "Governance Promotion Gates",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Minimum governance gates required for promotion (links to Phase 3 workflow gates)",
            defaultValue: """{"Production":{"minScore":70,"requiredProfile":"Enhanced","allBlockingPoliciesMet":true},"PreProduction":{"minScore":50,"allBlockingPoliciesMet":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 3540),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 5 — CATALOG, CONTRACTS, APIS & CHANGE GOVERNANCE
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Contract Types, Versioning & Breaking Change ──────

        ConfigurationDefinition.Create(
            key: "catalog.contract.types_enabled",
            displayName: "Enabled Contract Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Contract types supported and enabled (REST, SOAP, GraphQL, gRPC, AsyncAPI, Event, SharedSchema)",
            defaultValue: """["REST","SOAP","GraphQL","gRPC","AsyncAPI","Event","SharedSchema"]""",
            uiEditorType: "json-editor",
            sortOrder: 4000),

        ConfigurationDefinition.Create(
            key: "catalog.contract.api_types_enabled",
            displayName: "Enabled API Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "API classification types enabled (Public, Internal, Partner, ThirdParty)",
            defaultValue: """["Public","Internal","Partner","ThirdParty"]""",
            uiEditorType: "json-editor",
            sortOrder: 4010),

        ConfigurationDefinition.Create(
            key: "catalog.contract.versioning_policy",
            displayName: "Contract Versioning Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Versioning strategy per contract type (SemVer, CalVer, Sequential, Header-based)",
            defaultValue: """{"REST":"SemVer","SOAP":"Sequential","GraphQL":"SemVer","gRPC":"SemVer","AsyncAPI":"SemVer","Event":"SemVer","SharedSchema":"SemVer"}""",
            uiEditorType: "json-editor",
            sortOrder: 4020),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_change_policy",
            displayName: "Breaking Change Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Behavior on breaking change detection per type (Block, Warn, RequireApproval, Allow)",
            defaultValue: """{"REST":"RequireApproval","SOAP":"Block","GraphQL":"RequireApproval","gRPC":"RequireApproval","AsyncAPI":"Warn","Event":"Warn","SharedSchema":"Block"}""",
            uiEditorType: "json-editor",
            sortOrder: 4030),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_change_severity",
            displayName: "Breaking Change Default Severity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default severity for detected breaking changes (Critical, High, Medium, Low)",
            defaultValue: "High",
            validationRules: """{"enum":["Critical","High","Medium","Low"]}""",
            uiEditorType: "select",
            sortOrder: 4040),

        ConfigurationDefinition.Create(
            key: "catalog.contract.version_increment_rules",
            displayName: "Version Increment Rules",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Rules for automatic version increment (breaking=major, feature=minor, fix=patch)",
            defaultValue: """{"breakingChange":"major","newFeature":"minor","bugfix":"patch","documentation":"patch"}""",
            uiEditorType: "json-editor",
            sortOrder: 4050),

        ConfigurationDefinition.Create(
            key: "catalog.contract.breaking_promotion_restriction",
            displayName: "Breaking Change Promotion Restriction",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Block promotion to production when unresolved breaking changes exist",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4060),

        // ── Block B — Validation, Linting, Rulesets & Templates ────────

        ConfigurationDefinition.Create(
            key: "catalog.validation.lint_severity_defaults",
            displayName: "Lint Severity Defaults",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default lint severity levels (error, warn, info, off) for validation rules",
            defaultValue: """{"missingDescription":"warn","missingExample":"info","unusedSchema":"warn","invalidReference":"error","securitySchemeUndefined":"error"}""",
            uiEditorType: "json-editor",
            sortOrder: 4100),

        ConfigurationDefinition.Create(
            key: "catalog.validation.rulesets_by_contract_type",
            displayName: "Rulesets by Contract Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Validation ruleset bindings per contract type",
            defaultValue: """{"REST":["openapi-standard","security-best-practices"],"SOAP":["wsdl-compliance"],"GraphQL":["graphql-best-practices"],"gRPC":["protobuf-lint"],"AsyncAPI":["asyncapi-standard"],"Event":["event-schema-validation"],"SharedSchema":["schema-consistency"]}""",
            uiEditorType: "json-editor",
            sortOrder: 4110),

        ConfigurationDefinition.Create(
            key: "catalog.validation.blocking_vs_warning",
            displayName: "Validation Blocking vs Warning Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Which validation rules block publication vs only warn",
            defaultValue: """{"blocking":["invalidReference","securitySchemeUndefined"],"warning":["missingDescription","missingExample","unusedSchema"]}""",
            uiEditorType: "json-editor",
            sortOrder: 4120),

        ConfigurationDefinition.Create(
            key: "catalog.validation.min_validations_by_type",
            displayName: "Minimum Validations by Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum validation requirements per contract type before publication",
            defaultValue: """{"REST":{"schemaValid":true,"securityDefined":true,"pathsDocumented":true},"SOAP":{"wsdlValid":true},"GraphQL":{"schemaValid":true},"gRPC":{"protoValid":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4130),

        ConfigurationDefinition.Create(
            key: "catalog.templates.by_contract_type",
            displayName: "Contract Templates by Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default templates to use when creating new contracts per type",
            defaultValue: """{"REST":"openapi-3.1-standard","SOAP":"wsdl-2.0-standard","GraphQL":"graphql-standard","gRPC":"protobuf-standard","AsyncAPI":"asyncapi-2.6-standard","Event":"cloudevents-standard","SharedSchema":"json-schema-standard"}""",
            uiEditorType: "json-editor",
            sortOrder: 4140),

        ConfigurationDefinition.Create(
            key: "catalog.templates.metadata_defaults",
            displayName: "Contract Metadata Defaults",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default metadata fields pre-filled in new contract drafts",
            defaultValue: """{"license":"Proprietary","termsOfService":"","contact":"","externalDocs":""}""",
            uiEditorType: "json-editor",
            sortOrder: 4150),

        // ── Block C — Minimum Requirements & Publication ────────────────

        ConfigurationDefinition.Create(
            key: "catalog.requirements.owner_required",
            displayName: "Owner Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether a service/contract must have an assigned owner before publication",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4200),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.changelog_required",
            displayName: "Changelog Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether a changelog entry is required for publication",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4210),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.glossary_required",
            displayName: "Glossary Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether a glossary/term definitions section is required",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 4220),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.use_cases_required",
            displayName: "Use Cases Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether documented use cases are required before publication",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 4230),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.min_documentation",
            displayName: "Minimum Documentation Requirements",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum documentation requirements for publication (description, examples, etc.)",
            defaultValue: """{"descriptionMinLength":20,"operationDescriptions":true,"responseExamples":false,"errorDocumentation":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4240),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.min_catalog_fields",
            displayName: "Minimum Catalog Fields",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum required fields for a service catalog entry",
            defaultValue: """{"name":true,"description":true,"owner":true,"team":true,"domain":false,"tier":false,"lifecycle":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4250),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.min_contract_fields",
            displayName: "Minimum Contract Fields",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum required fields for a contract to be considered complete",
            defaultValue: """{"title":true,"version":true,"description":true,"servers":true,"securityScheme":true,"contact":false}""",
            uiEditorType: "json-editor",
            sortOrder: 4260),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.by_contract_type",
            displayName: "Requirements by Contract Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional minimum requirements per contract type",
            defaultValue: """{"REST":{"securityScheme":true,"pathDescriptions":true},"SOAP":{"wsdlValid":true},"GraphQL":{"schemaValid":true},"gRPC":{"protoValid":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4270),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.by_environment",
            displayName: "Requirements by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional requirements for specific environments (e.g. Production stricter)",
            defaultValue: """{"Production":{"ownerRequired":true,"changelogRequired":true,"minDocumentation":true,"allBlockingValidationsPass":true},"Development":{"ownerRequired":false,"changelogRequired":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4280),

        ConfigurationDefinition.Create(
            key: "catalog.requirements.by_criticality",
            displayName: "Requirements by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional requirements based on service/API criticality",
            defaultValue: """{"critical":{"ownerRequired":true,"changelogRequired":true,"glossaryRequired":true,"useCasesRequired":true},"standard":{"ownerRequired":true,"changelogRequired":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4290),

        // ── Block D — Publication & Promotion Policy ───────────────────

        ConfigurationDefinition.Create(
            key: "catalog.publication.pre_publish_review",
            displayName: "Pre-Publication Review Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether contracts require review/approval before publication",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4300),

        ConfigurationDefinition.Create(
            key: "catalog.publication.visibility_defaults",
            displayName: "Publication Visibility Defaults",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default visibility settings for newly published contracts",
            defaultValue: """{"Internal":"team","Public":"organization","Partner":"restricted"}""",
            uiEditorType: "json-editor",
            sortOrder: 4310),

        ConfigurationDefinition.Create(
            key: "catalog.publication.portal_defaults",
            displayName: "Developer Portal Publishing Defaults",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default settings for developer portal publication",
            defaultValue: """{"autoPublishToPortal":true,"includeExamples":true,"includeChangelog":true,"includeTryItOut":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4320),

        ConfigurationDefinition.Create(
            key: "catalog.publication.promotion_readiness",
            displayName: "Contract Promotion Readiness Criteria",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Criteria a contract must meet to be promoted to next environment",
            defaultValue: """{"allBlockingValidationsPass":true,"ownerAssigned":true,"changelogUpdated":true,"noUnresolvedBreakingChanges":true,"minGovernanceScore":60}""",
            uiEditorType: "json-editor",
            sortOrder: 4330),

        ConfigurationDefinition.Create(
            key: "catalog.publication.gating_by_environment",
            displayName: "Publication Gating by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Publication gating rules per environment",
            defaultValue: """{"Production":{"requireApproval":true,"requireAllGatesPass":true},"PreProduction":{"requireApproval":false,"requireAllGatesPass":true},"Development":{"requireApproval":false,"requireAllGatesPass":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4340),

        // ── Block E — Import/Export Policy ──────────────────────────────

        ConfigurationDefinition.Create(
            key: "catalog.import.types_allowed",
            displayName: "Allowed Import Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Contract types that can be imported (file upload, URL, Git sync)",
            defaultValue: """{"fileUpload":["OpenAPI","WSDL","GraphQL","Protobuf","AsyncAPI","JSONSchema"],"urlImport":["OpenAPI","AsyncAPI"],"gitSync":["OpenAPI","AsyncAPI","Protobuf"]}""",
            uiEditorType: "json-editor",
            sortOrder: 4400),

        ConfigurationDefinition.Create(
            key: "catalog.export.types_allowed",
            displayName: "Allowed Export Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Export formats allowed for contracts",
            defaultValue: """["OpenAPI-JSON","OpenAPI-YAML","WSDL","GraphQL-SDL","Protobuf","AsyncAPI-YAML","Markdown","HTML"]""",
            uiEditorType: "json-editor",
            sortOrder: 4410),

        ConfigurationDefinition.Create(
            key: "catalog.import.overwrite_policy",
            displayName: "Import Overwrite Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Behavior when importing a contract that already exists (Merge, Overwrite, Block, AskUser)",
            defaultValue: "AskUser",
            validationRules: """{"enum":["Merge","Overwrite","Block","AskUser"]}""",
            uiEditorType: "select",
            sortOrder: 4420),

        ConfigurationDefinition.Create(
            key: "catalog.import.validation_on_import",
            displayName: "Validate on Import",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether imported contracts are automatically validated against rulesets",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4430),

        // ── Block F — Change Types, Criticality & Blast Radius ─────────

        ConfigurationDefinition.Create(
            key: "change.types_enabled",
            displayName: "Enabled Change Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Types of changes supported (Feature, Bugfix, Hotfix, Refactor, Config, Infrastructure, Rollback)",
            defaultValue: """["Feature","Bugfix","Hotfix","Refactor","Config","Infrastructure","Rollback"]""",
            uiEditorType: "json-editor",
            sortOrder: 4500),

        ConfigurationDefinition.Create(
            key: "change.criticality_defaults",
            displayName: "Change Criticality Defaults",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default criticality level per change type (Critical, High, Medium, Low)",
            defaultValue: """{"Feature":"Medium","Bugfix":"Medium","Hotfix":"Critical","Refactor":"Low","Config":"Low","Infrastructure":"High","Rollback":"Critical"}""",
            uiEditorType: "json-editor",
            sortOrder: 4510),

        ConfigurationDefinition.Create(
            key: "change.risk_classification",
            displayName: "Change Risk Classification",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Risk classification rules by change type",
            defaultValue: """{"Hotfix":{"baseRisk":"High","requiresApproval":true},"Infrastructure":{"baseRisk":"High","requiresApproval":true},"Feature":{"baseRisk":"Medium","requiresApproval":false},"Rollback":{"baseRisk":"High","requiresApproval":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4520),

        ConfigurationDefinition.Create(
            key: "change.blast_radius.thresholds",
            displayName: "Blast Radius Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Blast radius score thresholds for classification (Critical, High, Medium, Low)",
            defaultValue: """{"Critical":90,"High":70,"Medium":40,"Low":0}""",
            uiEditorType: "json-editor",
            sortOrder: 4530),

        ConfigurationDefinition.Create(
            key: "change.blast_radius.categories",
            displayName: "Blast Radius Categories",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Blast radius category definitions with labels and colors",
            defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626","action":"RequireApproval"},"High":{"label":"High","color":"#F59E0B","action":"RequireReview"},"Medium":{"label":"Medium","color":"#3B82F6","action":"Notify"},"Low":{"label":"Low","color":"#10B981","action":"AutoApprove"}}""",
            uiEditorType: "json-editor",
            sortOrder: 4540),

        ConfigurationDefinition.Create(
            key: "change.blast_radius.environment_weights",
            displayName: "Blast Radius Environment Weights",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Impact weight multiplier per environment for blast radius calculation",
            defaultValue: """{"Production":1.0,"PreProduction":0.6,"Staging":0.4,"Development":0.2}""",
            uiEditorType: "json-editor",
            sortOrder: 4550),

        ConfigurationDefinition.Create(
            key: "change.severity_criteria",
            displayName: "Change Severity Criteria",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Criteria for determining change severity based on affected services, dependencies, etc.",
            defaultValue: """{"affectedServicesHigh":5,"affectedDependenciesHigh":10,"crossDomainChange":true,"dataSchemaChange":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4560),

        // ── Block G — Release Scoring, Evidence Pack & Rollback ────────

        ConfigurationDefinition.Create(
            key: "change.release_score.weights",
            displayName: "Release Confidence Score Weights",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Weights for each factor in the release confidence/change score calculation (must sum to 100)",
            defaultValue: """{"testCoverage":20,"codeReview":15,"blastRadius":20,"historicalSuccess":15,"documentationComplete":10,"governanceCompliance":10,"evidencePack":10}""",
            uiEditorType: "json-editor",
            sortOrder: 4600),

        ConfigurationDefinition.Create(
            key: "change.release_score.thresholds",
            displayName: "Release Score Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Release confidence score thresholds (HighConfidence, Moderate, LowConfidence, Block)",
            defaultValue: """{"HighConfidence":80,"Moderate":60,"LowConfidence":40,"Block":0}""",
            uiEditorType: "json-editor",
            sortOrder: 4610),

        ConfigurationDefinition.Create(
            key: "change.evidence_pack.required",
            displayName: "Evidence Pack Required",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether an evidence pack is required for releases",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4620),

        ConfigurationDefinition.Create(
            key: "change.evidence_pack.requirements",
            displayName: "Evidence Pack Requirements",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Minimum evidence pack requirements by environment and change type",
            defaultValue: """{"Production":{"testReport":true,"securityScan":true,"approvalRecord":true,"rollbackPlan":true},"PreProduction":{"testReport":true,"securityScan":false,"approvalRecord":false},"Development":{"testReport":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4630),

        ConfigurationDefinition.Create(
            key: "change.evidence_pack.by_criticality",
            displayName: "Evidence Pack Requirements by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Additional evidence pack requirements based on change criticality",
            defaultValue: """{"Critical":{"securityScan":true,"approvalRecord":true,"rollbackPlan":true,"impactAnalysis":true},"High":{"securityScan":true,"approvalRecord":true,"rollbackPlan":true},"Medium":{"testReport":true},"Low":{}}""",
            uiEditorType: "json-editor",
            sortOrder: 4640),

        ConfigurationDefinition.Create(
            key: "change.rollback.recommendation_policy",
            displayName: "Rollback Recommendation Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Policy for when rollback is recommended based on score, incidents and risk",
            defaultValue: """{"autoRecommendOnScoreBelow":40,"autoRecommendOnIncidentCorrelation":true,"requireRollbackPlanForProduction":true,"requireRollbackPlanForCriticalChanges":true}""",
            uiEditorType: "json-editor",
            sortOrder: 4650),

        ConfigurationDefinition.Create(
            key: "change.release_calendar.window_policy",
            displayName: "Release Calendar Window Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Release window constraints by change type (links to Phase 3 release windows)",
            defaultValue: """{"Hotfix":{"allowOutsideWindow":true,"requireApproval":true},"Feature":{"allowOutsideWindow":false,"requireApproval":false},"Infrastructure":{"allowOutsideWindow":false,"requireApproval":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 4660),

        ConfigurationDefinition.Create(
            key: "change.release_calendar.by_environment",
            displayName: "Release Calendar by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Release calendar restrictions per environment",
            defaultValue: """{"Production":{"allowedDays":["Monday","Tuesday","Wednesday","Thursday"],"blockedHours":{"start":"18:00","end":"08:00"},"requireWindow":true},"PreProduction":{"requireWindow":false},"Development":{"requireWindow":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 4670),

        ConfigurationDefinition.Create(
            key: "change.incident_correlation.enabled",
            displayName: "Release-to-Incident Correlation Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether release-to-incident correlation analysis is active",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 4680),

        ConfigurationDefinition.Create(
            key: "change.incident_correlation.window_hours",
            displayName: "Incident Correlation Window (Hours)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Time window in hours after a release to correlate with incidents",
            defaultValue: "24",
            validationRules: """{"min":1,"max":168}""",
            uiEditorType: "text",
            sortOrder: 4690),

        // ═══════════════════════════════════════════════════════════════════
        // PHASE 6 — OPERATIONS, INCIDENTS, FINOPS & BENCHMARKING
        // ═══════════════════════════════════════════════════════════════════

        // ── Block A — Incident Taxonomy, Severity, Criticality & SLA ───

        ConfigurationDefinition.Create(
            key: "incidents.taxonomy.categories",
            displayName: "Incident Categories",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Defined incident categories (Infrastructure, Application, Security, Data, Network, ThirdParty)",
            defaultValue: """["Infrastructure","Application","Security","Data","Network","ThirdParty"]""",
            uiEditorType: "json-editor",
            sortOrder: 5000),

        ConfigurationDefinition.Create(
            key: "incidents.taxonomy.types",
            displayName: "Incident Types",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Defined incident types (Outage, Degradation, Latency, ErrorSpike, SecurityBreach, DataLoss, ConfigDrift)",
            defaultValue: """["Outage","Degradation","Latency","ErrorSpike","SecurityBreach","DataLoss","ConfigDrift"]""",
            uiEditorType: "json-editor",
            sortOrder: 5010),

        ConfigurationDefinition.Create(
            key: "incidents.severity.defaults_by_type",
            displayName: "Default Severity by Incident Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default severity assigned per incident type (Critical, High, Medium, Low)",
            defaultValue: """{"Outage":"Critical","Degradation":"High","Latency":"Medium","ErrorSpike":"High","SecurityBreach":"Critical","DataLoss":"Critical","ConfigDrift":"Low"}""",
            uiEditorType: "json-editor",
            sortOrder: 5020),

        ConfigurationDefinition.Create(
            key: "incidents.severity.defaults_by_category",
            displayName: "Default Severity by Incident Category",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default severity assigned per incident category",
            defaultValue: """{"Infrastructure":"High","Application":"Medium","Security":"Critical","Data":"High","Network":"High","ThirdParty":"Medium"}""",
            uiEditorType: "json-editor",
            sortOrder: 5030),

        ConfigurationDefinition.Create(
            key: "incidents.criticality.defaults",
            displayName: "Incident Criticality Defaults",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default criticality per incident type/category combination",
            defaultValue: """{"Outage_Infrastructure":"Critical","SecurityBreach_Security":"Critical","Degradation_Application":"High","Latency_Network":"Medium","ConfigDrift_Application":"Low"}""",
            uiEditorType: "json-editor",
            sortOrder: 5040),

        ConfigurationDefinition.Create(
            key: "incidents.severity.mapping",
            displayName: "Severity Mapping Table",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Severity mapping with labels, colors and numeric weights",
            defaultValue: """{"Critical":{"label":"Critical","color":"#DC2626","weight":4},"High":{"label":"High","color":"#F59E0B","weight":3},"Medium":{"label":"Medium","color":"#3B82F6","weight":2},"Low":{"label":"Low","color":"#10B981","weight":1}}""",
            uiEditorType: "json-editor",
            sortOrder: 5050),

        ConfigurationDefinition.Create(
            key: "incidents.sla.by_severity",
            displayName: "SLA by Severity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "SLA targets in minutes by severity (acknowledgement, firstResponse, resolution)",
            defaultValue: """{"Critical":{"acknowledgementMinutes":5,"firstResponseMinutes":15,"resolutionMinutes":240},"High":{"acknowledgementMinutes":15,"firstResponseMinutes":60,"resolutionMinutes":480},"Medium":{"acknowledgementMinutes":60,"firstResponseMinutes":240,"resolutionMinutes":1440},"Low":{"acknowledgementMinutes":240,"firstResponseMinutes":480,"resolutionMinutes":4320}}""",
            uiEditorType: "json-editor",
            sortOrder: 5060),

        ConfigurationDefinition.Create(
            key: "incidents.sla.by_environment",
            displayName: "SLA Adjustments by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "SLA multiplier or override per environment (Production stricter)",
            defaultValue: """{"Production":{"multiplier":1.0},"PreProduction":{"multiplier":2.0},"Staging":{"multiplier":3.0},"Development":{"multiplier":5.0}}""",
            uiEditorType: "json-editor",
            sortOrder: 5070),

        ConfigurationDefinition.Create(
            key: "incidents.sla.production_behavior",
            displayName: "Production Severity Behavior",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Special SLA behavior for production environment by severity",
            defaultValue: """{"Critical":{"autoEscalate":true,"pageOnCall":true,"requirePostMortem":true},"High":{"autoEscalate":true,"pageOnCall":false,"requirePostMortem":true},"Medium":{"autoEscalate":false},"Low":{"autoEscalate":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 5080),

        // ── Block B — Owners, Classification, Correlation & Auto-Incident ─

        ConfigurationDefinition.Create(
            key: "incidents.owner.default_by_category",
            displayName: "Default Owner by Category",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default operational owner (team/role) per incident category",
            defaultValue: """{"Infrastructure":"platform-ops","Application":"service-owner","Security":"security-team","Data":"data-engineering","Network":"network-ops","ThirdParty":"vendor-management"}""",
            uiEditorType: "json-editor",
            sortOrder: 5100),

        ConfigurationDefinition.Create(
            key: "incidents.owner.fallback",
            displayName: "Fallback Incident Owner",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Fallback owner when no specific owner can be determined",
            defaultValue: "platform-admin",
            uiEditorType: "text",
            sortOrder: 5110),

        ConfigurationDefinition.Create(
            key: "incidents.classification.auto_enabled",
            displayName: "Automatic Classification Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether incidents are automatically classified based on alerts and context",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5120),

        ConfigurationDefinition.Create(
            key: "incidents.correlation.policy",
            displayName: "Alert-to-Incident Correlation Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Rules for correlating alerts and notifications into incidents",
            defaultValue: """{"correlateByService":true,"correlateByEnvironment":true,"correlateBySeverity":false,"correlationWindowMinutes":30,"correlationKeyFields":["service","environment","alertType"]}""",
            uiEditorType: "json-editor",
            sortOrder: 5130),

        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.enabled",
            displayName: "Auto-Incident Creation Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether incidents can be automatically created from alerts",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5140),

        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.policy",
            displayName: "Auto-Incident Creation Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Rules governing automatic incident creation (thresholds, conditions, limits)",
            defaultValue: """{"minSeverityForAutoCreate":"High","maxAutoIncidentsPerHour":10,"requireCorrelationMatch":true,"blockedCategories":[]}""",
            uiEditorType: "json-editor",
            sortOrder: 5150),

        ConfigurationDefinition.Create(
            key: "incidents.auto_creation.blocked_environments",
            displayName: "Auto-Incident Blocked Environments",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Environments where auto-incident creation is blocked",
            defaultValue: """[]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 5160),

        ConfigurationDefinition.Create(
            key: "incidents.enrichment.enabled",
            displayName: "Incident Enrichment Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether existing incidents are enriched with new correlated alerts",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5170),

        // ── Block C — Playbooks, Runbooks & Operational Automation ─────

        ConfigurationDefinition.Create(
            key: "operations.playbook.defaults_by_type",
            displayName: "Default Playbook by Incident Type",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default playbook identifier per incident type",
            defaultValue: """{"Outage":"playbook-outage-standard","Degradation":"playbook-degradation-triage","SecurityBreach":"playbook-security-response","DataLoss":"playbook-data-recovery","Latency":"playbook-performance-investigation"}""",
            uiEditorType: "json-editor",
            sortOrder: 5200),

        ConfigurationDefinition.Create(
            key: "operations.runbook.defaults_by_category",
            displayName: "Default Runbook by Category",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default runbook identifier per incident category",
            defaultValue: """{"Infrastructure":"runbook-infra-ops","Application":"runbook-app-debug","Security":"runbook-sec-incident","Network":"runbook-network-diag"}""",
            uiEditorType: "json-editor",
            sortOrder: 5210),

        ConfigurationDefinition.Create(
            key: "operations.playbook.required_by_environment",
            displayName: "Playbook Required by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether a playbook is required per environment for incident response",
            defaultValue: """{"Production":true,"PreProduction":false,"Development":false}""",
            uiEditorType: "json-editor",
            sortOrder: 5220),

        ConfigurationDefinition.Create(
            key: "operations.playbook.required_by_criticality",
            displayName: "Playbook Required by Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether a playbook is mandatory for Critical severity incidents",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5230),

        ConfigurationDefinition.Create(
            key: "operations.automation.enabled_by_environment",
            displayName: "Automation Enabled by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Operational automation enablement per environment",
            defaultValue: """{"Production":{"autoRestart":false,"autoScale":false,"autoRemediate":false},"PreProduction":{"autoRestart":true,"autoScale":true,"autoRemediate":false},"Development":{"autoRestart":true,"autoScale":true,"autoRemediate":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5240),

        ConfigurationDefinition.Create(
            key: "operations.automation.blocked_in_production",
            displayName: "Automation Blocked in Production",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Automations explicitly blocked in production environments",
            defaultValue: """["autoRemediate","autoDeleteResources","autoModifyData"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 5250),

        ConfigurationDefinition.Create(
            key: "operations.automation.by_severity",
            displayName: "Automation Allowed by Severity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Which automations are allowed per incident severity",
            defaultValue: """{"Critical":["autoNotify","autoEscalate"],"High":["autoNotify","autoEscalate","autoRestart"],"Medium":["autoNotify","autoRestart"],"Low":["autoNotify"]}""",
            uiEditorType: "json-editor",
            sortOrder: 5260),

        ConfigurationDefinition.Create(
            key: "operations.postincident.template_enabled",
            displayName: "Post-Incident Template Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether post-incident review template is automatically applied",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5270),

        // ── Block D — FinOps Budgets & Thresholds ──────────────────────

        ConfigurationDefinition.Create(
            key: "finops.budget.default_currency",
            displayName: "Default Budget Currency",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default currency for FinOps budgets (ISO 4217)",
            defaultValue: "USD",
            validationRules: """{"maxLength":3,"minLength":3}""",
            uiEditorType: "text",
            sortOrder: 5300),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_tenant",
            displayName: "Budget by Tenant",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Monthly budget allocation per tenant",
            defaultValue: """{"default":{"monthlyBudget":10000,"alertOnExceed":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5310),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_team",
            displayName: "Budget by Team",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Monthly budget allocation per team",
            defaultValue: """{"default":{"monthlyBudget":5000,"alertOnExceed":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5320),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_service",
            displayName: "Budget by Service",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Monthly budget allocation per service",
            defaultValue: """{"default":{"monthlyBudget":2000,"alertOnExceed":true}}""",
            uiEditorType: "json-editor",
            sortOrder: 5330),

        ConfigurationDefinition.Create(
            key: "finops.budget.alert_thresholds",
            displayName: "Budget Alert Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Alert thresholds as percentage of budget (ordered ascending)",
            defaultValue: """[{"percent":80,"severity":"Low","action":"Notify"},{"percent":90,"severity":"Medium","action":"Notify"},{"percent":100,"severity":"High","action":"NotifyAndBlock"},{"percent":120,"severity":"Critical","action":"Escalate"}]""",
            uiEditorType: "json-editor",
            sortOrder: 5340),

        ConfigurationDefinition.Create(
            key: "finops.budget.by_environment",
            displayName: "Budget by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Budget constraints per environment",
            defaultValue: """{"Production":{"monthlyBudget":8000,"hardLimit":true},"PreProduction":{"monthlyBudget":3000,"hardLimit":false},"Development":{"monthlyBudget":1000,"hardLimit":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 5350),

        ConfigurationDefinition.Create(
            key: "finops.budget.periodicity",
            displayName: "Budget Periodicity",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Budget period (Monthly, Quarterly, Yearly)",
            defaultValue: "Monthly",
            validationRules: """{"enum":["Monthly","Quarterly","Yearly"]}""",
            uiEditorType: "select",
            sortOrder: 5360),

        ConfigurationDefinition.Create(
            key: "finops.budget.rollover_enabled",
            displayName: "Budget Rollover Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether unused budget rolls over to the next period",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 5370),

        // ── Block E — Anomaly, Waste & Financial Recommendations ───────

        ConfigurationDefinition.Create(
            key: "finops.anomaly.detection_enabled",
            displayName: "Cost Anomaly Detection Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether cost anomaly detection is active",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5400),

        ConfigurationDefinition.Create(
            key: "finops.anomaly.thresholds",
            displayName: "Anomaly Detection Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Thresholds for anomaly detection (deviation percentage from baseline)",
            defaultValue: """{"warning":20,"high":50,"critical":100}""",
            uiEditorType: "json-editor",
            sortOrder: 5410),

        ConfigurationDefinition.Create(
            key: "finops.anomaly.comparison_window_days",
            displayName: "Anomaly Comparison Window (Days)",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Number of days for the baseline comparison window",
            defaultValue: "30",
            validationRules: """{"min":7,"max":90}""",
            uiEditorType: "text",
            sortOrder: 5420),

        ConfigurationDefinition.Create(
            key: "finops.waste.detection_enabled",
            displayName: "Waste Detection Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether waste detection analysis is active",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5430),

        ConfigurationDefinition.Create(
            key: "finops.waste.thresholds",
            displayName: "Waste Detection Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Thresholds for waste detection (idle percentage, underutilization)",
            defaultValue: """{"idleResourcePercent":90,"underutilizationPercent":20,"unusedDaysThreshold":14}""",
            uiEditorType: "json-editor",
            sortOrder: 5440),

        ConfigurationDefinition.Create(
            key: "finops.waste.categories",
            displayName: "Waste Categories",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Categories of waste for classification",
            defaultValue: """["IdleResources","Overprovisioned","UnattachedStorage","UnusedLicenses","OrphanedResources","OverlappingServices"]""",
            uiEditorType: "json-editor",
            sortOrder: 5450),

        ConfigurationDefinition.Create(
            key: "finops.recommendation.policy",
            displayName: "Financial Recommendation Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Policy governing financial recommendations and savings suggestions",
            defaultValue: """{"autoRecommend":true,"minSavingsThreshold":50,"showInDashboard":true,"notifyOnHighSavings":true,"highSavingsThreshold":500}""",
            uiEditorType: "json-editor",
            sortOrder: 5460),

        ConfigurationDefinition.Create(
            key: "finops.notification.policy",
            displayName: "Financial Notification Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Notification policy for FinOps events (anomalies, budget breaches, waste)",
            defaultValue: """{"notifyOnAnomaly":true,"notifyOnBudgetBreach":true,"notifyOnWasteDetected":true,"notifyOnRecommendation":false,"digestFrequency":"Weekly"}""",
            uiEditorType: "json-editor",
            sortOrder: 5470),

        ConfigurationDefinition.Create(
            key: "finops.anomaly.by_criticality",
            displayName: "Anomaly Policy by Service Criticality",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Anomaly detection sensitivity per service criticality level",
            defaultValue: """{"critical":{"warningDeviation":10,"autoEscalate":true},"standard":{"warningDeviation":20,"autoEscalate":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 5480),

        // ── Block F — Benchmarking Weights, Thresholds & Formulas ──────

        ConfigurationDefinition.Create(
            key: "benchmarking.score.weights",
            displayName: "Benchmarking Score Weights",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Weights for each benchmarking dimension (must sum to 100)",
            defaultValue: """{"reliability":25,"performance":20,"costEfficiency":20,"security":15,"operationalExcellence":10,"documentation":10}""",
            uiEditorType: "json-editor",
            sortOrder: 5500),

        ConfigurationDefinition.Create(
            key: "benchmarking.score.thresholds",
            displayName: "Benchmarking Score Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Score classification thresholds (Excellent, Good, Needs Improvement, Critical)",
            defaultValue: """{"Excellent":90,"Good":70,"NeedsImprovement":50,"Critical":0}""",
            uiEditorType: "json-editor",
            sortOrder: 5510),

        ConfigurationDefinition.Create(
            key: "benchmarking.score.bands",
            displayName: "Benchmarking Score Bands",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Score bands with labels and colors for visualization",
            defaultValue: """{"Excellent":{"label":"Excellent","color":"#10B981","minScore":90},"Good":{"label":"Good","color":"#3B82F6","minScore":70},"NeedsImprovement":{"label":"Needs Improvement","color":"#F59E0B","minScore":50},"Critical":{"label":"Critical","color":"#DC2626","minScore":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 5520),

        ConfigurationDefinition.Create(
            key: "benchmarking.formula.components",
            displayName: "Benchmarking Formula Components",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Configurable components of the benchmarking score formula",
            defaultValue: """{"reliability":{"uptimeWeight":0.5,"mttrWeight":0.3,"incidentRateWeight":0.2},"performance":{"p99LatencyWeight":0.4,"throughputWeight":0.3,"errorRateWeight":0.3},"costEfficiency":{"budgetAdherenceWeight":0.5,"wasteReductionWeight":0.3,"optimizationAdoptionWeight":0.2}}""",
            uiEditorType: "json-editor",
            sortOrder: 5530),

        ConfigurationDefinition.Create(
            key: "benchmarking.score.by_dimension",
            displayName: "Score Weights by Dimension",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Detailed weight distribution within each benchmarking dimension",
            defaultValue: """{"reliability":{"uptime":50,"mttr":30,"incidentRate":20},"performance":{"latency":40,"throughput":30,"errorRate":30},"costEfficiency":{"budgetAdherence":50,"waste":30,"optimization":20},"security":{"vulnerabilities":40,"compliance":30,"patchCurrency":30},"operationalExcellence":{"automation":40,"documentation":30,"changeSuccess":30},"documentation":{"coverage":50,"freshness":30,"quality":20}}""",
            uiEditorType: "json-editor",
            sortOrder: 5540),

        ConfigurationDefinition.Create(
            key: "benchmarking.thresholds.by_environment",
            displayName: "Benchmarking Thresholds by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Override benchmarking thresholds per environment",
            defaultValue: """{"Production":{"Excellent":95,"Good":80,"NeedsImprovement":60,"Critical":0},"Development":{"Excellent":80,"Good":60,"NeedsImprovement":40,"Critical":0}}""",
            uiEditorType: "json-editor",
            sortOrder: 5550),

        ConfigurationDefinition.Create(
            key: "benchmarking.missing_data.policy",
            displayName: "Missing Data Calculation Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "How to handle missing data in benchmarking (SkipDimension, UseDefault, Penalize)",
            defaultValue: "UseDefault",
            validationRules: """{"enum":["SkipDimension","UseDefault","Penalize"]}""",
            uiEditorType: "select",
            sortOrder: 5560),

        ConfigurationDefinition.Create(
            key: "benchmarking.missing_data.default_score",
            displayName: "Missing Data Default Score",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default score to use when data is missing (used with UseDefault policy)",
            defaultValue: "50",
            validationRules: """{"min":0,"max":100}""",
            uiEditorType: "text",
            sortOrder: 5570),

        // ── Block G — Functional Health/Anomaly/Drift Thresholds ───────

        ConfigurationDefinition.Create(
            key: "operations.health.anomaly_thresholds",
            displayName: "Operational Health Anomaly Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Functional thresholds for operational health anomaly detection",
            defaultValue: """{"errorRateWarning":1.0,"errorRateCritical":5.0,"latencyP99Warning":500,"latencyP99Critical":2000,"availabilityWarning":99.5,"availabilityCritical":99.0}""",
            uiEditorType: "json-editor",
            sortOrder: 5600),

        ConfigurationDefinition.Create(
            key: "operations.health.drift_detection_enabled",
            displayName: "Configuration Drift Detection Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether configuration drift detection between environments is active",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 5610),

        ConfigurationDefinition.Create(
            key: "operations.health.drift_thresholds",
            displayName: "Drift Detection Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Thresholds for drift severity classification",
            defaultValue: """{"minor":{"maxDriftedConfigs":5},"major":{"maxDriftedConfigs":15},"critical":{"maxDriftedConfigs":30}}""",
            uiEditorType: "json-editor",
            sortOrder: 5620),

        // ══════════════════════════════════════════════════════════════════
        // ██  PHASE 7 — AI & INTEGRATIONS PARAMETERIZATION               ██
        // ══════════════════════════════════════════════════════════════════

        // ── Block A — AI Provider & Model Enablement ───────────────────

        ConfigurationDefinition.Create(
            key: "ai.providers.enabled",
            displayName: "Enabled AI Providers",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of AI providers enabled for use in the platform",
            defaultValue: """["OpenAI","AzureOpenAI","Internal"]""",
            uiEditorType: "json-editor",
            sortOrder: 6000),

        ConfigurationDefinition.Create(
            key: "ai.models.enabled",
            displayName: "Enabled AI Models",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of AI models enabled for use in the platform",
            defaultValue: """["gpt-4o","gpt-4o-mini","gpt-3.5-turbo","internal-llm"]""",
            uiEditorType: "json-editor",
            sortOrder: 6010),

        ConfigurationDefinition.Create(
            key: "ai.providers.default_by_capability",
            displayName: "Default Provider by Capability",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default AI provider mapped to each product capability",
            defaultValue: """{"chat":"OpenAI","analysis":"AzureOpenAI","classification":"Internal","draftGeneration":"OpenAI","retrievalAugmented":"AzureOpenAI","codeReview":"OpenAI"}""",
            uiEditorType: "json-editor",
            sortOrder: 6020),

        ConfigurationDefinition.Create(
            key: "ai.models.default_by_capability",
            displayName: "Default Model by Capability",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default AI model mapped to each product capability",
            defaultValue: """{"chat":"gpt-4o","analysis":"gpt-4o","classification":"internal-llm","draftGeneration":"gpt-4o","retrievalAugmented":"gpt-4o","codeReview":"gpt-4o-mini"}""",
            uiEditorType: "json-editor",
            sortOrder: 6030),

        ConfigurationDefinition.Create(
            key: "ai.providers.fallback_order",
            displayName: "Provider Fallback Order",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Ordered fallback list of providers when primary is unavailable",
            defaultValue: """["AzureOpenAI","OpenAI","Internal"]""",
            uiEditorType: "json-editor",
            sortOrder: 6040),

        ConfigurationDefinition.Create(
            key: "ai.usage.allow_external",
            displayName: "Allow External AI Usage",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Whether external AI providers are allowed (false = internal only)",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6050),

        ConfigurationDefinition.Create(
            key: "ai.usage.blocked_environments",
            displayName: "AI Blocked Environments",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Environments where external AI usage is permanently blocked",
            defaultValue: """[]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 6060),

        ConfigurationDefinition.Create(
            key: "ai.usage.internal_only_capabilities",
            displayName: "Internal-Only AI Capabilities",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Capabilities that must only use internal AI provider",
            defaultValue: """["classification"]""",
            uiEditorType: "json-editor",
            sortOrder: 6070),

        // ── Block B — AI Budgets, Quotas & Usage Policies ──────────────

        ConfigurationDefinition.Create(
            key: "ai.budget.by_user",
            displayName: "AI Token Budget by User",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default monthly token budget per user",
            defaultValue: """{"monthlyTokens":100000,"alertOnExceed":true}""",
            uiEditorType: "json-editor",
            sortOrder: 6100),

        ConfigurationDefinition.Create(
            key: "ai.budget.by_team",
            displayName: "AI Token Budget by Team",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default monthly token budget per team",
            defaultValue: """{"monthlyTokens":500000,"alertOnExceed":true}""",
            uiEditorType: "json-editor",
            sortOrder: 6110),

        ConfigurationDefinition.Create(
            key: "ai.budget.by_tenant",
            displayName: "AI Token Budget by Tenant",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default monthly token budget per tenant",
            defaultValue: """{"monthlyTokens":2000000,"alertOnExceed":true,"hardLimit":false}""",
            uiEditorType: "json-editor",
            sortOrder: 6120),

        ConfigurationDefinition.Create(
            key: "ai.quota.by_capability",
            displayName: "AI Quota by Capability",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Token quota limits per capability per time window",
            defaultValue: """{"chat":{"dailyTokens":50000},"analysis":{"dailyTokens":100000},"draftGeneration":{"dailyTokens":30000},"retrievalAugmented":{"dailyTokens":80000}}""",
            uiEditorType: "json-editor",
            sortOrder: 6130),

        ConfigurationDefinition.Create(
            key: "ai.usage.limits_by_environment",
            displayName: "AI Usage Limits by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Token limits per environment to control non-production usage",
            defaultValue: """{"Production":{"dailyTokens":500000},"PreProduction":{"dailyTokens":100000},"Development":{"dailyTokens":50000}}""",
            uiEditorType: "json-editor",
            sortOrder: 6140),

        ConfigurationDefinition.Create(
            key: "ai.budget.exceed_policy",
            displayName: "Budget Exceed Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Behavior when AI budget is exceeded (Warn, Block, Throttle)",
            defaultValue: "Warn",
            validationRules: """{"enum":["Warn","Block","Throttle"]}""",
            uiEditorType: "select",
            sortOrder: 6150),

        ConfigurationDefinition.Create(
            key: "ai.budget.warning_thresholds",
            displayName: "AI Budget Warning Thresholds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Percentage thresholds for AI budget warnings",
            defaultValue: """[{"percent":70,"severity":"Low"},{"percent":85,"severity":"Medium"},{"percent":95,"severity":"High"},{"percent":100,"severity":"Critical"}]""",
            uiEditorType: "json-editor",
            sortOrder: 6160),

        // ── Block C — Retention, Audit, Prompts & Retrieval ────────────

        ConfigurationDefinition.Create(
            key: "ai.retention.conversation_days",
            displayName: "AI Conversation Retention Days",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Number of days to retain AI conversation history",
            defaultValue: "90",
            validationRules: """{"min":1,"max":365}""",
            uiEditorType: "text",
            sortOrder: 6200),

        ConfigurationDefinition.Create(
            key: "ai.retention.artifact_days",
            displayName: "AI Artifact Retention Days",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Number of days to retain AI-generated artifacts",
            defaultValue: "180",
            validationRules: """{"min":1,"max":730}""",
            uiEditorType: "text",
            sortOrder: 6210),

        ConfigurationDefinition.Create(
            key: "ai.audit.level",
            displayName: "AI Audit Level",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Level of AI usage auditing (Minimal, Standard, Full)",
            defaultValue: "Standard",
            validationRules: """{"enum":["Minimal","Standard","Full"]}""",
            uiEditorType: "select",
            sortOrder: 6220),

        ConfigurationDefinition.Create(
            key: "ai.audit.log_prompts",
            displayName: "Log AI Prompts",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether to log full prompts sent to AI providers for audit",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 6230),

        ConfigurationDefinition.Create(
            key: "ai.audit.log_responses",
            displayName: "Log AI Responses",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether to log full AI responses for audit",
            defaultValue: "false",
            uiEditorType: "toggle",
            sortOrder: 6240),

        ConfigurationDefinition.Create(
            key: "ai.prompts.base_by_capability",
            displayName: "Base Prompts by Capability",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Base system prompts for each AI capability",
            defaultValue: """{"chat":"You are NexTraceOne AI Assistant, a helpful operational intelligence assistant.","analysis":"You are an expert operational analyst. Analyze the data provided and give actionable insights.","classification":"Classify the following operational event into the appropriate category and severity.","draftGeneration":"Generate a professional draft based on the provided context and requirements."}""",
            uiEditorType: "json-editor",
            sortOrder: 6250),

        ConfigurationDefinition.Create(
            key: "ai.prompts.allow_tenant_override",
            displayName: "Allow Tenant Prompt Override",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            description: "Whether tenants can override base prompts",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 6260),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.top_k",
            displayName: "Retrieval Top-K",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Number of top documents to retrieve for RAG",
            defaultValue: "5",
            validationRules: """{"min":1,"max":50}""",
            uiEditorType: "text",
            sortOrder: 6270),

        ConfigurationDefinition.Create(
            key: "ai.defaults.temperature",
            displayName: "Default Temperature",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default temperature for AI model inference (0.0-2.0)",
            defaultValue: "0.7",
            validationRules: """{"min":0.0,"max":2.0}""",
            uiEditorType: "text",
            sortOrder: 6280),

        ConfigurationDefinition.Create(
            key: "ai.defaults.max_tokens",
            displayName: "Default Max Tokens",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default maximum tokens per AI request",
            defaultValue: "4096",
            validationRules: """{"min":100,"max":128000}""",
            uiEditorType: "text",
            sortOrder: 6290),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.similarity_threshold",
            displayName: "Retrieval Similarity Threshold",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Decimal,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Minimum similarity score for document retrieval (0.0-1.0)",
            defaultValue: "0.7",
            validationRules: """{"min":0.0,"max":1.0}""",
            uiEditorType: "text",
            sortOrder: 6300),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.source_allowlist",
            displayName: "Retrieval Source Allowlist",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Allowed sources for document retrieval (empty = all allowed)",
            defaultValue: """[]""",
            uiEditorType: "json-editor",
            sortOrder: 6310),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.source_denylist",
            displayName: "Retrieval Source Denylist",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Denied sources for document retrieval",
            defaultValue: """[]""",
            uiEditorType: "json-editor",
            sortOrder: 6320),

        ConfigurationDefinition.Create(
            key: "ai.retrieval.context_by_environment",
            displayName: "Context Sources by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Which context sources are available per environment",
            defaultValue: """{"Production":{"telemetry":true,"documents":true,"incidents":true},"Development":{"telemetry":true,"documents":true,"incidents":false}}""",
            uiEditorType: "json-editor",
            sortOrder: 6330),

        // ── Block D — Connector Enablement, Schedules, Retries & Timeouts ──

        ConfigurationDefinition.Create(
            key: "integrations.connectors.enabled",
            displayName: "Enabled Connectors",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "List of connectors enabled in the platform",
            defaultValue: """["AzureDevOps","GitHub","Jira","ServiceNow","PagerDuty","Datadog","Prometheus"]""",
            uiEditorType: "json-editor",
            sortOrder: 6400),

        ConfigurationDefinition.Create(
            key: "integrations.connectors.enabled_by_environment",
            displayName: "Connectors Enabled by Environment",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Per-environment connector enablement overrides",
            defaultValue: """{"Production":["AzureDevOps","GitHub","ServiceNow","PagerDuty","Datadog","Prometheus"],"Development":["AzureDevOps","GitHub","Jira"]}""",
            uiEditorType: "json-editor",
            sortOrder: 6410),

        ConfigurationDefinition.Create(
            key: "integrations.schedule.default",
            displayName: "Default Sync Schedule",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default cron-like sync schedule for connectors",
            defaultValue: "0 */6 * * *",
            uiEditorType: "text",
            sortOrder: 6420),

        ConfigurationDefinition.Create(
            key: "integrations.schedule.by_connector",
            displayName: "Sync Schedule by Connector",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Custom sync schedule per connector type",
            defaultValue: """{"AzureDevOps":"0 */4 * * *","GitHub":"0 */4 * * *","Jira":"0 */6 * * *","ServiceNow":"0 */2 * * *","PagerDuty":"*/30 * * * *","Datadog":"*/15 * * * *","Prometheus":"*/5 * * * *"}""",
            uiEditorType: "json-editor",
            sortOrder: 6430),

        ConfigurationDefinition.Create(
            key: "integrations.retry.max_attempts",
            displayName: "Max Retry Attempts",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of retry attempts for failed integrations",
            defaultValue: "3",
            validationRules: """{"min":0,"max":10}""",
            uiEditorType: "text",
            sortOrder: 6440),

        ConfigurationDefinition.Create(
            key: "integrations.retry.backoff_seconds",
            displayName: "Retry Backoff Seconds",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Base backoff interval in seconds between retries",
            defaultValue: "30",
            validationRules: """{"min":5,"max":600}""",
            uiEditorType: "text",
            sortOrder: 6450),

        ConfigurationDefinition.Create(
            key: "integrations.retry.exponential_backoff",
            displayName: "Exponential Backoff Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether to use exponential backoff for retry intervals",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6460),

        ConfigurationDefinition.Create(
            key: "integrations.timeout.default_seconds",
            displayName: "Default Integration Timeout",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default timeout in seconds for integration operations",
            defaultValue: "120",
            validationRules: """{"min":10,"max":3600}""",
            uiEditorType: "text",
            sortOrder: 6470),

        ConfigurationDefinition.Create(
            key: "integrations.timeout.by_connector",
            displayName: "Timeout by Connector",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Custom timeout in seconds per connector type",
            defaultValue: """{"AzureDevOps":180,"GitHub":120,"Jira":120,"ServiceNow":180,"PagerDuty":60,"Datadog":90,"Prometheus":60}""",
            uiEditorType: "json-editor",
            sortOrder: 6480),

        ConfigurationDefinition.Create(
            key: "integrations.execution.max_concurrent",
            displayName: "Max Concurrent Executions",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Maximum number of concurrent integration executions",
            defaultValue: "5",
            validationRules: """{"min":1,"max":20}""",
            uiEditorType: "text",
            sortOrder: 6490),

        // ── Block E — Filters, Mappings, Import/Export & Sync Policy ───

        ConfigurationDefinition.Create(
            key: "integrations.sync.filter_policy",
            displayName: "Sync Filter Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default filters applied to sync operations",
            defaultValue: """{"excludeArchived":true,"excludeDeleted":true,"maxAgeHours":720}""",
            uiEditorType: "json-editor",
            sortOrder: 6500),

        ConfigurationDefinition.Create(
            key: "integrations.sync.mapping_policy",
            displayName: "Sync Mapping Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default field mapping rules for sync operations",
            defaultValue: """{"autoMapByName":true,"strictTypeValidation":true,"unmappedFieldAction":"Ignore"}""",
            uiEditorType: "json-editor",
            sortOrder: 6510),

        ConfigurationDefinition.Create(
            key: "integrations.import.policy",
            displayName: "Import Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default import behavior and validation rules",
            defaultValue: """{"allowOverwrite":false,"requireValidation":true,"onConflict":"Skip","maxBatchSize":1000}""",
            uiEditorType: "json-editor",
            sortOrder: 6520),

        ConfigurationDefinition.Create(
            key: "integrations.export.policy",
            displayName: "Export Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default export behavior and format rules",
            defaultValue: """{"includeMetadata":true,"defaultFormat":"JSON","maxRecords":10000,"sanitizeSensitive":true}""",
            uiEditorType: "json-editor",
            sortOrder: 6530),

        ConfigurationDefinition.Create(
            key: "integrations.sync.overwrite_behavior",
            displayName: "Sync Overwrite Behavior",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Default behavior when sync detects existing data (Overwrite, Merge, Skip)",
            defaultValue: "Merge",
            validationRules: """{"enum":["Overwrite","Merge","Skip"]}""",
            uiEditorType: "select",
            sortOrder: 6540),

        ConfigurationDefinition.Create(
            key: "integrations.sync.pre_validation_enabled",
            displayName: "Pre-Sync Validation Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether to validate data before sync operations",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6550),

        ConfigurationDefinition.Create(
            key: "integrations.freshness.staleness_threshold_hours",
            displayName: "Staleness Threshold Hours",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Hours after which ingested data is considered stale",
            defaultValue: "24",
            validationRules: """{"min":1,"max":168}""",
            uiEditorType: "text",
            sortOrder: 6560),

        ConfigurationDefinition.Create(
            key: "integrations.freshness.by_connector",
            displayName: "Freshness Thresholds by Connector",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Custom staleness threshold in hours per connector",
            defaultValue: """{"AzureDevOps":12,"GitHub":12,"Jira":24,"ServiceNow":6,"PagerDuty":1,"Datadog":1,"Prometheus":1}""",
            uiEditorType: "json-editor",
            sortOrder: 6570),

        // ── Block F — Failure Reaction, Notification & Governance ───────

        ConfigurationDefinition.Create(
            key: "integrations.failure.notification_policy",
            displayName: "Integration Failure Notification Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Notification rules for integration failures",
            defaultValue: """{"notifyOnFirstFailure":true,"notifyOnConsecutiveFailures":3,"notifyOnAuthFailure":true,"notifyOnStaleness":true,"digestFrequency":"Hourly"}""",
            uiEditorType: "json-editor",
            sortOrder: 6600),

        ConfigurationDefinition.Create(
            key: "integrations.failure.severity_mapping",
            displayName: "Failure Severity Mapping",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Severity levels for different integration failure types",
            defaultValue: """{"authFailure":"Critical","syncFailure":"High","timeoutFailure":"Medium","validationFailure":"Low","staleData":"Medium"}""",
            uiEditorType: "json-editor",
            sortOrder: 6610),

        ConfigurationDefinition.Create(
            key: "integrations.failure.escalation_policy",
            displayName: "Failure Escalation Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Escalation rules based on failure severity and duration",
            defaultValue: """{"Critical":{"escalateAfterMinutes":15,"recipient":"platform-admin"},"High":{"escalateAfterMinutes":60,"recipient":"integration-owner"},"Medium":{"escalateAfterMinutes":240},"Low":{"escalateAfterMinutes":1440}}""",
            uiEditorType: "json-editor",
            sortOrder: 6620),

        ConfigurationDefinition.Create(
            key: "integrations.failure.auto_disable_enabled",
            displayName: "Auto-Disable on Failure Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Whether to auto-disable connectors after repeated failures",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 6630),

        ConfigurationDefinition.Create(
            key: "integrations.failure.auto_disable_threshold",
            displayName: "Auto-Disable Failure Threshold",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Integer,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Number of consecutive failures before auto-disabling a connector",
            defaultValue: "5",
            validationRules: """{"min":2,"max":50}""",
            uiEditorType: "text",
            sortOrder: 6640),

        ConfigurationDefinition.Create(
            key: "integrations.failure.auth_reaction_policy",
            displayName: "Auth Failure Reaction Policy",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Reaction when connector authentication fails",
            defaultValue: """{"pauseSync":true,"notifyOwner":true,"autoRetryAfterMinutes":60,"maxAuthRetries":3}""",
            uiEditorType: "json-editor",
            sortOrder: 6650),

        ConfigurationDefinition.Create(
            key: "integrations.owner.fallback_recipient",
            displayName: "Integration Fallback Owner",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Fallback owner/recipient for integration notifications",
            defaultValue: "platform-admin",
            uiEditorType: "text",
            sortOrder: 6660),

        ConfigurationDefinition.Create(
            key: "integrations.governance.blocked_in_production",
            displayName: "Integration Operations Blocked in Production",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Json,
            allowedScopes: [ConfigurationScope.System],
            description: "Integration operations permanently blocked in production",
            defaultValue: """["bulkDelete","schemaOverwrite","forceReSync"]""",
            isInheritable: false,
            uiEditorType: "json-editor",
            sortOrder: 6670),
    ];
}
