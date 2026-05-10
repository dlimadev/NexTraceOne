using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que lê traces OTel e popula o inventário de consumidores reais de contratos.
///
/// Estratégia: para cada API asset não decommissionado, procura traces cujo OperationName
/// contenha o prefixo do RoutePattern do asset. Cada ServiceName único nesses traces
/// é registado como consumidor. A frequência diária é extrapolada da contagem de traces
/// na janela de lookback.
///
/// Frequência: a cada 15 minutos (configurável via BackgroundWorkers:ContractConsumerIngestion).
/// Janela de lookback: 1 hora de traces (configurável).
///
/// Referência: CC-04, FUTURE-ROADMAP.md Wave A.2.
/// </summary>
public sealed class ContractConsumerIngestionJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<ContractConsumerIngestionJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "contract-consumer-ingestion";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);

        await RunIngestionCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<ContractConsumerIngestionOptions>>().Value;

            if (!options.Enabled)
            {
                logger.LogDebug("ContractConsumerIngestionJob is disabled by configuration. Sleeping.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            try
            {
                await Task.Delay(options.IntervalBetweenCycles, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!stoppingToken.IsCancellationRequested)
                await RunIngestionCycleAsync(stoppingToken);
        }
    }

    private async Task RunIngestionCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ContractConsumerIngestionOptions>>().Value;

        if (!options.Enabled) return;

        jobHealthRegistry.MarkStarted(HealthCheckName);

        try
        {
            var graphDb = scope.ServiceProvider.GetRequiredService<CatalogGraphDbContext>();
            var consumerRepo = scope.ServiceProvider.GetRequiredService<IContractConsumerInventoryRepository>();
            var contractsUow = scope.ServiceProvider.GetRequiredService<IContractsUnitOfWork>();
            var observability = scope.ServiceProvider.GetRequiredService<IObservabilityProvider>();

            // Build route-prefix → apiAssetId mapping from published API assets
            var assetRoutes = await BuildAssetRouteTableAsync(graphDb, cancellationToken);
            if (assetRoutes.Count == 0)
            {
                logger.LogDebug("ContractConsumerIngestionJob: no active API assets found. Skipping cycle.");
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var from = now - options.LookbackWindow;
            var totalUpserted = 0;

            foreach (var environment in options.Environments)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var upserted = await ProcessEnvironmentAsync(
                        environment, from, now,
                        assetRoutes, consumerRepo, contractsUow,
                        observability, options, cancellationToken);

                    totalUpserted += upserted;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "ContractConsumerIngestionJob: error processing environment '{Environment}'.", environment);
                }
            }

            logger.LogInformation(
                "ContractConsumerIngestionJob cycle complete. Environments: {EnvCount}, Records upserted: {UpsertCount}.",
                options.Environments.Count, totalUpserted);

            jobHealthRegistry.MarkSucceeded(HealthCheckName);
        }
        catch (Exception ex)
        {
            jobHealthRegistry.MarkFailed(HealthCheckName, "Contract consumer ingestion cycle failed.");
            logger.LogError(ex, "ContractConsumerIngestionJob: unhandled error in ingestion cycle.");
        }
    }

    private static async Task<List<AssetRouteEntry>> BuildAssetRouteTableAsync(
        CatalogGraphDbContext graphDb, CancellationToken cancellationToken)
    {
        return await graphDb.ApiAssets
            .AsNoTracking()
            .Where(a => !a.IsDecommissioned && !string.IsNullOrEmpty(a.RoutePattern))
            .Select(a => new AssetRouteEntry(a.Id.Value, a.RoutePattern))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> ProcessEnvironmentAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        List<AssetRouteEntry> assetRoutes,
        IContractConsumerInventoryRepository consumerRepo,
        IContractsUnitOfWork contractsUow,
        IObservabilityProvider observability,
        ContractConsumerIngestionOptions options,
        CancellationToken cancellationToken)
    {
        var filter = new TraceQueryFilter
        {
            Environment = environment,
            From = from,
            Until = until,
            Limit = options.MaxTracesPerEnvironment
        };

        var traces = await observability.QueryTracesAsync(filter, cancellationToken);
        if (traces.Count == 0) return 0;

        var lookbackHours = Math.Max(1.0, (until - from).TotalHours);
        var upsertCount = 0;

        // For each API asset, find traces whose OperationName matches the route prefix.
        // Group by ServiceName (consumer) to calculate per-consumer frequency.
        foreach (var asset in assetRoutes)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var routePrefix = ExtractRoutePrefix(asset.RoutePattern);
            var matching = traces
                .Where(t => MatchesRoute(t.OperationName, routePrefix)
                         && !string.IsNullOrWhiteSpace(t.ServiceName))
                .ToList();

            if (matching.Count == 0) continue;

            var byConsumer = matching.GroupBy(t => t.ServiceName, StringComparer.OrdinalIgnoreCase);

            foreach (var group in byConsumer)
            {
                try
                {
                    var consumerService = group.Key;
                    var callCount = group.Count();
                    var frequencyPerDay = callCount / lookbackHours * 24.0;
                    var lastCall = group.Max(t => t.StartTime);
                    var version = ExtractVersionFromTraces(group);

                    var existing = await consumerRepo.GetByUniqueKeyAsync(
                        asset.ApiAssetId, options.TenantId, consumerService, environment, cancellationToken);

                    if (existing is null)
                    {
                        var inventory = ContractConsumerInventory.Create(
                            options.TenantId, asset.ApiAssetId, consumerService,
                            environment, version, frequencyPerDay, lastCall, until);
                        await consumerRepo.AddAsync(inventory, cancellationToken);
                    }
                    else
                    {
                        existing.Update(frequencyPerDay, lastCall, until);
                        await consumerRepo.UpdateAsync(existing, cancellationToken);
                    }

                    await contractsUow.CommitAsync(cancellationToken);
                    upsertCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "ContractConsumerIngestionJob: failed to upsert consumer '{Consumer}' → asset '{AssetId}'.",
                        group.Key, asset.ApiAssetId);
                }
            }
        }

        return upsertCount;
    }

    // Extracts the invariant prefix of a route pattern (up to the first path template parameter).
    // e.g. "/api/v1/users/{id}" → "/api/v1/users"
    //      "/api/v2/orders"     → "/api/v2/orders"
    private static string ExtractRoutePrefix(string routePattern)
    {
        var braceIndex = routePattern.IndexOf('{');
        var prefix = braceIndex > 0 ? routePattern[..braceIndex].TrimEnd('/') : routePattern;
        return prefix.ToLowerInvariant();
    }

    private static bool MatchesRoute(string operationName, string routePrefix)
    {
        if (string.IsNullOrWhiteSpace(operationName)) return false;
        var op = operationName.ToLowerInvariant();
        // OperationName may be "GET /api/v1/users/123" or "/api/v1/users/123"
        var slashIndex = op.IndexOf('/');
        if (slashIndex < 0) return false;
        var path = op[slashIndex..];
        return path.StartsWith(routePrefix, StringComparison.Ordinal);
    }

    private static string? ExtractVersionFromTraces(IEnumerable<TraceSummary> traces)
    {
        foreach (var trace in traces)
        {
            var slashIndex = trace.OperationName.IndexOf('/');
            if (slashIndex < 0) continue;
            var path = trace.OperationName[slashIndex..];
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var v = segments.FirstOrDefault(s => s.StartsWith('v') && s.Length > 1 && char.IsDigit(s[1]));
            if (v is not null) return v;
        }
        return null;
    }

    private sealed record AssetRouteEntry(Guid ApiAssetId, string RoutePattern);
}
