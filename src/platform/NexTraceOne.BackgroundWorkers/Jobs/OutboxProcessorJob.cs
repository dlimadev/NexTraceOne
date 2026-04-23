using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using System.Text.Json;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Worker que processa mensagens pendentes do Outbox do módulo Identity.
/// Executa em lote, desserializa os eventos persistidos e os entrega ao EventBus in-process.
///
/// LIMITATION: Currently only processes IdentityDbContext outbox.
/// Other module DbContexts (Governance, ChangeIntelligence, Contracts, AI, etc.)
/// have outbox tables but no processor. Extend this job or create per-module processors.
/// </summary>
public sealed class OutboxProcessorJob(
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<OutboxProcessorJob> logger) : BackgroundService
{
    private const int _batchSize = 50;
    private const int _maxRetryCount = 5;
    internal const string HealthCheckName = "outbox-processor";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await ProcessIdentityOutboxAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Outbox cycle failed.");
                logger.LogError(ex, "Unhandled error in outbox processor job.");
            }
        }
    }

    private async Task ProcessIdentityOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var pendingMessages = await dbContext
            .Set<OutboxMessage>()
            .Where(message => message.ProcessedAt == null && message.RetryCount < _maxRetryCount)
            .OrderBy(message => message.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    message.RetryCount++;
                    message.LastError = $"Event type '{message.EventType}' could not be resolved.";
                    logger.LogWarning("Outbox message {OutboxMessageId} ignored because event type {EventType} could not be resolved", message.Id, message.EventType);
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType);
                if (domainEvent is null)
                {
                    message.RetryCount++;
                    message.LastError = $"Event payload for '{message.EventType}' could not be deserialized.";
                    logger.LogWarning("Outbox message {OutboxMessageId} ignored because payload for {EventType} could not be deserialized", message.Id, message.EventType);
                    continue;
                }

                var publishMethod = typeof(IEventBus)
                    .GetMethod(nameof(IEventBus.PublishAsync))!
                    .MakeGenericMethod(eventType);

                await (Task)publishMethod.Invoke(eventBus, [domainEvent, cancellationToken])!;

                message.ProcessedAt = dateTimeProvider.UtcNow;
                message.LastError = null;

                // Atomic per-message save: prevents duplicate delivery on crash.
                // If we crash after publish but before save, only this one message is redelivered.
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Outbox message {OutboxMessageId} processed successfully", message.Id);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                // Segurança: não armazenar a mensagem completa da exceção no banco,
                // pois pode conter detalhes internos (stack trace, tipos, connection strings).
                message.LastError = $"Processing failed at attempt {message.RetryCount}.";
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogError(ex, "Failed to process outbox message {OutboxMessageId}", message.Id);
            }
        }

        var exhaustedMessages = pendingMessages.Where(message => message.ProcessedAt == null && message.RetryCount >= _maxRetryCount).ToArray();
        foreach (var exhaustedMessage in exhaustedMessages)
        {
            logger.LogError(
                "Outbox message {OutboxMessageId} reached max retry count with last error: {LastError}",
                exhaustedMessage.Id,
                exhaustedMessage.LastError);
        }
    }
}
