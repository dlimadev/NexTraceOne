using System.Text.Json;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Services.NormalizationStrategies;

/// <summary>
/// Estratégia de normalização para eventos RabbitMQ (AMQP).
/// Espera envelope AMQP: { "headers": { "x-service": "...", "x-release": "...", "x-env": "..." }, "body": "{...}" }
/// ou payload direto com service_name no corpo. Retorna null quando inválido — sinaliza dead letter.
/// </summary>
public sealed class RabbitMqChangeEventStrategy : IEventNormalizationStrategy
{
    /// <inheritdoc />
    public EventSourceType SourceType => EventSourceType.RabbitMq;

    /// <inheritdoc />
    public bool CanHandle(string sourceType) =>
        string.Equals(sourceType, "RabbitMQ", StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, "RabbitMq", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<NormalizedEvent?> NormalizeAsync(RawConsumerEvent raw, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw.Payload);
            var root = doc.RootElement;

            string? serviceName = null;
            string? releaseId = null;
            string? environment = null;
            string eventType = "ChangeEvent";
            DateTimeOffset occurredAt = raw.ReceivedAt;

            // Tenta extrair do envelope AMQP com headers
            if (root.TryGetProperty("headers", out var headers))
            {
                serviceName = headers.TryGetProperty("x-service", out var svcEl) ? svcEl.GetString() : null;
                releaseId = headers.TryGetProperty("x-release", out var relEl) ? relEl.GetString() : null;
                environment = headers.TryGetProperty("x-env", out var envEl) ? envEl.GetString() : null;
                eventType = headers.TryGetProperty("x-event-type", out var etEl)
                    ? etEl.GetString() ?? "ChangeEvent"
                    : "ChangeEvent";
            }

            // Fallback: tentar extrair do body ou do root diretamente
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                JsonElement body = root;
                if (root.TryGetProperty("body", out var bodyEl))
                {
                    var bodyStr = bodyEl.GetString();
                    if (!string.IsNullOrWhiteSpace(bodyStr))
                    {
                        using var bodyDoc = JsonDocument.Parse(bodyStr);
                        body = bodyDoc.RootElement.Clone();
                    }
                }

                if (body.TryGetProperty("service_name", out var snEl))
                    serviceName = snEl.GetString();

                if (body.TryGetProperty("release_id", out var riEl))
                    releaseId = riEl.GetString();

                if (body.TryGetProperty("environment", out var envEl2))
                    environment = envEl2.GetString();

                if (body.TryGetProperty("occurred_at", out var oaEl)
                    && oaEl.TryGetDateTimeOffset(out var oaParsed))
                    occurredAt = oaParsed;
            }

            if (string.IsNullOrWhiteSpace(serviceName))
                return Task.FromResult<NormalizedEvent?>(null);

            return Task.FromResult<NormalizedEvent?>(new NormalizedEvent(
                EventType: eventType,
                ServiceName: serviceName,
                ReleaseId: releaseId,
                EnvironmentName: environment,
                OccurredAt: occurredAt,
                RawPayload: raw.Payload));
        }
        catch (JsonException)
        {
            return Task.FromResult<NormalizedEvent?>(null);
        }
    }
}
