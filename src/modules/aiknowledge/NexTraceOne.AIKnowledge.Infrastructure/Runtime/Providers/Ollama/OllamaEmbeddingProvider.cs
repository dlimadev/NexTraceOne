using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Ollama;

/// <summary>
/// Implementação de IEmbeddingProvider para o Ollama.
/// Gera embeddings via endpoint local /api/embeddings.
/// </summary>
public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    public const string ProviderIdentifier = "ollama";

    private readonly OllamaHttpClient _client;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(
        OllamaHttpClient client,
        IOptions<OllamaOptions> options,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;

    public async Task<EmbeddingResult> GenerateEmbeddingsAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Texts.Count == 0)
        {
            return new EmbeddingResult(true, Array.Empty<float[]>(), request.ModelId, ProviderId, 0);
        }

        var modelId = string.IsNullOrWhiteSpace(request.ModelId)
            ? _options.DefaultModel ?? "nomic-embed-text"
            : request.ModelId;

        try
        {
            var embeddings = new List<float[]>(request.Texts.Count);
            var totalTokens = 0;

            foreach (var text in request.Texts)
            {
                var (embedding, tokens) = await _client.GenerateEmbeddingAsync(
                    modelId, text, cancellationToken);

                embeddings.Add(embedding);
                totalTokens += tokens;
            }

            _logger.LogDebug("Ollama embeddings generated for {Count} texts using model {Model}, tokens: {Tokens}",
                request.Texts.Count, modelId, totalTokens);

            return new EmbeddingResult(true, embeddings, modelId, ProviderId, totalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama embedding generation failed for model {Model}", modelId);
            return new EmbeddingResult(false, null, modelId, ProviderId, 0, ex.Message);
        }
    }
}
