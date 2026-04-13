using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job nocturno que aplica políticas de retenção de dados de IA por tenant.
/// Executa às 02:00 UTC (simulado via delay inicial + PeriodicTimer de 24h).
///
/// Para cada política activa com DataRetentionDays > 0:
/// - Elimina AiMessage com CreatedAt anterior ao período de retenção
/// - Elimina AIUsageEntry com Timestamp anterior ao período
/// - Elimina AiTokenUsageLedger com Timestamp anterior ao período
///
/// Design:
/// - BackgroundService com PeriodicTimer (24h).
/// - Cria scope por ciclo para isolar DbContext.
/// - Operações são idempotentes e seguras para re-execução.
/// - Cada operação é registada em log estruturado com contagem de registos eliminados.
/// </summary>
internal sealed class AiDataRetentionJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AiDataRetentionJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "ai-data-retention-job";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AiDataRetentionJob started.");

        // Calcula delay até próxima execução às 02:00 UTC
        var now = DateTimeOffset.UtcNow;
        var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 2, 0, 0, TimeSpan.Zero);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var initialDelay = nextRun - now;
        logger.LogInformation(
            "AiDataRetentionJob will first run at {NextRun} UTC (in {Delay})",
            nextRun, initialDelay);

        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("AiDataRetentionJob cancelled before first run.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRetentionCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in AiDataRetentionJob cycle.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        logger.LogInformation("AiDataRetentionJob stopped.");
    }

    private async Task RunRetentionCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var policyRepository = scope.ServiceProvider.GetRequiredService<IAiAccessPolicyRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IAiMessageRepository>();
        var usageRepository = scope.ServiceProvider.GetRequiredService<IAiUsageEntryRepository>();
        var ledgerRepository = scope.ServiceProvider.GetRequiredService<IAiTokenUsageLedgerRepository>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;

        // Obter políticas activas com retenção definida
        var policies = await policyRepository.ListAsync(
            scope: null, isActive: true, cancellationToken);

        var retentionPolicies = policies
            .Where(p => p.DataRetentionDays.HasValue && p.DataRetentionDays.Value > 0)
            .ToList();

        if (retentionPolicies.Count == 0)
        {
            logger.LogDebug("AiDataRetentionJob: no active policies with DataRetentionDays configured.");
            return;
        }

        // Usar a política com menor retenção como limite global do ciclo
        // (em cenários multi-tenant com contexto isolado, seria por tenant)
        var minRetentionDays = retentionPolicies.Min(p => p.DataRetentionDays!.Value);
        var cutoff = now.AddDays(-minRetentionDays);

        logger.LogInformation(
            "AiDataRetentionJob: applying retention cutoff {Cutoff} UTC (min {Days} days)",
            cutoff, minRetentionDays);

        // Eliminar mensagens antigas
        var deletedMessages = await messageRepository.DeleteOlderThanAsync(cutoff, cancellationToken);
        logger.LogInformation(
            "AiDataRetentionJob: deleted {Count} AiMessages older than {Cutoff}",
            deletedMessages, cutoff);

        // Eliminar entradas de auditoria antigas
        var deletedUsage = await usageRepository.DeleteOlderThanAsync(cutoff, cancellationToken);
        logger.LogInformation(
            "AiDataRetentionJob: deleted {Count} AIUsageEntries older than {Cutoff}",
            deletedUsage, cutoff);

        // Eliminar entradas de ledger antigas
        var deletedLedger = await ledgerRepository.DeleteOlderThanAsync(cutoff, cancellationToken);
        logger.LogInformation(
            "AiDataRetentionJob: deleted {Count} AiTokenUsageLedger entries older than {Cutoff}",
            deletedLedger, cutoff);

        logger.LogInformation(
            "AiDataRetentionJob cycle complete. Messages={M}, UsageEntries={U}, LedgerEntries={L}",
            deletedMessages, deletedUsage, deletedLedger);
    }
}
