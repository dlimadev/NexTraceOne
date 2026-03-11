namespace NexTraceOne.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Evento de integração publicado quando o papel de um usuário muda em um tenant.
/// </summary>
public sealed record UserRoleChangedIntegrationEvent(Guid UserId, Guid TenantId, string RoleName)
{
    /// <summary>Identificador único do evento.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Data/hora UTC de ocorrência do evento.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Módulo de origem do evento.</summary>
    public string SourceModule { get; init; } = "Identity";
}
