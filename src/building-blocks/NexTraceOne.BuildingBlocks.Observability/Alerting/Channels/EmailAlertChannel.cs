using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Channels;

/// <summary>
/// Canal de alertas via email SMTP.
/// Constrói um email HTML profissional com cor por severidade e envia via SmtpClient.
/// Utiliza System.Net.Mail (built-in) sem dependências externas.
/// </summary>
public sealed class EmailAlertChannel : IAlertChannel
{
    private readonly IOptions<AlertingOptions> _options;
    private readonly ILogger<EmailAlertChannel> _logger;

    public EmailAlertChannel(
        IOptions<AlertingOptions> options,
        ILogger<EmailAlertChannel> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ChannelName => "Email";

    /// <inheritdoc />
    public async Task<bool> SendAsync(AlertPayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var emailOptions = _options.Value.Email;

        if (string.IsNullOrWhiteSpace(emailOptions.SmtpHost))
        {
            _logger.LogWarning("Email alert channel SMTP host is not configured; skipping dispatch");
            return false;
        }

        if (string.IsNullOrWhiteSpace(emailOptions.FromAddress))
        {
            _logger.LogWarning("Email alert channel sender address is not configured; skipping dispatch");
            return false;
        }

        if (emailOptions.Recipients.Count == 0)
        {
            _logger.LogWarning("Email alert channel has no recipients configured; skipping dispatch");
            return false;
        }

        try
        {
            using var message = BuildMailMessage(payload, emailOptions);
            using var smtpClient = CreateSmtpClient(emailOptions);

            await smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation(
                "Alert dispatched via Email to {RecipientCount} recipients: {Title} [{Severity}]",
                emailOptions.Recipients.Count,
                payload.Title,
                payload.Severity);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Email alert dispatch cancelled for alert: {Title}", payload.Title);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email alert dispatch error for alert: {Title}", payload.Title);
            return false;
        }
    }

    private static MailMessage BuildMailMessage(AlertPayload payload, EmailChannelOptions emailOptions)
    {
        var from = new MailAddress(emailOptions.FromAddress!, emailOptions.FromName);
        var subject = $"[{payload.Severity}] {payload.Title}";

        var message = new MailMessage
        {
            From = from,
            Subject = subject,
            Body = BuildHtmlBody(payload),
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        foreach (var recipient in emailOptions.Recipients)
        {
            message.To.Add(recipient);
        }

        return message;
    }

    private static SmtpClient CreateSmtpClient(EmailChannelOptions emailOptions)
    {
        var client = new SmtpClient(emailOptions.SmtpHost, emailOptions.SmtpPort)
        {
            EnableSsl = emailOptions.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(emailOptions.Username) &&
            !string.IsNullOrWhiteSpace(emailOptions.Password))
        {
            client.Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password);
        }

        return client;
    }

    private static string BuildHtmlBody(AlertPayload payload)
    {
        var severityColor = payload.Severity switch
        {
            AlertSeverity.Info => "#2196F3",
            AlertSeverity.Warning => "#FF9800",
            AlertSeverity.Error => "#F44336",
            AlertSeverity.Critical => "#9C27B0",
            _ => "#757575"
        };

        var contextRows = new StringBuilder();
        foreach (var kvp in payload.Context)
        {
            contextRows.Append(System.Globalization.CultureInfo.InvariantCulture, $"""
                <tr>
                    <td style="padding:6px 12px;font-weight:bold;color:#555;">{WebUtility.HtmlEncode(kvp.Key)}</td>
                    <td style="padding:6px 12px;color:#333;">{WebUtility.HtmlEncode(kvp.Value)}</td>
                </tr>
            """);
        }

        var contextSection = payload.Context.Count > 0
            ? $"""
                <h3 style="color:#333;margin-top:20px;">Context</h3>
                <table style="border-collapse:collapse;width:100%;">
                    {contextRows}
                </table>
            """
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Segoe UI,Roboto,Helvetica Neue,Arial,sans-serif;margin:0;padding:20px;background:#f5f5f5;">
                <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
                    <div style="background:{severityColor};padding:20px 24px;">
                        <h1 style="color:#fff;margin:0;font-size:20px;">{WebUtility.HtmlEncode(payload.Title)}</h1>
                        <span style="color:rgba(255,255,255,0.85);font-size:13px;">{payload.Severity} — {WebUtility.HtmlEncode(payload.Source)}</span>
                    </div>
                    <div style="padding:24px;">
                        <p style="color:#333;line-height:1.6;margin-top:0;">{WebUtility.HtmlEncode(payload.Description)}</p>
                        <table style="border-collapse:collapse;width:100%;margin-top:16px;">
                            <tr>
                                <td style="padding:6px 12px;font-weight:bold;color:#555;">Timestamp</td>
                                <td style="padding:6px 12px;color:#333;">{payload.Timestamp:yyyy-MM-dd HH:mm:ss zzz}</td>
                            </tr>
                            <tr>
                                <td style="padding:6px 12px;font-weight:bold;color:#555;">Correlation ID</td>
                                <td style="padding:6px 12px;color:#333;">{WebUtility.HtmlEncode(payload.CorrelationId ?? "—")}</td>
                            </tr>
                        </table>
                        {contextSection}
                    </div>
                    <div style="padding:16px 24px;background:#fafafa;color:#999;font-size:12px;text-align:center;">
                        NexTraceOne Alerting Gateway
                    </div>
                </div>
            </body>
            </html>
        """;
    }
}
