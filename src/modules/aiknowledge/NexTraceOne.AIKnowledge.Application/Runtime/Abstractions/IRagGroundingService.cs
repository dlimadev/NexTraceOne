namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de RAG (Retrieval-Augmented Generation) para enriquecimento de prompts com contexto relevante.
/// Gera embeddings da query, busca no vector store, e retorna texto de grounding.
/// </summary>
public interface IRagGroundingService
{
    /// <summary>
    /// Recupera contexto relevante do vector store para enriquecer um prompt.
    /// </summary>
    /// <param name="query">Texto da query para busca semântica.</param>
    /// <param name="collectionName">Nome da coleção no vector store.</param>
    /// <param name="topK">Número máximo de resultados relevantes.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Texto de contexto concatenado, ou null se não houver resultados ou se o serviço falhar.</returns>
    Task<string?> GetGroundingContextAsync(
        string query,
        string collectionName = "aiknowledge",
        int topK = 5,
        CancellationToken ct = default);
}
