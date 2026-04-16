using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiKnowledgeSourceRepository(AiGovernanceDbContext context) : IAiKnowledgeSourceRepository
{
    public async Task<IReadOnlyList<AIKnowledgeSource>> ListAsync(
        KnowledgeSourceType? sourceType, bool? isActive, CancellationToken ct)
    {
        var query = context.KnowledgeSources.AsQueryable();

        if (sourceType.HasValue)
            query = query.Where(s => s.SourceType == sourceType.Value);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        return await query.OrderBy(s => s.Priority).ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task PersistVectorAsync(
        AIKnowledgeSourceId sourceId, float[] embedding, CancellationToken ct)
    {
        // Persiste o vetor na coluna pgvector via raw SQL (E-A01).
        // A coluna EmbeddingVector é do tipo vector(768) — não mapeada pelo EF Core model.
        var vectorLiteral = $"[{string.Join(",", embedding)}]";
        var sql = $"""
            UPDATE aik_knowledge_sources
            SET "EmbeddingVector" = '{vectorLiteral}'::vector
            WHERE "Id" = '{sourceId.Value}'
            """;

        await context.Database.ExecuteSqlRawAsync(sql, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(AIKnowledgeSourceId Id, double Score)>> SearchByVectorAsync(
        float[] queryEmbedding, int maxResults, CancellationToken ct)
    {
        // Verifica se pgvector está disponível antes de usar o operador <=>.
        // Fallback silencioso: retorna lista vazia (DocumentRetrievalService usa cosine em memória).
        try
        {
            var vectorLiteral = $"[{string.Join(",", queryEmbedding)}]";
            var sql = $"""
                SELECT "Id", (1.0 - ("EmbeddingVector" <=> '{vectorLiteral}'::vector)) AS score
                FROM aik_knowledge_sources
                WHERE "IsActive" = true
                  AND "EmbeddingVector" IS NOT NULL
                ORDER BY "EmbeddingVector" <=> '{vectorLiteral}'::vector
                LIMIT {maxResults}
                """;

            var results = new List<(AIKnowledgeSourceId, double)>();

            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            if (command.Connection!.State != System.Data.ConnectionState.Open)
                await command.Connection.OpenAsync(ct);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var id = AIKnowledgeSourceId.From(reader.GetGuid(0));
                var score = reader.GetDouble(1);
                results.Add((id, score));
            }

            return results;
        }
        catch
        {
            // pgvector não disponível ou coluna não criada — fallback a cosine em memória
            return [];
        }
    }
}
