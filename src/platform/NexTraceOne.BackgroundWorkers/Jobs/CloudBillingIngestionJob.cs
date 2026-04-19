using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que importa registos de billing cloud para o módulo FinOps.
/// Executa diariamente (intervalo de 24 horas) e apenas quando o provider está configurado.
///
/// Design:
/// - Usa IServiceScopeFactory para criar escopo de DI a cada ciclo (serviços Scoped).
/// - Verifica ICloudBillingProvider.IsConfigured antes de executar — sem overhead quando null.
/// - O período de billing é determinado pelo mês corrente (YYYY-MM).
/// - Persistência real via ICostIntelligenceModule será integrada numa iteração futura.
/// </summary>
internal sealed class CloudBillingIngestionJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CloudBillingIngestionJob> logger) : BackgroundService
{
    private static readonly TimeSpan DailyInterval = TimeSpan.FromHours(24);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CloudBillingIngestionJob: Started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunIngestionCycleAsync(stoppingToken);

            try
            {
                await Task.Delay(DailyInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("CloudBillingIngestionJob: Stopped.");
    }

    /// <summary>
    /// Executa um ciclo de ingestão de billing para o mês corrente.
    /// Resolve o provider via DI em escopo isolado para suportar serviços Scoped.
    /// </summary>
    private async Task RunIngestionCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var billingProvider = scope.ServiceProvider.GetRequiredService<ICloudBillingProvider>();

        if (!billingProvider.IsConfigured)
        {
            logger.LogDebug(
                "CloudBillingIngestionJob: Provider '{ProviderName}' is not configured. Skipping billing ingestion.",
                billingProvider.ProviderName);
            return;
        }

        var period = DateTimeOffset.UtcNow.ToString("yyyy-MM");

        try
        {
            logger.LogInformation(
                "CloudBillingIngestionJob: Fetching billing records for period {Period} from provider '{ProviderName}'.",
                period, billingProvider.ProviderName);

            var records = await billingProvider.FetchBillingRecordsAsync(period, stoppingToken);

            logger.LogInformation(
                "CloudBillingIngestionJob: Fetched {Count} billing records for period {Period}. Would import via ICostIntelligenceModule (pending integration).",
                records.Count, period);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "CloudBillingIngestionJob: Failed to fetch billing records for period {Period} from provider '{ProviderName}'.",
                period, billingProvider.ProviderName);
        }
    }
}
