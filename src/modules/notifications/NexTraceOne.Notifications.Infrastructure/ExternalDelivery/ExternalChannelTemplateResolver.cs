using System.Text;
using System.Text.Json;

using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Resolve templates específicos para canais externos (email HTML e Teams Adaptive Card).
/// Gera conteúdo adequado ao formato de cada canal, mantendo consistência de informação.
/// </summary>
internal sealed class ExternalChannelTemplateResolver : IExternalChannelTemplateResolver
{
    /// <inheritdoc/>
    public EmailTemplate ResolveEmailTemplate(Notification notification, string baseUrl)
    {
        var severityColor = GetSeverityColor(notification.Severity);
        var actionLink = BuildActionLink(notification, baseUrl);

        var subject = $"[NexTraceOne] [{notification.Severity}] {notification.Title}";

        var htmlBody = BuildEmailHtml(notification, severityColor, actionLink);
        var plainTextBody = BuildEmailPlainText(notification, actionLink);

        return new EmailTemplate(subject, htmlBody, plainTextBody);
    }

    /// <inheritdoc/>
    public TeamsCardPayload ResolveTeamsTemplate(Notification notification, string baseUrl)
    {
        var actionLink = BuildActionLink(notification, baseUrl);
        var json = BuildAdaptiveCardJson(notification, actionLink);
        return new TeamsCardPayload(json);
    }

    private static string BuildActionLink(Notification notification, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(notification.ActionUrl))
            return baseUrl.TrimEnd('/') + "/notifications";

        var actionUrl = notification.ActionUrl;
        if (actionUrl.StartsWith('/'))
            return baseUrl.TrimEnd('/') + actionUrl;

