using NexTraceOne.BuildingBlocks.Domain;

namespace NexTraceOne.Licensing.Contracts.IntegrationEvents;

/// <summary>
/// Evento de integração publicado quando uma licença é ativada com sucesso.
/// </summary>
public sealed record LicenseActivatedIntegrationEvent(Guid LicenseId, string LicenseKey, string HardwareFingerprint) : IIntegrationEvent
{
    /// <summary>Identificador único do evento.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Data/hora UTC de ocorrência do evento.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Módulo de origem do evento.</summary>
    public string SourceModule { get; init; } = "Licensing";
}
