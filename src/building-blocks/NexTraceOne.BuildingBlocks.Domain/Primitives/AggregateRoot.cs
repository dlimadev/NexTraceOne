namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Classe base para todos os Aggregate Roots do domínio.
/// Um Aggregate Root é a única entidade através da qual o lado de fora
/// pode interagir com o agregado. Ele é responsável por manter as invariantes
/// e a consistência transacional de todas as entidades do agregado.
/// Também é o único responsável por emitir Domain Events.
/// </summary>
/// <typeparam name="TId">Tipo do identificador fortemente tipado.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : ITypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Coleção imutável dos Domain Events emitidos por este aggregate
    /// durante a operação atual. Serão coletados pelo DbContext antes do commit.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Registra um Domain Event para ser publicado após o commit.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Limpa a fila de eventos após coleta pelo DbContext.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
