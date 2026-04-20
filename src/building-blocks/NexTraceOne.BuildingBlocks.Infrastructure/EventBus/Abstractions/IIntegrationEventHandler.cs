namespace NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;

/// <summary>
/// Handler para Integration Events recebidos de outros módulos.
/// Cada módulo implementa handlers para os eventos que deseja consumir.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}
