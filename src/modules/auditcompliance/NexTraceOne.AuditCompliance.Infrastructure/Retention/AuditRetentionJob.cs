using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AuditCompliance.Application.Features.ApplyRetention;
using NexTraceOne.AuditCompliance.Application.Retention;

namespace NexTraceOne.AuditCompliance.Infrastructure.Retention;

/// <summary>
/// Job de background para aplicar retenção de eventos auditáveis.
/// Executa periodicamente a ApplyRetention com base nas políticas activas.
/// </summary>
internal sealed class AuditRetentionJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AuditRetentionJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "audit-retention-job";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AuditRetentionJob started.");

        using var initScope = serviceScopeFactory.CreateScope();
        var initOpts = initScope.ServiceProvider.GetRequiredService<IOptions<AuditRetentionOptions>>().Value;
        await Task.Delay(TimeSpan.FromSeconds(initOpts.JobStartupDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            int intervalMinutes;
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
                logger.LogError(ex, "Unhandled error in AuditRetentionJob cycle.");
            }

            using var scope = serviceScopeFactory.CreateScope();
            var opts = scope.ServiceProvider.GetRequiredService<IOptions<AuditRetentionOptions>>().Value;
            intervalMinutes = Math.Max(1, opts.JobIntervalMinutes);

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        logger.LogInformation("AuditRetentionJob stopped.");
    }

    private async Task RunRetentionCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new ApplyRetention.Command(), cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "AuditRetentionJob: retention cycle failed. Error={ErrorCode}",
                result.Error.Code);
            return;
        }

        if (!result.Value.PolicyApplied)
        {
            logger.LogInformation("AuditRetentionJob: no active retention policy found.");
            return;
        }

        if (result.Value.DeletedEventCount > 0)
        {
            logger.LogInformation(
                "AuditRetentionJob: policy={PolicyName} retentionDays={RetentionDays} deleted={Count}",
                result.Value.PolicyName,
                result.Value.RetentionDays,
                result.Value.DeletedEventCount);
        }
    }
}
