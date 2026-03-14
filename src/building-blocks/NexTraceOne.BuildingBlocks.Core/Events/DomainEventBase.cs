namespace NexTraceOne.BuildingBlocks.Core;

/// <summary>
/// Implementação base para Domain Events.
/// Preenche automaticamente EventId e OccurredAt.
/// Todo Domain Event da plataforma deve herdar desta classe.
/// Exemplo: public sealed record ReleaseCreatedDomainEvent(ReleaseId ReleaseId) : DomainEventBase;
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
