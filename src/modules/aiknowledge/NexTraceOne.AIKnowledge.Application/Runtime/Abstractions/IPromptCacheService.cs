namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de cache distribuído para prompts e respostas de LLM.
/// Reduz custo e latência em prompts repetidos ou similares.
/// Usa Redis quando disponível; fallback para in-memory.
/// </summary>
public interface IPromptCacheService
{
    /// <summary>
    /// Tenta obter uma resposta cacheada para um prompt + modelo + configuração.
    /// </summary>
    Task<string?> GetCachedResponseAsync(string promptHash, string modelId, CancellationToken ct = default);

    /// <summary>
    /// Armazena uma resposta no cache com TTL configurável.
    /// </summary>
    Task CacheResponseAsync(string promptHash, string modelId, string response, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>
    /// Gera um hash determinístico para um prompt (normalizado).
    /// </summary>
    string ComputePromptHash(string prompt, string modelId, double? temperature = null, int? maxTokens = null);

    /// <summary>
    /// Invalida entradas de cache por prefixo (ex: quando knowledge sources mudam).
    /// </summary>
    Task InvalidateAsync(string prefix, CancellationToken ct = default);
}
