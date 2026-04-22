using System.Text.Json;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Services.NormalizationStrategies;

/// <summary>
/// Estratégia de normalização para eventos Kafka.
/// Espera payload JSON com chaves: service_name, release_id, environment, event_type, occurred_at.
/// Retorna null quando o payload é inválido ou falta service_name — sinaliza dead letter.
/// </summary>
public sealed class KafkaChangeEventStrategy : IEventNormalizationStrategy
{
    /// <inheritdoc />
    public EventSourceType SourceType => EventSourceType.Kafka;

    /// <inheritdoc />
    public bool CanHandle(string sourceType) =>
        string.Equals(sourceType, "Kafka", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<NormalizedEvent?> NormalizeAsync(RawConsumerEvent raw, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw.Payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("service_name", out var serviceNameEl))
                return Task.FromResult<NormalizedEvent?>(null);

            var serviceName = serviceNameEl.GetString();
            if (string.IsNullOrWhiteSpace(serviceName))
                return Task.FromResult<NormalizedEvent?>(null);

            var eventType = root.TryGetProperty("event_type", out var etEl)
                ? etEl.GetString() ?? "ChangeEvent"
                : "ChangeEvent";

            var releaseId = root.TryGetProperty("release_id", out var riEl)
                ? riEl.GetString()
                : null;

            var environment = root.TryGetProperty("environment", out var envEl)
                ? envEl.GetString()
                : null;

            var occurredAt = root.TryGetProperty("occurred_at", out var oaEl)
                && oaEl.TryGetDateTimeOffset(out var oaParsed)
                ? oaParsed
                : raw.ReceivedAt;

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
