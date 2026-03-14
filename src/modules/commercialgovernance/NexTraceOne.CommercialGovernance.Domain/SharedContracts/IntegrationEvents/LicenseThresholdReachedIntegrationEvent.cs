using NexTraceOne.BuildingBlocks.Core;

namespace NexTraceOne.Licensing.Contracts.IntegrationEvents;

/// <summary>
/// Evento de integração publicado quando uma quota de licença atinge o threshold configurado.
/// </summary>
public sealed record LicenseThresholdReachedIntegrationEvent(Guid LicenseId, string LicenseKey, string MetricCode, long CurrentUsage, long Limit) : IIntegrationEvent
{
    /// <summary>Identificador único do evento.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Data/hora UTC de ocorrência do evento.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Módulo de origem do evento.</summary>
    public string SourceModule { get; init; } = "Licensing";
}
