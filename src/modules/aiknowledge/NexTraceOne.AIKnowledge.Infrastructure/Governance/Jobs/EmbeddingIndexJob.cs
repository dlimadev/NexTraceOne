using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job periódico que gera embeddings para fontes de conhecimento ainda não indexadas.
/// Itera as fontes com EmbeddingJson == null e chama IEmbeddingProvider para gerar vetores,
/// persistindo o resultado para uso no retrieval semântico por similaridade coseno.
///
/// Design:
/// - BackgroundService com PeriodicTimer (execução a cada 30 minutos).
/// - Cria scope por ciclo para isolar DbContext e serviços Scoped.
/// - Falhas por fonte são ignoradas individualmente para não bloquear o batch.
/// </summary>
internal sealed class EmbeddingIndexJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EmbeddingIndexJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private const string DefaultEmbeddingModel = "nomic-embed-text";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmbeddingIndexJob started.");

        // Aguarda 90s para deixar outros serviços inicializarem
        await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken);

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
                logger.LogError(ex, "Unhandled error in EmbeddingIndexJob cycle.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        logger.LogInformation("EmbeddingIndexJob stopped.");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var sourceRepository = scope.ServiceProvider.GetRequiredService<IAiKnowledgeSourceRepository>();
        var embeddingProvider = scope.ServiceProvider.GetRequiredService<IEmbeddingProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AiGovernanceDbContext>();

        var allSources = await sourceRepository.ListAsync(
            sourceType: null,
            isActive: true,
            ct: cancellationToken);

        var unindexed = allSources.Where(s => s.EmbeddingJson is null).ToList();

        if (unindexed.Count == 0)
        {
            logger.LogDebug("EmbeddingIndexJob: no unindexed knowledge sources found.");
            return;
        }

        logger.LogInformation("EmbeddingIndexJob: indexing {Count} knowledge sources.", unindexed.Count);

        foreach (var source in unindexed)
        {
            try
            {
                var text = $"{source.Name} {source.Description}".Trim();

                var result = await embeddingProvider.GenerateEmbeddingsAsync(
                    new EmbeddingRequest(DefaultEmbeddingModel, [text]),
                    cancellationToken);

                if (!result.Success || result.Embeddings is null || result.Embeddings.Count == 0)
                {
                    logger.LogWarning(
                        "EmbeddingIndexJob: failed to generate embedding for source {SourceId}: {Error}",
                        source.Id.Value, result.ErrorMessage ?? "empty result");
                    continue;
                }

                var embedding = result.Embeddings[0];

                // Persiste embedding como JSON (fallback para cosine em memória)
                source.SetEmbedding(embedding);

                // Persiste o vetor real na coluna pgvector (E-A01)
                // Falha silenciosa se pgvector não estiver instalado
                try
                {
                    await sourceRepository.PersistVectorAsync(source.Id, embedding, cancellationToken);
                    logger.LogDebug(
                        "EmbeddingIndexJob: persisted pgvector for source {SourceId} ({Dims} dims).",
                        source.Id.Value, embedding.Length);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "EmbeddingIndexJob: pgvector persist failed for source {SourceId} — " +
                        "EmbeddingJson fallback will be used.",
                        source.Id.Value);
                }

                logger.LogDebug(
                    "EmbeddingIndexJob: indexed source {SourceId} with {Dims}-dim embedding.",
                    source.Id.Value, embedding.Length);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "EmbeddingIndexJob: error indexing source {SourceId}.", source.Id.Value);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("EmbeddingIndexJob: cycle complete.");
    }
}
