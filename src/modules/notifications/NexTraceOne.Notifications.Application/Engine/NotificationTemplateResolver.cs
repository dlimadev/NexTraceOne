using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Engine;

/// <summary>
/// Implementação do resolver de templates internos de notificação.
/// Constrói título, mensagem, categoria, severidade e flag de ação a partir do tipo de evento
/// e de parâmetros contextuais.
/// Templates são i18n-friendly: os valores podem ser externalizados no futuro.
/// </summary>
public sealed class NotificationTemplateResolver : Abstractions.INotificationTemplateResolver
{
    /// <inheritdoc/>
    public Abstractions.ResolvedNotificationTemplate Resolve(
        string eventType,
        IReadOnlyDictionary<string, string> parameters)
    {
        return eventType switch
        {
            NotificationType.IncidentCreated => BuildIncidentCreated(parameters),
            NotificationType.IncidentEscalated => BuildIncidentEscalated(parameters),
            NotificationType.ApprovalPending => BuildApprovalPending(parameters),
            NotificationType.ApprovalApproved => BuildApprovalApproved(parameters),
            NotificationType.ApprovalRejected => BuildApprovalRejected(parameters),
            NotificationType.BreakGlassActivated => BuildBreakGlassActivated(parameters),
            NotificationType.JitAccessPending => BuildJitAccessPending(parameters),
            NotificationType.ComplianceCheckFailed => BuildComplianceCheckFailed(parameters),
            NotificationType.BudgetExceeded => BuildBudgetExceeded(parameters),
            NotificationType.IntegrationFailed => BuildIntegrationFailed(parameters),
            NotificationType.AiProviderUnavailable => BuildAiProviderUnavailable(parameters),
            _ => BuildGeneric(eventType, parameters)
        };
    }

    private static Abstractions.ResolvedNotificationTemplate BuildIncidentCreated(IReadOnlyDictionary<string, string> p)
    {
        var service = Get(p, "ServiceName", "Unknown service");
        var severity = Get(p, "IncidentSeverity", "Unknown");
        return new(
            Title: $"Incident created — {service}",
            Message: $"A new incident with severity {severity} has been created for service {service}. Investigate and take action.",
            Category: NotificationCategory.Incident,
            Severity: NotificationSeverity.Critical,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildIncidentEscalated(IReadOnlyDictionary<string, string> p)
    {
        var service = Get(p, "ServiceName", "Unknown service");
        return new(
            Title: $"Incident escalated — {service}",
            Message: $"An incident for service {service} has been escalated due to severity or unresolved status. Immediate attention required.",
            Category: NotificationCategory.Incident,
            Severity: NotificationSeverity.Critical,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildApprovalPending(IReadOnlyDictionary<string, string> p)
    {
        var entity = Get(p, "EntityName", "workflow");
        var requestedBy = Get(p, "RequestedBy", "a team member");
        return new(
            Title: $"Approval required — {entity}",
            Message: $"A new approval has been requested by {requestedBy} for {entity}. Review and decide.",
            Category: NotificationCategory.Approval,
            Severity: NotificationSeverity.ActionRequired,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildApprovalApproved(IReadOnlyDictionary<string, string> p)
    {
        var entity = Get(p, "EntityName", "workflow");
        var approvedBy = Get(p, "ApprovedBy", "an approver");
        return new(
            Title: $"Approved — {entity}",
            Message: $"The approval for {entity} has been approved by {approvedBy}.",
            Category: NotificationCategory.Approval,
            Severity: NotificationSeverity.Info,
            RequiresAction: false);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildApprovalRejected(IReadOnlyDictionary<string, string> p)
    {
        var entity = Get(p, "EntityName", "workflow");
        var rejectedBy = Get(p, "RejectedBy", "an approver");
        var reason = Get(p, "Reason", "No reason provided.");
        return new(
            Title: $"Rejected — {entity}",
            Message: $"The approval for {entity} was rejected by {rejectedBy}. Reason: {reason}",
            Category: NotificationCategory.Approval,
            Severity: NotificationSeverity.Warning,
            RequiresAction: false);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildBreakGlassActivated(IReadOnlyDictionary<string, string> p)
    {
        var user = Get(p, "ActivatedBy", "Unknown user");
        return new(
            Title: "Break-glass access activated",
            Message: $"Emergency break-glass access was activated by {user}. Review immediately.",
            Category: NotificationCategory.Security,
            Severity: NotificationSeverity.Critical,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildJitAccessPending(IReadOnlyDictionary<string, string> p)
    {
        var user = Get(p, "RequestedBy", "a user");
        var resource = Get(p, "Resource", "a resource");
        return new(
            Title: $"JIT access pending — {resource}",
            Message: $"Just-in-time access to {resource} has been requested by {user}. Approve or deny.",
            Category: NotificationCategory.Security,
            Severity: NotificationSeverity.ActionRequired,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildComplianceCheckFailed(IReadOnlyDictionary<string, string> p)
    {
        var service = Get(p, "ServiceName", "Unknown service");
        var gapCount = Get(p, "GapCount", "1");
        return new(
            Title: $"Compliance check failed — {service}",
            Message: $"{gapCount} compliance gap(s) detected for {service}. Review and remediate.",
            Category: NotificationCategory.Compliance,
            Severity: NotificationSeverity.Warning,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildBudgetExceeded(IReadOnlyDictionary<string, string> p)
    {
        var service = Get(p, "ServiceName", "Unknown service");
        var expected = Get(p, "ExpectedCost", "N/A");
        var actual = Get(p, "ActualCost", "N/A");
        return new(
            Title: $"Budget exceeded — {service}",
            Message: $"Cost anomaly detected for {service}: expected {expected}, actual {actual}. Review immediately.",
            Category: NotificationCategory.FinOps,
            Severity: NotificationSeverity.Warning,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildIntegrationFailed(IReadOnlyDictionary<string, string> p)
    {
        var integration = Get(p, "IntegrationName", "Unknown integration");
        var error = Get(p, "ErrorMessage", "An error occurred.");
        return new(
            Title: $"Integration failed — {integration}",
            Message: $"Integration {integration} has failed: {error}. Check configuration and retry.",
            Category: NotificationCategory.Integration,
            Severity: NotificationSeverity.Warning,
            RequiresAction: true);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildAiProviderUnavailable(IReadOnlyDictionary<string, string> p)
    {
        var provider = Get(p, "ProviderName", "Unknown provider");
        return new(
            Title: $"AI provider unavailable — {provider}",
            Message: $"The AI provider {provider} is currently unavailable. AI-assisted features may be degraded.",
            Category: NotificationCategory.AI,
            Severity: NotificationSeverity.Warning,
            RequiresAction: false);
    }

    private static Abstractions.ResolvedNotificationTemplate BuildGeneric(
        string eventType,
        IReadOnlyDictionary<string, string> p)
    {
        var description = Get(p, "Description", $"Event {eventType} occurred.");
        return new(
            Title: eventType,
            Message: description,
            Category: NotificationCategory.Informational,
            Severity: NotificationSeverity.Info,
            RequiresAction: false);
    }

    private static string Get(IReadOnlyDictionary<string, string> parameters, string key, string fallback) =>
        parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
}
