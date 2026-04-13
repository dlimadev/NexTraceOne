namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Cache de embeddings para evitar recalculações repetidas durante a sessão.
/// Útil para grounding semântico em queries frequentes com o mesmo texto.
/// </summary>
public interface IEmbeddingCacheService
{
    /// <summary>
    /// Retorna o embedding para o texto fornecido — do cache se disponível,
    /// ou computado via IEmbeddingProvider se ainda não cacheado.
    /// </summary>
    Task<float[]> GetOrComputeAsync(string text, CancellationToken ct = default);
}

/// <summary>
/// Implementação nula de IEmbeddingCacheService — sempre retorna array vazio.
/// Usada como fallback quando o serviço real não está configurado.
/// </summary>
public sealed class NullEmbeddingCacheService : IEmbeddingCacheService
{
    /// <summary>Instância singleton de uso seguro.</summary>
    public static readonly NullEmbeddingCacheService Instance = new();

    private NullEmbeddingCacheService() { }

    public Task<float[]> GetOrComputeAsync(string text, CancellationToken ct = default)
        => Task.FromResult(Array.Empty<float>());
}
