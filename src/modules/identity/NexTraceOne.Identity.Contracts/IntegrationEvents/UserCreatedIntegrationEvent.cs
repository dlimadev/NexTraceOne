using NexTraceOne.BuildingBlocks.Domain;

namespace NexTraceOne.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Evento de integração publicado quando um usuário é criado.
/// </summary>
public sealed record UserCreatedIntegrationEvent(Guid UserId, string Email, Guid TenantId) : IIntegrationEvent
{
    /// <summary>Identificador único do evento.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Data/hora UTC de ocorrência do evento.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Módulo de origem do evento.</summary>
    public string SourceModule { get; init; } = "Identity";
}
