namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Cache para leituras de quota de tokens — evita DB calls repetidos por request.
/// Implementações: in-memory (padrão) ou Redis (quando disponível).
/// </summary>
public interface ITokenQuotaCache
{
    /// <summary>Retorna total de tokens consumidos no período, ou null se não estiver em cache.</summary>
    Task<long?> GetUsageAsync(string userId, string granularity, CancellationToken ct = default);

    /// <summary>Guarda total de tokens consumidos no período com TTL especificado.</summary>
    Task SetUsageAsync(string userId, string granularity, long tokens, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Invalida todas as entradas de cache para o utilizador.</summary>
    Task InvalidateUserAsync(string userId, CancellationToken ct = default);
}
