using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.HandleModelFeedbackThresholdExceeded;

/// <summary>
/// Handler de integração para ModelFeedbackThresholdExceededIntegrationEvent.
/// Quando o feedback negativo acumulado sobre um modelo excede o threshold configurado,
/// reduz automaticamente a prioridade de todas as estratégias de roteamento activas,
/// despriorizando-as no pipeline de seleção de roteamento (E-M02).
///
/// Lógica:
/// - Recupera todas as estratégias activas
/// - Chama ReducePriorityDueToNegativeFeedback em cada uma
/// - Persiste as alterações via IUnitOfWork
/// </summary>
public sealed class HandleModelFeedbackThresholdExceededHandler(
    IAiRoutingStrategyRepository routingStrategyRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<HandleModelFeedbackThresholdExceededHandler> logger)
{
    public async Task HandleAsync(
        ModelFeedbackThresholdExceededIntegrationEvent @event,
        CancellationToken ct = default)
    {
        logger.LogWarning(
            "Feedback threshold exceeded: Agent={AgentName} Model={ModelUsed} NegativeCount={NegativeCount} Threshold={Threshold} Period={Period}. " +
            "Applying automatic routing priority reduction.",
            @event.AgentName, @event.ModelUsed, @event.NegativeCount, @event.ThresholdValue, @event.Period);

        var activeStrategies = await routingStrategyRepository.ListAsync(isActive: true, ct);

        if (activeStrategies.Count == 0)
        {
            logger.LogInformation(
                "No active routing strategies found to adjust for model {ModelUsed}.", @event.ModelUsed);
            return;
        }

        var now = dateTimeProvider.UtcNow;
        var reason = $"Automatic priority reduction: model '{@event.ModelUsed}' exceeded negative feedback threshold " +
                     $"({@event.NegativeCount}/{@event.ThresholdValue} in {@event.Period})";

        var adjustedCount = 0;
        foreach (var strategy in activeStrategies)
        {
            strategy.ReducePriorityDueToNegativeFeedback(reason, now);
            await routingStrategyRepository.UpdateAsync(strategy, ct);
            adjustedCount++;
        }

        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "Routing priority reduction applied to {Count} active strategies. " +
            "Model={ModelUsed} Agent={AgentName}",
            adjustedCount, @event.ModelUsed, @event.AgentName);
    }
}
