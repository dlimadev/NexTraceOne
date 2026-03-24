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
    ];
}
