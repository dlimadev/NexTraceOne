using NexTraceOne.AiGovernance.Domain.Entities;

namespace NexTraceOne.AiGovernance.Application.Abstractions;

/// <summary>
/// Repositório de mensagens de conversas do assistente de IA.
/// Suporta listagem de mensagens por conversa e persistência.
/// </summary>
public interface IAiMessageRepository
{
    /// <summary>Lista mensagens de uma conversa ordenadas por timestamp.</summary>
    Task<IReadOnlyList<AiMessage>> ListByConversationAsync(
        Guid conversationId,
        int pageSize,
        CancellationToken ct);

    /// <summary>Adiciona uma nova mensagem para persistência.</summary>
    Task AddAsync(AiMessage message, CancellationToken ct);

    /// <summary>Conta o total de mensagens de uma conversa.</summary>
    Task<int> CountByConversationAsync(Guid conversationId, CancellationToken ct);
}
