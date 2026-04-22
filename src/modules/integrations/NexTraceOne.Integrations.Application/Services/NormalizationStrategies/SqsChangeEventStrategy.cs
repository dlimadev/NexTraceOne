using System.Text.Json;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Services.NormalizationStrategies;

/// <summary>
/// Estratégia de normalização para eventos Amazon SQS.
/// Espera envelope SQS: { "Type": "Notification", "Message": "{...}" }
/// onde o corpo da mensagem contém service_name, release_id, environment.
/// Retorna null quando o envelope é inválido ou falta service_name — sinaliza dead letter.
/// </summary>
public sealed class SqsChangeEventStrategy : IEventNormalizationStrategy
{
    /// <inheritdoc />
    public EventSourceType SourceType => EventSourceType.Sqs;

    /// <inheritdoc />
    public bool CanHandle(string sourceType) =>
        string.Equals(sourceType, "SQS", StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, "Sqs", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<NormalizedEvent?> NormalizeAsync(RawConsumerEvent raw, CancellationToken ct)
    {
        try
        {
            using var outerDoc = JsonDocument.Parse(raw.Payload);
            var outer = outerDoc.RootElement;

            // Suporta envelope SNS→SQS ou payload direto
            JsonElement root;
            if (outer.TryGetProperty("Message", out var msgEl))
            {
                var msgStr = msgEl.GetString();
                if (string.IsNullOrWhiteSpace(msgStr))
                    return Task.FromResult<NormalizedEvent?>(null);

                using var innerDoc = JsonDocument.Parse(msgStr);
                // Necessário extrair antes de libertar innerDoc — converter para string intermédia
                return Task.FromResult(ParseBody(JsonDocument.Parse(msgStr).RootElement, raw));
            }

            root = outer;
            return Task.FromResult(ParseBody(root, raw));
        }
        catch (JsonException)
        {
            return Task.FromResult<NormalizedEvent?>(null);
        }
    }

    private static NormalizedEvent? ParseBody(JsonElement root, RawConsumerEvent raw)
    {
        if (!root.TryGetProperty("service_name", out var serviceNameEl))
            return null;

        var serviceName = serviceNameEl.GetString();
        if (string.IsNullOrWhiteSpace(serviceName))
            return null;

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

        return new NormalizedEvent(
            EventType: eventType,
            ServiceName: serviceName,
            ReleaseId: releaseId,
            EnvironmentName: environment,
            OccurredAt: occurredAt,
            RawPayload: raw.Payload);
    }
}
