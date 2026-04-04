using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.OpenAI;

/// <summary>
/// Implementação de IEmbeddingProvider para OpenAI.
/// Gera embeddings via endpoint /v1/embeddings.
/// </summary>
public sealed class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    public const string ProviderIdentifier = "openai";

    private readonly OpenAiHttpClient _client;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;

    public OpenAiEmbeddingProvider(
        OpenAiHttpClient client,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiEmbeddingProvider> logger)
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
            ? _options.EmbeddingModel ?? "text-embedding-3-small"
            : request.ModelId;

        try
        {
            var (embeddings, tokens) = await _client.GenerateEmbeddingsAsync(
                modelId, request.Texts, cancellationToken);

            _logger.LogDebug("OpenAI embeddings generated for {Count} texts using model {Model}, tokens: {Tokens}",
                request.Texts.Count, modelId, tokens);

            return new EmbeddingResult(true, embeddings, modelId, ProviderId, tokens);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI embedding generation failed for model {Model}", modelId);
            return new EmbeddingResult(false, null, modelId, ProviderId, 0, ex.Message);
        }
    }
}
