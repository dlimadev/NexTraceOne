using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job periódico que sincroniza AIKnowledgeSources indexados (com EmbeddingJson)
/// para o Qdrant vector store. Permite que o RAG grounding service retrieval
/// realmente encontre documentos relevantes.
///
/// Design:
/// - BackgroundService com PeriodicTimer (execução a cada 30 minutos).
/// - Cria scope por ciclo para isolar DbContext e serviços Scoped.
/// - Upsert idempotente por UUID — Qdrant sobrescreve vetores com mesmo ID.
/// - Falhas por fonte são ignoradas individualmente para não bloquear o batch.
/// </summary>
internal sealed class QdrantIndexJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QdrantIndexJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private const string CollectionName = "aiknowledge";

    // Rastreia última execução para reindexar apenas fontes modificadas desde então.
    private DateTimeOffset _lastRunTimestamp = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("QdrantIndexJob started. Collection={Collection}", CollectionName);

        // Aguarda 120s para deixar Qdrant container e outros serviços inicializarem
        await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in QdrantIndexJob cycle.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        logger.LogInformation("QdrantIndexJob stopped.");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var sourceRepository = scope.ServiceProvider.GetRequiredService<IAiKnowledgeSourceRepository>();
        var vectorStore = scope.ServiceProvider.GetService<IVectorStoreRepository>();

        if (vectorStore is null)
        {
            logger.LogWarning("QdrantIndexJob: IVectorStoreRepository not registered (Qdrant:Enabled=false). Skipping cycle.");
            return;
        }

        var allSources = await sourceRepository.ListAsync(
            sourceType: null,
            isActive: true,
            ct: cancellationToken);

        var sourcesToIndex = allSources
            .Where(s => s.EmbeddingJson is not null && s.UpdatedAt > _lastRunTimestamp)
            .ToList();

        if (sourcesToIndex.Count == 0)
        {
            logger.LogDebug("QdrantIndexJob: no modified knowledge sources to sync since {LastRun}.", _lastRunTimestamp);
            _lastRunTimestamp = DateTimeOffset.UtcNow;
            return;
        }

        logger.LogInformation(
            "QdrantIndexJob: syncing {Count} knowledge sources to Qdrant collection '{Collection}'.",
            sourcesToIndex.Count, CollectionName);

        var vectorSize = sourcesToIndex.First().GetEmbedding()?.Length ?? 768;
        await vectorStore.EnsureCollectionAsync(CollectionName, vectorSize, cancellationToken);

        var indexedCount = 0;
        foreach (var source in sourcesToIndex)
        {
            try
            {
                var embedding = source.GetEmbedding();
                if (embedding is null || embedding.Length == 0)
                {
                    logger.LogWarning("QdrantIndexJob: source {SourceId} has null/empty embedding — skipping.", source.Id.Value);
                    continue;
                }

                var metadata = new Dictionary<string, object>
                {
                    ["name"] = source.Name,
                    ["description"] = source.Description,
                    ["sourceType"] = source.SourceType.ToString(),
                    ["endpointOrPath"] = source.EndpointOrPath,
                    ["content"] = $"{source.Name}\n{source.Description}\n{source.EndpointOrPath}".Trim()
                };

                await vectorStore.StoreAsync(
                    CollectionName,
                    source.Id.Value,
                    embedding,
                    metadata,
                    cancellationToken);

                indexedCount++;
                logger.LogDebug(
                    "QdrantIndexJob: upserted source {SourceId} into {Collection}.",
                    source.Id.Value, CollectionName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "QdrantIndexJob: failed to sync source {SourceId} to Qdrant.", source.Id.Value);
            }
        }

        _lastRunTimestamp = DateTimeOffset.UtcNow;
        logger.LogInformation(
            "QdrantIndexJob: cycle complete. {Indexed}/{Total} sources synced to Qdrant.",
            indexedCount, sourcesToIndex.Count);
    }
}
