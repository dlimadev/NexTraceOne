using NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que orquestra a expiração de entidades do módulo Identity.
/// Delega cada tipo de expiração a um handler especializado (IExpirationHandler),
/// garantindo que cada responsabilidade tenha uma única razão para mudar.
///
/// Handlers orquestrados:
/// - DelegationExpirationHandler — delegações cuja vigência terminou.
/// - BreakGlassExpirationHandler — solicitações Break Glass cujo prazo encerrou.
/// - JitAccessExpirationHandler — solicitações JIT Access expiradas.
/// - AccessReviewExpirationHandler — campanhas de Access Review com prazo ultrapassado.
/// - EnvironmentAccessExpirationHandler — acessos temporários a ambientes expirados.
///
/// Design:
/// - Executa a cada 60 segundos (configurável).
/// - Usa lote para minimizar pressão no banco.
/// - Idempotente: entidades já expiradas são ignoradas por seus métodos de domínio.
/// - Falhas em um handler não afetam os demais (isolamento por try/catch).
/// </summary>
public sealed class IdentityExpirationJob(
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    IEnumerable<IExpirationHandler> expirationHandlers,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<IdentityExpirationJob> logger) : BackgroundService
{
    private const int BatchSize = 100;
    internal const string HealthCheckName = "identity-expiration";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await ProcessExpirationsAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Identity expiration cycle failed.");
                logger.LogError(ex, "Unhandled error in Identity expiration job.");
            }
        }
    }

    /// <summary>
    /// Orquestra todas as expirações num único ciclo.
    /// Cada handler é executado independentemente — falha em um não impede os demais.
    /// </summary>
    private async Task ProcessExpirationsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = dateTimeProvider.UtcNow;

        foreach (var handler in expirationHandlers)
        {
            try
            {
                var count = await handler.HandleAsync(dbContext, now, BatchSize, cancellationToken);

                if (count > 0)
                {
                    logger.LogInformation(
                        "Expiration handler {HandlerType} processed {Count} item(s).",
                        handler.GetType().Name,
                        count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Expiration handler {HandlerType} failed.",
                    handler.GetType().Name);
            }
        }
    }
}
