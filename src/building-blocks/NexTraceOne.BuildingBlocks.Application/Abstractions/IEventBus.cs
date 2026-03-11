namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração do barramento de eventos para Integration Events.
/// Integration Events cruzam fronteiras de módulo via Outbox Pattern.
/// REGRA: Use para eventos ENTRE módulos. Para eventos DENTRO do módulo, use DomainEvent.
/// </summary>
public interface IEventBus
{
    /// <summary>Publica um Integration Event via Outbox Pattern.</summary>
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class;
}
