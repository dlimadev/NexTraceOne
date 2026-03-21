using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Repositório de entradas sugeridas para base de conhecimento da orquestração de IA.
/// </summary>
public interface IKnowledgeCaptureEntryRepository
{
    /// <summary>Obtém uma entrada pelo identificador.</summary>
    Task<KnowledgeCaptureEntry?> GetByIdAsync(KnowledgeCaptureEntryId id, CancellationToken ct);

    /// <summary>Verifica se existe outra entrada com o mesmo título na mesma conversa.</summary>
    Task<bool> HasDuplicateTitleInConversationAsync(
        AiConversationId conversationId,
        KnowledgeCaptureEntryId excludeId,
        string title,
        CancellationToken ct);
}
