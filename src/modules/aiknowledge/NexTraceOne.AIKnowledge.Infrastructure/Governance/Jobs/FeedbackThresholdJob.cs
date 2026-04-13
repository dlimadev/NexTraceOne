using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job periódico que agrega feedback negativo de IA por agente e modelo nas últimas 24h.
/// Quando o total excede o threshold configurado (padrão: 5), publica
/// ModelFeedbackThresholdExceededIntegrationEvent para alertar o Platform Admin.
///
/// Design:
/// - BackgroundService com PeriodicTimer (execução horária).
/// - Cria scope por ciclo para isolar DbContext e serviços Scoped.
/// - Threshold é 5 negativos nas últimas 24h (candidato a configuração por tenant).
/// </summary>
internal sealed class FeedbackThresholdJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<FeedbackThresholdJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "ai-feedback-threshold-job";

    /// <summary>Número de feedbacks negativos que aciona o alerta (candidato a configuração).</summary>
    private const int DefaultNegativeThreshold = 5;

    /// <summary>Janela de análise de feedback negativo.</summary>
    private static readonly TimeSpan FeedbackWindow = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FeedbackThresholdJob started.");

        // Aguarda 60s antes do primeiro ciclo para deixar a aplicação inicializar
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in FeedbackThresholdJob cycle.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        logger.LogInformation("FeedbackThresholdJob stopped.");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var feedbackRepository = scope.ServiceProvider.GetRequiredService<IAiFeedbackRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var since = dateTimeProvider.UtcNow - FeedbackWindow;

        // Obter combinações distintas de agente+modelo com feedback negativo recente
        var recentNegative = await feedbackRepository.ListByRatingAsync(
            Domain.Governance.Enums.FeedbackRating.Negative, 500, cancellationToken);

        var candidates = recentNegative
            .Where(f => f.SubmittedAt >= since)
            .GroupBy(f => (f.AgentName, f.ModelUsed));

        foreach (var group in candidates)
        {
            var count = group.Count();
            if (count < DefaultNegativeThreshold)
                continue;

            var (agentName, modelUsed) = group.Key;

            logger.LogWarning(
                "AI feedback threshold exceeded: Agent={AgentName}, Model={Model}, NegativeCount={Count}, Window=24h",
                agentName, modelUsed, count);

            await eventBus.PublishAsync(
                new ModelFeedbackThresholdExceededIntegrationEvent(
                    agentName,
                    modelUsed,
                    count,
                    DefaultNegativeThreshold,
                    "24h",
                    TenantId: null),
                cancellationToken);
        }
    }
}
