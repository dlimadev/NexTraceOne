namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Contrato para execução de embeddings em provedores de IA.
/// Implementado por cada provider que suporta embeddings (Ollama, OpenAI, etc.).
/// Segue o mesmo padrão de IChatCompletionProvider — um contrato por capacidade.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Identificador do provider associado.</summary>
    string ProviderId { get; }

    /// <summary>Gera embeddings para uma lista de textos.</summary>
    Task<EmbeddingResult> GenerateEmbeddingsAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Request para geração de embeddings.</summary>
public sealed record EmbeddingRequest(
    string ModelId,
    IReadOnlyList<string> Texts);

/// <summary>Resultado da geração de embeddings.</summary>
public sealed record EmbeddingResult(
    bool Success,
    IReadOnlyList<float[]>? Embeddings,
    string ModelId,
    string ProviderId,
    int TokensUsed,
    string? ErrorMessage = null);
