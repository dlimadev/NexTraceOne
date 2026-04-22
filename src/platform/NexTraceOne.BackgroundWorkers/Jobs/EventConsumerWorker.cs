using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Worker periódico que simula a sondagem de sistemas de mensageria (Kafka, SQS, ServiceBus, RabbitMQ)
/// e encaminha eventos falhados para a fila dead letter via IEventConsumerDeadLetterRepository.
///
/// Esta implementação estabelece a infraestrutura do consumer worker sem criar
/// ligações reais a brokers — o pattern de substituição por implementações reais
/// segue o mesmo modelo do NullKafkaEventProducer / ConfluentKafkaEventProducer.
///
/// Configuração:
/// - EventConsumer:PollingIntervalSeconds (padrão: 30)
/// - EventConsumer:MaxConcurrentConsumers (padrão: 4) — limita paralelismo
/// </summary>
internal sealed class EventConsumerWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EventConsumerWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventConsumerWorker: Started — polling interval {Interval}s.", DefaultInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunPollingCycleAsync(stoppingToken);

            try
            {
                await Task.Delay(DefaultInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("EventConsumerWorker: Stopped.");
    }

    /// <summary>
    /// Executa um ciclo de sondagem para cada estratégia de normalização registada.
    /// Eventos que não podem ser normalizados são encaminhados para dead letter.
    /// </summary>
    private async Task RunPollingCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var strategies = scope.ServiceProvider
            .GetServices<IEventNormalizationStrategy>()
            .ToList();

        var deadLetterRepository = scope.ServiceProvider
            .GetRequiredService<IEventConsumerDeadLetterRepository>();

        if (strategies.Count == 0)
        {
            logger.LogDebug("EventConsumerWorker: No normalization strategies registered. Skipping cycle.");
            return;
        }

        // Simulated polling — em produção cada estratégia conecta ao seu broker
        var pollingTasks = strategies
            .Take(GetMaxConcurrentConsumers(scope))
            .Select(strategy => PollStrategyAsync(strategy, deadLetterRepository, stoppingToken));

        await Task.WhenAll(pollingTasks);
    }

    /// <summary>
    /// Simula a sondagem de uma estratégia individual.
    /// Em produção, conecta ao broker e processa mensagens pendentes.
    /// </summary>
    private async Task PollStrategyAsync(
        IEventNormalizationStrategy strategy,
        IEventConsumerDeadLetterRepository deadLetterRepository,
        CancellationToken ct)
    {
        try
        {
            logger.LogDebug(
                "EventConsumerWorker: Polling strategy '{SourceType}' — no broker configured (null poll).",
                strategy.SourceType);

            // Nenhum evento a processar quando broker não está configurado
            // Em produção: consumir mensagens reais do broker aqui
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "EventConsumerWorker: Unhandled error polling strategy '{SourceType}'. Routing to dead letter.",
                strategy.SourceType);

            var deadLetter = EventConsumerDeadLetterRecord.Record(
                tenantId: Guid.Empty,
                sourceType: strategy.SourceType.ToString(),
                topic: "unknown",
                partitionKey: null,
                payload: "{}",
                lastError: ex.Message);

            await deadLetterRepository.AddAsync(deadLetter, ct);
        }
    }

    private static int GetMaxConcurrentConsumers(IServiceScope scope)
    {
        // Lido a partir de configuração se disponível; padrão 4
        try
        {
            var config = scope.ServiceProvider
                .GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var raw = config?["EventConsumer:MaxConcurrentConsumers"];
            return int.TryParse(raw, out var v) && v > 0 ? v : 4;
        }
        catch
        {
            return 4;
        }
    }
}
