using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Worker genérico que processa mensagens pendentes do Outbox de qualquer DbContext
/// que herde de <see cref="NexTraceDbContextBase"/>.
///
/// Executa em lote, desserializa os eventos persistidos e os entrega ao EventBus in-process.
/// Cada instância processa o outbox de um único DbContext, garantindo isolamento entre módulos
/// e permitindo que falhas em um módulo não afetem o processamento de outros.
///
/// Comportamento:
/// - Processa em ciclos de 5 segundos
/// - Cada ciclo processa até 50 mensagens pendentes
/// - Mensagens que esgotam as 5 tentativas são persistidas em bb_dead_letter_messages (DLQ)
/// - Erros de um módulo não propagam para outros módulos
/// - Cada mensagem é salva atomicamente após processamento
/// </summary>
/// <typeparam name="TContext">O DbContext concreto que herda de NexTraceDbContextBase.</typeparam>
public sealed class ModuleOutboxProcessorJob<TContext>(
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<ModuleOutboxProcessorJob<TContext>> logger) : BackgroundService
    where TContext : NexTraceDbContextBase
{
    private const int BatchSize = 50;
    private const int MaxRetryCount = 5;

    /// <summary>
    /// Nome do health check derivado do tipo do DbContext.
    /// Formato: "outbox-{ModuleName}" (ex: "outbox-CatalogGraphDbContext").
    /// </summary>
    internal static string HealthCheckName => $"outbox-{typeof(TContext).Name}";

    private static string ModuleName => typeof(TContext).Name;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("Outbox processor started for module {Module}", ModuleName);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await ProcessOutboxAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, $"Outbox cycle failed for {ModuleName}.");
                logger.LogError(ex, "Unhandled error in outbox processor for module {Module}", ModuleName);
            }
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        // IDeadLetterRepository é resolvido de forma lazy — apenas quando uma mensagem
        // esgota retries — para não quebrar environments sem DLQ configurada (ex: testes).
        IDeadLetterRepository? dlqRepository = null;

        var pendingMessages = await dbContext
            .Set<OutboxMessage>()
            .Where(message => message.ProcessedAt == null && message.RetryCount < MaxRetryCount)
            .OrderBy(message => message.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        logger.LogDebug(
            "Processing {Count} outbox messages for module {Module}",
            pendingMessages.Count,
            ModuleName);

        var processedCount = 0;
        var failedCount = 0;

        foreach (var message in pendingMessages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    message.RetryCount++;
                    message.LastError = $"Event type '{message.EventType}' could not be resolved.";
                    logger.LogWarning(
                        "Outbox message {OutboxMessageId} in {Module} ignored because event type {EventType} could not be resolved",
                        message.Id, ModuleName, message.EventType);
                    failedCount++;
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType);
                if (domainEvent is null)
                {
                    message.RetryCount++;
                    message.LastError = $"Event payload for '{message.EventType}' could not be deserialized.";
                    logger.LogWarning(
                        "Outbox message {OutboxMessageId} in {Module} ignored because payload for {EventType} could not be deserialized",
                        message.Id, ModuleName, message.EventType);
                    failedCount++;
                    continue;
                }

                var publishMethod = typeof(IEventBus)
                    .GetMethod(nameof(IEventBus.PublishAsync))!
                    .MakeGenericMethod(eventType);

                await (Task)publishMethod.Invoke(eventBus, [domainEvent, cancellationToken])!;

                message.ProcessedAt = dateTimeProvider.UtcNow;
                message.LastError = null;

                // Atomic per-message save: prevents duplicate delivery on crash.
                await dbContext.SaveChangesAsync(cancellationToken);

                processedCount++;

                logger.LogInformation(
                    "Outbox message {OutboxMessageId} processed successfully for module {Module}",
                    message.Id, ModuleName);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                // Segurança: não armazenar detalhes internos da exceção no banco.
                message.LastError = $"Processing failed at attempt {message.RetryCount}.";

                if (message.RetryCount >= MaxRetryCount)
                {
                    dlqRepository ??= scope.ServiceProvider.GetService<IDeadLetterRepository>();
                    await PersistToDeadLetterQueueAsync(message, ex, dlqRepository, cancellationToken);
                    // ProcessedAt não é definido: a mensagem outbox permanece como registo de auditoria.
                    // Não será re-selecionada pois o query filtra RetryCount < MaxRetryCount.
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                failedCount++;
                logger.LogError(ex,
                    "Failed to process outbox message {OutboxMessageId} for module {Module}",
                    message.Id, ModuleName);
            }
        }

        if (processedCount > 0 || failedCount > 0)
        {
            logger.LogInformation(
                "Outbox cycle completed for {Module}: {Processed} processed, {Failed} failed out of {Total} messages",
                ModuleName, processedCount, failedCount, pendingMessages.Count);
        }
    }

    private async Task PersistToDeadLetterQueueAsync(
        OutboxMessage message,
        Exception exception,
        IDeadLetterRepository? dlqRepository,
        CancellationToken cancellationToken)
    {
        if (dlqRepository is null)
        {
            logger.LogCritical(
                "CRITICAL: IDeadLetterRepository not registered — outbox message {OutboxMessageId} from {Module} exhausted retries and will not be persisted to DLQ.",
                message.Id, ModuleName);
            return;
        }

        try
        {
            var dlqMessage = DeadLetterMessage.From(message, exception, dateTimeProvider.UtcNow);
            await dlqRepository.SaveAsync(dlqMessage, cancellationToken);

            logger.LogError(
                "Outbox message {OutboxMessageId} in {Module} exhausted {RetryCount} retries — persisted to DLQ as {DlqId}",
                message.Id, ModuleName, message.RetryCount, dlqMessage.Id);
        }
        catch (Exception dlqEx)
        {
            // Falha ao escrever na DLQ não deve interromper o processamento do outbox.
            logger.LogCritical(dlqEx,
                "CRITICAL: Failed to persist outbox message {OutboxMessageId} from {Module} to DLQ. Message may be lost.",
                message.Id, ModuleName);
        }
    }
}
