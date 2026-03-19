using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Health check genérico para validar conectividade real de um DbContext EF Core.
/// Usa CanConnectAsync para verificar se a aplicação consegue abrir conexão útil
/// com a base de dados do contexto avaliado.
/// </summary>
public sealed class DbContextConnectivityHealthCheck<TContext>(TContext dbContext) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            var data = new Dictionary<string, object?>
            {
                ["dbContext"] = typeof(TContext).Name,
                ["provider"] = dbContext.Database.ProviderName
            };

            return canConnect
                ? HealthCheckResult.Healthy($"{typeof(TContext).Name} can connect to its database.", data)
                : HealthCheckResult.Unhealthy($"{typeof(TContext).Name} cannot connect to its database.", data: data);
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object?>
            {
                ["dbContext"] = typeof(TContext).Name,
                ["provider"] = dbContext.Database.ProviderName
            };

            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{typeof(TContext).Name} failed to validate database connectivity.",
                ex,
                data);
        }
    }
}
