using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;

/// <summary>
/// Abstração para parsers de eventos legacy que normalizam payloads específicos
/// em NormalizedLegacyEvent canónico.
/// </summary>
public interface ILegacyEventParser<TRequest>
{
    NormalizedLegacyEvent Parse(TRequest request);
}
