namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;

/// <summary>
/// Abstração de repositório de vector store para RAG.
/// Implementações: Qdrant (produção), pgvector (legacy), in-memory (testes).
/// </summary>
public interface IVectorStoreRepository
{
    /// <summary>Armazena um vector com metadata associada.</summary>
    Task StoreAsync(
        string collectionName,
        Guid id,
        ReadOnlyMemory<float> vector,
        Dictionary<string, object> metadata,
        CancellationToken ct = default);

    /// <summary>Procura os k vectores mais similares.</summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        ReadOnlyMemory<float> queryVector,
        int topK = 5,
        CancellationToken ct = default);

    /// <summary>Elimina um vector por ID.</summary>
    Task DeleteAsync(string collectionName, Guid id, CancellationToken ct = default);

    /// <summary>Cria uma coleção se não existir.</summary>
    Task EnsureCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default);
}

/// <summary>Resultado de uma pesquisa vectorial.</summary>
public sealed record VectorSearchResult(
    Guid Id,
    float Score,
    Dictionary<string, object> Metadata);
