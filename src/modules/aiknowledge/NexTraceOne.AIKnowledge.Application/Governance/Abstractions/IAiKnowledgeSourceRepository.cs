using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de fontes de conhecimento para grounding/contexto da IA.
/// Suporta listagem filtrada por tipo de fonte e estado de ativação.
/// </summary>
public interface IAiKnowledgeSourceRepository
{
    /// <summary>Lista fontes de conhecimento com filtros opcionais de tipo e estado ativo.</summary>
    Task<IReadOnlyList<AIKnowledgeSource>> ListAsync(
        KnowledgeSourceType? sourceType,
        bool? isActive,
        CancellationToken ct);

    /// <summary>
    /// Persiste uma nova fonte de conhecimento (usada pelo pipeline de indexação de fontes externas).
    /// Upsert por nome: actualiza se já existir, insere se não existir.
    /// </summary>
    Task StoreKnowledgeSourceAsync(AIKnowledgeSource source, CancellationToken ct);

    /// <summary>
    /// Persiste o vector de embedding na coluna pgvector da base de dados.
    /// Chamado pelo EmbeddingIndexJob após indexação bem-sucedida. (E-A01)
    /// </summary>
    Task PersistVectorAsync(
        AIKnowledgeSourceId sourceId,
        float[] embedding,
        CancellationToken ct);

    /// <summary>
    /// Executa busca semântica ANN via pgvector (operador coseno &lt;=&gt;).
    /// Retorna IDs das fontes mais próximas do query embedding, ordenadas por
    /// distância crescente (mais similar primeiro). (E-A01)
    /// </summary>
    Task<IReadOnlyList<(AIKnowledgeSourceId Id, double Score)>> SearchByVectorAsync(
        float[] queryEmbedding,
        int maxResults,
        CancellationToken ct);
}