        return actionUrl;
    }

    private static string GetSeverityColor(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Critical => "#D32F2F",
        NotificationSeverity.Warning => "#F57C00",
        NotificationSeverity.ActionRequired => "#1976D2",
        NotificationSeverity.Info => "#388E3C",
        _ => "#616161"
    };

    private static string GetSeverityEmoji(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Critical => "🔴",
        NotificationSeverity.Warning => "🟠",
        NotificationSeverity.ActionRequired => "🔵",
        NotificationSeverity.Info => "🟢",
        _ => "⚪"
    };

    private static string BuildEmailHtml(Notification notification, string severityColor, string actionLink)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\"></head>");
        sb.AppendLine("<body style=\"font-family:Segoe UI,Helvetica,Arial,sans-serif;margin:0;padding:0;background-color:#f5f5f5\">");
        sb.AppendLine("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;margin:20px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 4px rgba(0,0,0,0.1)\">");

        // Header bar
        sb.AppendLine($"<tr><td style=\"background-color:{severityColor};padding:16px 24px;color:#fff;font-size:14px;font-weight:600\">");
        sb.AppendLine($"NexTraceOne — {notification.Severity} Notification");
        sb.AppendLine("</td></tr>");

        // Title
        sb.AppendLine("<tr><td style=\"padding:24px 24px 8px\">");
        sb.AppendLine($"<h2 style=\"margin:0;color:#212121;font-size:18px\">{Sanitize(notification.Title)}</h2>");
        sb.AppendLine("</td></tr>");

        // Message
        sb.AppendLine("<tr><td style=\"padding:8px 24px\">");
        sb.AppendLine($"<p style=\"margin:0;color:#424242;font-size:14px;line-height:1.6\">{Sanitize(notification.Message)}</p>");
        sb.AppendLine("</td></tr>");

        // Context table
        sb.AppendLine("<tr><td style=\"padding:16px 24px\">");
        sb.AppendLine("<table style=\"width:100%;font-size:13px;color:#616161;border-collapse:collapse\">");
        sb.AppendLine($"<tr><td style=\"padding:4px 0;font-weight:600\">Category</td><td style=\"padding:4px 0\">{notification.Category}</td></tr>");
        sb.AppendLine($"<tr><td style=\"padding:4px 0;font-weight:600\">Severity</td><td style=\"padding:4px 0\">{notification.Severity}</td></tr>");
        sb.AppendLine($"<tr><td style=\"padding:4px 0;font-weight:600\">Source</td><td style=\"padding:4px 0\">{Sanitize(notification.SourceModule)}</td></tr>");
        if (!string.IsNullOrWhiteSpace(notification.SourceEntityId))
            sb.AppendLine($"<tr><td style=\"padding:4px 0;font-weight:600\">Entity</td><td style=\"padding:4px 0\">{Sanitize(notification.SourceEntityType)} / {Sanitize(notification.SourceEntityId)}</td></tr>");
        sb.AppendLine($"<tr><td style=\"padding:4px 0;font-weight:600\">Time</td><td style=\"padding:4px 0\">{notification.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</td></tr>");

        // Action button
        sb.AppendLine("<tr><td style=\"padding:16px 24px 24px\">");
        sb.AppendLine($"<a href=\"{Sanitize(actionLink)}\" style=\"display:inline-block;background-color:{severityColor};color:#fff;text-decoration:none;padding:10px 24px;border-radius:4px;font-size:14px;font-weight:600\">");
        sb.AppendLine(notification.RequiresAction ? "Take Action" : "View Details");
        sb.AppendLine("</a>");
        sb.AppendLine("</td></tr>");

        // Footer
        sb.AppendLine("<tr><td style=\"padding:16px 24px;background-color:#f5f5f5;font-size:12px;color:#9e9e9e;text-align:center\">");
        sb.AppendLine("This is an automated notification from NexTraceOne. Do not reply to this email.");
        sb.AppendLine("</td></tr>");

        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    private static string BuildEmailPlainText(Notification notification, string actionLink)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[NexTraceOne] [{notification.Severity}] {notification.Title}");
        sb.AppendLine();
        sb.AppendLine(notification.Message);
        sb.AppendLine();
        sb.AppendLine($"Category: {notification.Category}");
        sb.AppendLine($"Severity: {notification.Severity}");
        sb.AppendLine($"Source: {notification.SourceModule}");
        if (!string.IsNullOrWhiteSpace(notification.SourceEntityId))
            sb.AppendLine($"Entity: {notification.SourceEntityType} / {notification.SourceEntityId}");
        sb.AppendLine($"Time: {notification.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine($"Link: {actionLink}");
        sb.AppendLine();
        sb.AppendLine("--");
        sb.AppendLine("NexTraceOne — Automated Notification");
        return sb.ToString();
    }

    private static string BuildAdaptiveCardJson(Notification notification, string actionLink)
    {
        var severityEmoji = GetSeverityEmoji(notification.Severity);

        var card = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    contentUrl = (string?)null,
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = BuildAdaptiveCardBody(notification, severityEmoji),
                        actions = new object[]
                        {
                            new
                            {
                                type = "Action.OpenUrl",
                                title = notification.RequiresAction ? "Take Action" : "View in NexTraceOne",
                                url = actionLink
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(card, new JsonSerializerOptions { WriteIndented = false });
    }

    private static object[] BuildAdaptiveCardBody(Notification notification, string severityEmoji)
    {
        var items = new List<object>
        {
            // Header
            new
            {
                type = "TextBlock",
                text = $"{severityEmoji} {notification.Title}",
                weight = "Bolder",
                size = "Medium",
                wrap = true
            },
            // Severity badge
            new
            {
                type = "TextBlock",
                text = $"**Severity:** {notification.Severity} | **Category:** {notification.Category}",
                spacing = "Small",
                isSubtle = true,
                wrap = true
            },
            // Message
            new
            {
                type = "TextBlock",
                text = notification.Message,
                wrap = true,
                spacing = "Medium"
            },
            // Separator
            new
            {
                type = "TextBlock",
                text = "---",
                spacing = "Small"
            },
            // Context
            new
            {
                type = "FactSet",
                facts = BuildFactSet(notification)
            }
        };

        return [.. items];
    }

    private static object[] BuildFactSet(Notification notification)
    {
        var facts = new List<object>
        {
            new { title = "Source", value = notification.SourceModule },
            new { title = "Time", value = notification.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" }
        };

        if (!string.IsNullOrWhiteSpace(notification.SourceEntityType))
            facts.Add(new { title = "Entity", value = $"{notification.SourceEntityType} / {notification.SourceEntityId}" });

        return [.. facts];
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
