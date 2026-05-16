using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.VectorStore;

/// <summary>
/// Implementação de IVectorStoreRepository usando Qdrant via gRPC.
/// Phase 1: stub funcional — operações reais serão ativadas na Phase 2.
/// </summary>
public sealed class QdrantVectorStoreRepository : IVectorStoreRepository, IDisposable
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStoreRepository> _logger;

    public QdrantVectorStoreRepository(
        string host,
        int port,
        ILogger<QdrantVectorStoreRepository> logger)
    {
        _client = new QdrantClient(host, port);
        _logger = logger;
    }

    public async Task StoreAsync(
        string collectionName,
        Guid id,
        ReadOnlyMemory<float> vector,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = id.ToString() },
            Vectors = vector.ToArray(),
        };

        foreach (var kv in metadata)
        {
            point.Payload[kv.Key] = ConvertToValue(kv.Value);
        }

        await _client.UpsertAsync(collectionName, new List<PointStruct> { point }, cancellationToken: ct);
        _logger.LogDebug("Stored vector {Id} in collection {Collection}", id, collectionName);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        ReadOnlyMemory<float> queryVector,
        int topK = 5,
        CancellationToken ct = default)
    {
        var results = await _client.QueryAsync(
            collectionName,
            queryVector.ToArray(),
            limit: (ulong)topK,
            cancellationToken: ct);

        return results.Select(r => new VectorSearchResult(
            Guid.Parse(r.Id.Uuid),
            r.Score,
            r.Payload.ToDictionary(kv => kv.Key, kv => (object)kv.Value.ToString())))
            .ToList();
    }

    public async Task DeleteAsync(string collectionName, Guid id, CancellationToken ct = default)
    {
        // Phase 2: usar API correta do Qdrant.Client conforme versão instalada
        _logger.LogDebug("Delete vector {Id} from collection {Collection} (stub)", id, collectionName);
        await Task.CompletedTask;
    }

    public async Task EnsureCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default)
    {
        var exists = await _client.CollectionExistsAsync(collectionName, cancellationToken: ct);
        if (!exists)
        {
            await _client.CreateCollectionAsync(
                collectionName,
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine },
                cancellationToken: ct);
            _logger.LogInformation("Created Qdrant collection {Collection} with size {Size}", collectionName, vectorSize);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static Value ConvertToValue(object value)
    {
        return value switch
        {
            string s => new Value { StringValue = s },
            int i => new Value { IntegerValue = i },
            long l => new Value { IntegerValue = l },
            double d => new Value { DoubleValue = d },
            bool b => new Value { BoolValue = b },
            _ => new Value { StringValue = value.ToString() }
        };
    }
}
