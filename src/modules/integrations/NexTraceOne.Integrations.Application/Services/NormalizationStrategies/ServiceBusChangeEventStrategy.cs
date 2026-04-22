using System.Text.Json;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Services.NormalizationStrategies;

/// <summary>
/// Estratégia de normalização para eventos Azure Service Bus.
/// Espera payload JSON com chaves: serviceName, version, env, eventType, occurredAt.
/// Retorna null quando o payload é inválido ou falta serviceName — sinaliza dead letter.
/// </summary>
public sealed class ServiceBusChangeEventStrategy : IEventNormalizationStrategy
{
    /// <inheritdoc />
    public EventSourceType SourceType => EventSourceType.ServiceBus;

    /// <inheritdoc />
    public bool CanHandle(string sourceType) =>
        string.Equals(sourceType, "ServiceBus", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<NormalizedEvent?> NormalizeAsync(RawConsumerEvent raw, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw.Payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("serviceName", out var serviceNameEl))
                return Task.FromResult<NormalizedEvent?>(null);

            var serviceName = serviceNameEl.GetString();
            if (string.IsNullOrWhiteSpace(serviceName))
                return Task.FromResult<NormalizedEvent?>(null);

            var eventType = root.TryGetProperty("eventType", out var etEl)
                ? etEl.GetString() ?? "ChangeEvent"
                : "ChangeEvent";

            var releaseId = root.TryGetProperty("version", out var vEl)
                ? vEl.GetString()
                : null;

            var environment = root.TryGetProperty("env", out var envEl)
                ? envEl.GetString()
                : null;

            var occurredAt = root.TryGetProperty("occurredAt", out var oaEl)
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
