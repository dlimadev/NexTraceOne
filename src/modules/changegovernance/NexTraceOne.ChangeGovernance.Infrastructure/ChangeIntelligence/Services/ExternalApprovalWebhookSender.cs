using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação do port <see cref="IExternalApprovalWebhookSender"/> via HttpClient.
/// Envia pedidos de aprovação outbound para sistemas externos (ServiceNow, Teams, Slack, custom).
/// Falhas são tratadas com graceful degradation: retorna false sem lançar excepção.
/// </summary>
internal sealed class ExternalApprovalWebhookSender(
    IHttpClientFactory httpClientFactory,
    ILogger<ExternalApprovalWebhookSender> logger) : IExternalApprovalWebhookSender
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<bool> SendAsync(
        string webhookUrl,
        ApprovalWebhookPayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("ExternalApprovalWebhook");
            using var response = await client.PostAsJsonAsync(webhookUrl, payload, SerializerOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "External approval webhook returned non-success status {StatusCode} for URL {WebhookUrl}. ReleaseId: {ReleaseId}",
                    (int)response.StatusCode, webhookUrl, payload.ReleaseId);
                return false;
            }

            logger.LogInformation(
                "External approval webhook sent successfully to {WebhookUrl}. ApprovalRequestId: {ApprovalRequestId}",
                webhookUrl, payload.ApprovalRequestId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "HTTP error sending external approval webhook to {WebhookUrl}. ReleaseId: {ReleaseId}",
                webhookUrl, payload.ReleaseId);
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex,
                "Timeout sending external approval webhook to {WebhookUrl}. ReleaseId: {ReleaseId}",
                webhookUrl, payload.ReleaseId);
            return false;
        }
    }
}
