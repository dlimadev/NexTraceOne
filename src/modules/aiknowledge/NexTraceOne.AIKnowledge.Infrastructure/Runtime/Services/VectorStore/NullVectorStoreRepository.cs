using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.VectorStore;

/// <summary>
/// Implementação nula de IVectorStoreRepository para quando o vector store está desabilitado.
/// Todos os métodos são no-ops ou retornam coleções vazias (fail-open).
/// </summary>
public sealed class NullVectorStoreRepository : IVectorStoreRepository
{
    public Task StoreAsync(
        string collectionName,
        Guid id,
        ReadOnlyMemory<float> vector,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        ReadOnlyMemory<float> queryVector,
        int topK = 5,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);

    public Task DeleteAsync(string collectionName, Guid id, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task EnsureCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default)
        => Task.CompletedTask;
}
