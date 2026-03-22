using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Channels;

/// <summary>
/// Canal de alertas via HTTP POST (Webhook).
/// Envia o payload JSON para o URL configurado, com headers opcionais e timeout.
/// Utiliza HttpClientFactory para gestão eficiente de conexões.
/// </summary>
public sealed class WebhookAlertChannel : IAlertChannel
{
    /// <summary>Nome HTTP client registado no HttpClientFactory.</summary>
    public const string HttpClientName = "NexTraceOne.Alerting.Webhook";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AlertingOptions> _options;
    private readonly ILogger<WebhookAlertChannel> _logger;

    public WebhookAlertChannel(
        IHttpClientFactory httpClientFactory,
        IOptions<AlertingOptions> options,
        ILogger<WebhookAlertChannel> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ChannelName => "Webhook";

    /// <inheritdoc />
    public async Task<bool> SendAsync(AlertPayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var webhookOptions = _options.Value.Webhook;

        if (string.IsNullOrWhiteSpace(webhookOptions.Url))
        {
            _logger.LogWarning("Webhook alert channel URL is not configured; skipping dispatch");
            return false;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);

            foreach (var header in webhookOptions.Headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(webhookOptions.TimeoutSeconds));

            var response = await client.PostAsJsonAsync(webhookOptions.Url, payload, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Alert dispatched via Webhook: {Title} [{Severity}]",
                    payload.Title,
                    payload.Severity);
                return true;
            }

            _logger.LogWarning(
                "Webhook alert dispatch failed with HTTP {StatusCode} for alert: {Title}",
                (int)response.StatusCode,
                payload.Title);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Webhook alert dispatch cancelled for alert: {Title}", payload.Title);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook alert dispatch error for alert: {Title}", payload.Title);
            return false;
        }
    }
}
