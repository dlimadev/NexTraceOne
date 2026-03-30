using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;

/// <summary>
/// Abstração para persistência de eventos legacy normalizados em storage analítico (Elasticsearch).
/// </summary>
public interface ILegacyEventWriter
{
    Task WriteLegacyEventsAsync(
        IReadOnlyList<NormalizedLegacyEvent> events,
        CancellationToken cancellationToken = default);
}
