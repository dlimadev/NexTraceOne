using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Persistence;
using NexTraceOne.Configuration.Infrastructure.Persistence;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.Notifications.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Implementação de IPendingMigrationsProvider que consulta todos os 17 DbContexts
/// registados no ApiHost para obter a lista de migrations pendentes.
///
/// Esta classe reside no ApiHost (e não nos módulos) porque é o único ponto
/// do sistema que tem visibilidade sobre todos os DbContexts de todos os módulos.
///
/// Cada DbContext é resolvido dentro de um scope de DI isolado para evitar
/// interferência com o ciclo de vida dos contextos usados em requests normais.
/// </summary>
internal sealed class ApiHostPendingMigrationsProvider(IServiceScopeFactory scopeFactory) : IPendingMigrationsProvider
{
    public async Task<IReadOnlyList<PendingMigrationInfo>> GetPendingMigrationsAsync(CancellationToken cancellationToken)
    {
        var allPending = new List<PendingMigrationInfo>();

        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await CollectPendingAsync<BuildingBlocksDbContext>(sp, "BuildingBlocks", allPending, cancellationToken);
        await CollectPendingAsync<ConfigurationDbContext>(sp, "Configuration", allPending, cancellationToken);
        await CollectPendingAsync<IdentityDbContext>(sp, "Identity", allPending, cancellationToken);
        await CollectPendingAsync<ServiceCatalogDbContext>(sp, "ServiceCatalog", allPending, cancellationToken);
        await CollectPendingAsync<ChangeGovernanceDbContext>(sp, "ChangeGovernance", allPending, cancellationToken);
        await CollectPendingAsync<IncidentResponseDbContext>(sp, "IncidentResponse", allPending, cancellationToken);
        await CollectPendingAsync<CostIntelligenceDbContext>(sp, "CostIntelligence", allPending, cancellationToken);
        await CollectPendingAsync<TelemetryStoreDbContext>(sp, "TelemetryStore", allPending, cancellationToken);
        await CollectPendingAsync<AuditDbContext>(sp, "Audit", allPending, cancellationToken);
        await CollectPendingAsync<GovernanceDbContext>(sp, "Governance", allPending, cancellationToken);
        await CollectPendingAsync<IntegrationsDbContext>(sp, "Integrations", allPending, cancellationToken);
        await CollectPendingAsync<ProductAnalyticsDbContext>(sp, "ProductAnalytics", allPending, cancellationToken);
        await CollectPendingAsync<NotificationsDbContext>(sp, "Notifications", allPending, cancellationToken);
        await CollectPendingAsync<KnowledgeDbContext>(sp, "Knowledge", allPending, cancellationToken);
        await CollectPendingAsync<AiHubDbContext>(sp, "AiHub", allPending, cancellationToken);

        return allPending;
    }

    private static async Task CollectPendingAsync<TContext>(
        IServiceProvider sp,
        string contextName,
        List<PendingMigrationInfo> results,
        CancellationToken cancellationToken)
        where TContext : DbContext
    {
        try
        {
            var context = sp.GetRequiredService<TContext>();
            var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            foreach (var migrationId in pending)
            {
                results.Add(new PendingMigrationInfo(migrationId, contextName));
            }
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation
            throw;
        }
        catch
        {
            // DbContext not available or DB not accessible — skip silently.
            // The health endpoint will reflect the real DB status.
        }
    }
}
