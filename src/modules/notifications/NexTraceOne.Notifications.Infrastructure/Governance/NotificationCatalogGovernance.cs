using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Governance;

/// <summary>
/// Implementação da governança do catálogo de notificações.
/// Phase 7 — valida tipos, cobertura de templates, canais e regras obrigatórias.
///
/// Governança:
///   - Todos os tipos devem estar registados em NotificationType.All
///   - Cada tipo deve ter template no resolver
///   - Tipos obrigatórios definidos pelo MandatoryNotificationPolicy
///   - Canais devem ter estado conhecido
/// </summary>
internal sealed class NotificationCatalogGovernance(
    INotificationTemplateResolver templateResolver,
    IMandatoryNotificationPolicy mandatoryPolicy,
    ILogger<NotificationCatalogGovernance> logger) : INotificationCatalogGovernance
{
    /// <summary>Tipos que têm template dedicado no resolver (não generic fallback).</summary>
    private static readonly HashSet<string> TypesWithDedicatedTemplate =
    [
        NotificationType.IncidentCreated,
        NotificationType.IncidentEscalated,
        NotificationType.ApprovalPending,
        NotificationType.ApprovalApproved,
        NotificationType.ApprovalRejected,
        NotificationType.BreakGlassActivated,
        NotificationType.JitAccessPending,
        NotificationType.ComplianceCheckFailed,
        NotificationType.BudgetExceeded,
        NotificationType.IntegrationFailed,
        NotificationType.AiProviderUnavailable
    ];

    /// <inheritdoc/>
    public Task<CatalogGovernanceSummary> GetGovernanceSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var allTypes = NotificationType.All;
        var typesWithTemplate = allTypes.Where(t => TypesWithDedicatedTemplate.Contains(t)).ToList();
        var typesWithoutTemplate = allTypes.Where(t => !TypesWithDedicatedTemplate.Contains(t)).ToList();

        var mandatoryCount = allTypes.Count(t =>
        {
            // Check using Critical severity and Incident category as test
            return mandatoryPolicy.IsMandatory(t, NotificationCategory.Incident, NotificationSeverity.Critical);
        });

        var channelStatus = new Dictionary<string, bool>
        {
            [DeliveryChannel.InApp.ToString()] = true,
            [DeliveryChannel.Email.ToString()] = true,
            [DeliveryChannel.MicrosoftTeams.ToString()] = true
        };

        var totalCategories = Enum.GetValues<NotificationCategory>().Length;

        logger.LogDebug(
            "Catalog governance: {Total} types, {WithTemplate} with template, {Without} without, {Mandatory} mandatory",
            allTypes.Count, typesWithTemplate.Count, typesWithoutTemplate.Count, mandatoryCount);

        return Task.FromResult(new CatalogGovernanceSummary
        {
            TotalEventTypes = allTypes.Count,
            TypesWithTemplate = typesWithTemplate.Count,
            TypesWithoutTemplate = typesWithoutTemplate,
            MandatoryTypes = mandatoryCount,
            ChannelStatus = channelStatus,
            TotalCategories = totalCategories
        });
    }

    /// <inheritdoc/>
    public Task<CatalogValidationResult> ValidateEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var isRegistered = NotificationType.IsValid(eventType);
        var hasTemplate = TypesWithDedicatedTemplate.Contains(eventType);

        // Check mandatory status using critical severity for worst-case
        var isMandatory = mandatoryPolicy.IsMandatory(
            eventType, NotificationCategory.Incident, NotificationSeverity.Critical);

        if (!isRegistered)
            messages.Add($"Event type '{eventType}' is not registered in the notification catalog.");

        if (!hasTemplate)
            messages.Add($"Event type '{eventType}' does not have a dedicated template (uses generic fallback).");

        if (isMandatory && !hasTemplate)
            messages.Add($"Mandatory event type '{eventType}' should have a dedicated template.");

        var isValid = isRegistered && (hasTemplate || !isMandatory);

        return Task.FromResult(new CatalogValidationResult
        {
            IsValid = isValid,
            EventType = eventType,
            HasTemplate = hasTemplate,
            IsMandatory = isMandatory,
            Messages = messages
        });
    }
}
