using NexTraceOne.AIKnowledge.Application.Governance.Features.HandleModelFeedbackThresholdExceeded;
using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.EventHandlers;

/// <summary>
/// Adaptador de infra que implementa IIntegrationEventHandler para
/// ModelFeedbackThresholdExceededIntegrationEvent.
/// Delega ao handler da camada Application, mantendo Clean Architecture. (E-M02)
/// </summary>
internal sealed class ModelFeedbackThresholdExceededEventHandlerAdapter(
    HandleModelFeedbackThresholdExceededHandler handler)
    : IIntegrationEventHandler<ModelFeedbackThresholdExceededIntegrationEvent>
{
    public Task HandleAsync(
        ModelFeedbackThresholdExceededIntegrationEvent @event,
        CancellationToken ct = default)
        => handler.HandleAsync(@event, ct);
}
