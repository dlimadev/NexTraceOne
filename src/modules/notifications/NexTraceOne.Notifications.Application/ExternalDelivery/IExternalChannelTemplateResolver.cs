using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.ExternalDelivery;

/// <summary>
/// Resolve templates específicos por canal externo (email, Teams).
/// Gera subject/body para email e payload estruturado para Teams.
/// </summary>
public interface IExternalChannelTemplateResolver
{
    /// <summary>Gera subject e corpo HTML para o canal de email.</summary>
    EmailTemplate ResolveEmailTemplate(Notification notification, string baseUrl);

    /// <summary>Gera payload de Adaptive Card para o canal Microsoft Teams.</summary>
    TeamsCardPayload ResolveTeamsTemplate(Notification notification, string baseUrl);
}

/// <summary>Template materializado para envio por email.</summary>
public sealed record EmailTemplate(
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);

/// <summary>Payload estruturado para Adaptive Card do Microsoft Teams.</summary>
public sealed record TeamsCardPayload(string JsonPayload);
